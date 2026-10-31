using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Editor-only authoring tool. The saved stage consists of ordinary editable Tilemaps.</summary>
public static class TestStageMapBuilder
{
    const string Source = "Assets/2.Sprites/Map/spring farm tilemap.png";
    const string Folder = "Assets/6.TilePalettes";
    const string PalettePath = Folder + "/SpringFarm.prefab";
    const string ScenePath = "Assets/Scenes/TestStage.unity";
    static readonly Dictionary<Vector2Int, Tile> Tiles = new();
    static Material material;

    // Explicit menu action only; never rebuild on reload or on entering Play mode.
    [MenuItem("Tools/Explosive Wizard/Create Test Stage Map")]
    public static void Build()
    {
        if (File.Exists(ScenePath) || File.Exists(PalettePath))
            throw new InvalidOperationException("Test stage or palette already exists. Preserve hand edits; use a new path to regenerate.");
        Directory.CreateDirectory(Folder + "/Tiles");
        AssetDatabase.Refresh();
        Tiles.Clear();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Sprite>().ToArray();
        if (sprites.Length == 0) throw new InvalidOperationException("Map sprites have not been imported.");
        foreach (var sprite in sprites)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = sprite.name;
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            var path = Folder + "/Tiles/" + sprite.name + ".asset";
            AssetDatabase.CreateAsset(tile, path);
            Tiles.Add(new Vector2Int((int)sprite.rect.x / 16, (int)sprite.rect.y / 16), tile);
        }

        // Preserve the import scale (16 px / 32 PPU = 0.5 world units).
        float cellSize = 16f / sprites[0].pixelsPerUnit;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) throw new InvalidOperationException("URP 2D unlit shader missing.");
        material = new Material(shader) { name = "Map Unlit" };
        AssetDatabase.CreateAsset(material, Folder + "/MapUnlit.mat");

        CreatePalette(Source, "SpringFarm", sprites, cellSize);
        foreach (var path in Directory.GetFiles("Assets/2.Sprites/Map", "*.png").Where(p => p.Replace('\\', '/') != Source))
        {
            var extraSprites = AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<Sprite>().ToArray();
            CreatePalette(path, Path.GetFileNameWithoutExtension(path), extraSprites, cellSize);
        }

        BuildStage();
    }

    public static void BuildStage()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Preserve existing stage edits.");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Sprite>().ToArray();
        Tiles.Clear();
        foreach (var sprite in sprites)
            Tiles.Add(new Vector2Int((int)sprite.rect.x / 16, (int)sprite.rect.y / 16),
                AssetDatabase.LoadAssetAtPath<Tile>(Folder + "/Tiles/" + sprite.name + ".asset"));
        material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/MapUnlit.mat");
        float cellSize = 16f / sprites[0].pixelsPerUnit;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var root = new GameObject("Test Stage - Forest Arena");
        var grid = new GameObject("Grid - 16px Art Tiles", typeof(Grid));
        grid.transform.SetParent(root.transform);
        grid.GetComponent<Grid>().cellSize = new Vector3(cellSize, cellSize, 1);
        var ground = Layer(grid.transform, "01 Ground", 0);
        var arena = Layer(grid.transform, "02 Arena and Entrance", 1);
        var detail = Layer(grid.transform, "03 Ground Details", 2);
        var foliage = Layer(grid.transform, "04 Border Foliage", 3);

        BuildMap(ground, arena, detail, foliage);
        var markers = new GameObject("Reference Points - placeholders only");
        markers.transform.SetParent(root.transform);
        Marker(markers.transform, "Player Entry", new Vector3(0, -3, 0));
        Marker(markers.transform, "Enemy A", new Vector3(-2, 2, 0));
        Marker(markers.transform, "Enemy B", new Vector3(2, 2, 0));
        Marker(markers.transform, "Arena Center", Vector3.zero);

        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(28, 43, 32, 255);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        foreach (var map in new[] { ground, arena, detail, foliage }) map.CompressBounds();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        // Keep the user's other open scenes intact. Batch validation can reload separately.
        ValidatePalette();
        ValidateStage(scene);
        RenderPreview(camera);
        Debug.Log($"MAP_BUILD_OK: {sprites.Length} source tiles; palettes and editable stage saved.");
    }

    static void CreatePalette(string source, string name, Sprite[] sprites, float cellSize)
    {
        if (sprites.Length == 0) throw new InvalidOperationException("No sliced sprites: " + source);
        var path = Folder + "/" + name + ".prefab";
        GridPaletteUtility.CreateNewPalette(Folder, name, GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Manual, new Vector3(cellSize, cellSize, 1), GridLayout.CellSwizzle.XYZ);
        var editable = PrefabUtility.LoadPrefabContents(path);
        var paletteMap = editable.GetComponentInChildren<Tilemap>();
        paletteMap.GetComponent<TilemapRenderer>().sharedMaterial = material;
        int topRow = sprites.Max(s => (int)s.rect.y / 16);
        foreach (var sprite in sprites)
        {
            Tile tile;
            if (source == Source) tile = Tiles[new Vector2Int((int)sprite.rect.x / 16, (int)sprite.rect.y / 16)];
            else
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, Folder + "/Tiles/" + sprite.name + ".asset");
            }
            paletteMap.SetTile(new Vector3Int((int)sprite.rect.x / 16, (int)sprite.rect.y / 16 - topRow, 0), tile);
        }
        paletteMap.CompressBounds();
        PrefabUtility.SaveAsPrefabAsset(editable, path);
        PrefabUtility.UnloadPrefabContents(editable);

    }

    static void BuildMap(Tilemap ground, Tilemap arena, Tilemap detail, Tilemap foliage)
    {
        // A broad grass island; every 3x3 edge comes from a contiguous source patch.
        Patch(ground, -20, -14, 40, 28, 2, 17);
        // Keep the central floor open and assemble a complete continuous border.
        Patch(arena, -12, -8, 24, 16, 5, 6);
        // Southern entry path joins the arena without an edge seam.
        for (int y = -13; y <= -7; y++)
            for (int x = -2; x <= 1; x++)
                Put(arena, x, y, x == -2 ? 5 : x == 1 ? 7 : 6, 7);

        Put(detail, -14, -9, 2, 0);
        Put(detail, 13, 8, 3, 0);
        // Border bushes are stamped as complete source clusters, never shuffled slices.
        foreach (int x in new[] { -18, -14, -10, -6, -2, 2, 6, 10, 14 })
        {
            Stamp(foliage, x, 10, 5, 13, 3, 3);
            if (Math.Abs(x) > 3) Stamp(foliage, x, -13, 5, 13, 3, 3);
        }
        foreach (int y in new[] { -8, -4, 0, 4, 8 })
        {
            Stamp(foliage, -19, y, 5, 13, 3, 3);
            Stamp(foliage, 16, y, 5, 13, 3, 3);
        }
        foreach (var p in new[] { new Vector2Int(-14, -5), new Vector2Int(13, -3),
                     new Vector2Int(-13, 5), new Vector2Int(12, 7), new Vector2Int(-7, 9), new Vector2Int(7, -10) })
            Stamp(foliage, p.x, p.y, 2, 13, 2, 2);
        foreach (var p in new[] { new Vector2Int(-15, 1), new Vector2Int(14, 3),
                     new Vector2Int(-8, -10), new Vector2Int(9, 9) })
            Put(detail, p.x, p.y, 0, 18);

        // Four complete trees from the objects sheet reinforce the forest boundary.
        var treeSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/2.Sprites/Map/farm objects free.png")
            .OfType<Sprite>().Where(s => s.rect.x >= 48 && s.rect.x < 96 && s.rect.y >= 127).ToArray();
        foreach (var origin in new[] { new Vector2Int(-16, 6), new Vector2Int(13, 6),
                     new Vector2Int(-16, -11), new Vector2Int(13, -11) })
        {
            foreach (var sprite in treeSprites)
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(Folder + "/Tiles/" + sprite.name + ".asset");
                foliage.SetTile(new Vector3Int(origin.x + (int)sprite.rect.x / 16 - 3,
                    origin.y + (int)sprite.rect.y / 16 - 7, 0), tile);
            }
        }

    }

    static Tilemap Layer(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent);
        var renderer = go.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        renderer.sharedMaterial = material;
        return go.GetComponent<Tilemap>();
    }

    static void Put(Tilemap map, int x, int y, int sx, int sy)
    {
        if (!Tiles.TryGetValue(new Vector2Int(sx, sy), out var tile))
            throw new InvalidOperationException($"Missing source tile at {sx},{sy}");
        map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static void Patch(Tilemap map, int x, int y, int width, int height, int sx, int sy)
    {
        for (int j = 0; j < height; j++)
            for (int i = 0; i < width; i++)
                Put(map, x + i, y + j, sx + (i == 0 ? 0 : i == width - 1 ? 2 : 1),
                    sy + (j == 0 ? 0 : j == height - 1 ? 2 : 1));
    }

    static void Stamp(Tilemap map, int x, int y, int sx, int sy, int width, int height)
    {
        for (int j = 0; j < height; j++)
            for (int i = 0; i < width; i++) Put(map, x + i, y + j, sx + i, sy + j);
    }

    static void Marker(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = position;
    }

    // Apply to a saved stage; the terrain remains independently editable.
    public static void AddDirtGrid()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var floor = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Tilemap>())
            .Single(map => map.name == "02 Arena and Entrance");
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/2.Sprites/border.png").OfType<Sprite>().Single();
        const string tilePath = Folder + "/Tiles/DirtGridBorder.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }
        tile.name = "Dirt Grid Border";
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        // Account for the supplied sprite pivot without changing its import settings.
        tile.transform = Matrix4x4.Translate(new Vector3(
            (sprite.pivot.x / sprite.rect.width - .5f) * sprite.rect.width / sprite.pixelsPerUnit,
            (sprite.pivot.y / sprite.rect.height - .5f) * sprite.rect.height / sprite.pixelsPerUnit, 0));
        EditorUtility.SetDirty(tile);
        material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/MapUnlit.mat");
        var parent = floor.transform.parent.parent;
        var existing = parent.Find("Grid - 32px Border Cells");
        var grid = existing != null ? existing.gameObject : new GameObject("Grid - 32px Border Cells", typeof(Grid));
        grid.transform.SetParent(parent, false);
        grid.GetComponent<Grid>().cellSize = Vector3.one;
        var overlay = grid.GetComponentInChildren<Tilemap>();
        if (overlay == null) overlay = Layer(grid.transform, "05 Dirt Grid Overlay", 4);
        overlay.ClearAllTiles();
        // Each 32px border cell covers four of the existing 16px terrain tiles.
        int count = 0;
        for (int y = Mathf.FloorToInt(floor.cellBounds.yMin / 2f); y < Mathf.CeilToInt(floor.cellBounds.yMax / 2f); y++)
            for (int x = Mathf.FloorToInt(floor.cellBounds.xMin / 2f); x < Mathf.CeilToInt(floor.cellBounds.xMax / 2f); x++)
            {
                bool covered = true;
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                        covered &= floor.HasTile(new Vector3Int(x * 2 + dx, y * 2 + dy, 0));
                if (!covered) continue;
                overlay.SetTile(new Vector3Int(x, y, 0), tile);
                count++;
            }
        if (count == 0) throw new InvalidOperationException("No dirt grid cells were placed.");
        overlay.CompressBounds();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        RenderPreview(scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>()).Single());
        Debug.Log($"DIRT_GRID_OK: {count} border cells on a separate Tilemap.");
    }

    public static void Validate()
    {
        ValidatePalette();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateStage(scene);
        RenderPreview(Camera.main);
    }

    static void ValidateStage(UnityEngine.SceneManagement.Scene scene)
    {
        var maps = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Tilemap>()).ToArray();
        if ((maps.Length != 4 && maps.Length != 5) || maps.Any(m => m.GetUsedTilesCount() == 0))
            throw new InvalidOperationException("Stage layers missing or empty.");
        foreach (var layer in maps)
            foreach (var position in layer.cellBounds.allPositionsWithin)
                if (layer.HasTile(position) && layer.GetSprite(position) == null)
                    throw new InvalidOperationException("Broken tile sprite in " + layer.name);
        Debug.Log($"MAP_VALIDATION_OK: {maps.Length} populated stage layers.");
    }

    static void ValidatePalette()
    {
        int count = 0;
        foreach (var sourcePath in Directory.GetFiles("Assets/2.Sprites/Map", "*.png"))
        {
        var source = sourcePath.Replace('\\', '/');
        var path = source == Source ? PalettePath : Folder + "/" + Path.GetFileNameWithoutExtension(source) + ".prefab";
        var palette = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (palette == null || !AssetDatabase.LoadAllAssetsAtPath(path).OfType<GridPalette>().Any())
            throw new InvalidOperationException("Palette metadata missing.");
        var map = palette.GetComponentInChildren<Tilemap>();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>().ToArray();
        int topRow = sprites.Max(s => (int)s.rect.y / 16);
        foreach (var sprite in sprites)
        {
            var position = new Vector3Int((int)sprite.rect.x / 16, (int)sprite.rect.y / 16 - topRow, 0);
            if (map.GetSprite(position) != sprite) throw new InvalidOperationException("Palette layout mismatch: " + sprite.name);
            count++;
        }
        }
        Debug.Log($"MAP_PALETTE_OK: {count} palette sprites match their source coordinates.");
    }

    static void RenderPreview(Camera camera)
    {
        var target = new RenderTexture(1280, 720, 24) { filterMode = FilterMode.Point };
        camera.targetTexture = target;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Docs");
        File.WriteAllBytes("Docs/TestStage-preview.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
    }
}
