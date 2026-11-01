using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class StageArtBuilder
{
    const string ArtPath="Assets/4.UI/PixelLab/Iai/";
    const string TilePath="Assets/6.TilePalettes/Iai";
    [MenuItem("Tools/Explosive Wizard/Apply PixelLab Art to TestStage")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
        const string scenePath="Assets/Scenes/TestStage.unity";
        if(EditorSceneManager.GetActiveScene().path!=scenePath)
        {if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(scenePath);}
        var stage=UnityEngine.Object.FindFirstObjectByType<Stage>();if(stage==null)throw new InvalidOperationException("Stage required.");
        var art=stage.GetComponent<StageArt>();if(art==null)art=stage.gameObject.AddComponent<StageArt>();
        art.player=Sprite("Player-IaiStudent");art.slime=Sprite("Enemy-Slime");art.pebble=Sprite("Enemy-PebbleSpirit");art.panel=Sprite("UI-IaiPanel");
        art.cardFrame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.UI/Cards/CardFrame.png");art.cardStar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.UI/Cards/CardStar.png");
        if(art.legacyMaps==null||art.legacyMaps.Length==0)art.legacyMaps=UnityEngine.Object.FindObjectsByType<Grid>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(g=>g.name=="Grid - 16px Art Tiles"||g.name=="Grid - 32px Border Cells").Select(g=>g.gameObject).ToArray();
        Directory.CreateDirectory(TilePath);AssetDatabase.Refresh();
        var grass=MakeTile("MeadowGrass",Sprite("Tile-MeadowGrass"));var dirt=MakeTile("MeadowDirt",Sprite("Tile-MeadowDirt"));var stone=MakeTile("AcademyHall",Sprite("Tile-AcademyHall"));
        var border=AssetDatabase.LoadAssetAtPath<Tile>("Assets/6.TilePalettes/Tiles/DirtGridBorder.asset");
        art.meadow=GridRoot(stage,"Iai Meadow");art.hall=GridRoot(stage,"Academy Hall");
        var grassMap=Layer(art.meadow,"Grass",0);var dirtMap=Layer(art.meadow,"Arena Dirt",1);var borders=Layer(art.meadow,"Arena Grid",2);borders.color=new Color(1,1,1,.4f);
        var hallMap=Layer(art.hall,"Hall Floor",0);
        grassMap.transform.localScale=new Vector3(2,2,1);hallMap.transform.localScale=new Vector3(2,2,1);
        for(int x=-12;x<stage.MapSize+12;x++)for(int y=-10;y<stage.MapSize+10;y++)
        {var cell=new Vector3Int(x,y,0);grassMap.SetTile(cell,grass);hallMap.SetTile(cell,stone);}
        for(int x=0;x<stage.MapSize;x++)for(int y=0;y<stage.MapSize;y++)if(stage.IsCellEnabled(new Vector2Int(x,y)))
        {var cell=new Vector3Int(x,y,0);dirtMap.SetTile(cell,dirt);if(border!=null)borders.SetTile(cell,border);}
        var portal=art.hall.transform.Find("Void Entrance");if(portal==null){portal=new GameObject("Void Entrance",typeof(SpriteRenderer)).transform;portal.SetParent(art.hall.transform,false);}
        portal.localPosition=new Vector3(stage.MapSize*.5f,stage.MapSize*.5f,-.05f);portal.localScale=Vector3.one*2.1f;
        art.voidEntrance=portal.GetComponent<SpriteRenderer>();art.voidEntrance.sprite=Sprite("Object-VoidEntrance");art.voidEntrance.color=Color.white;art.voidEntrance.sortingOrder=10;
        art.Dress(stage.Player);art.Dress(stage.Enemy);EditorUtility.SetDirty(art);
        var controller=stage.GetComponent<StageCombatController>();if(controller==null)controller=stage.gameObject.AddComponent<StageCombatController>();
        foreach(var mode in new[]{CombatUIPreviewMode.HUD,CombatUIPreviewMode.Bag,CombatUIPreviewMode.TurnReward,CombatUIPreviewMode.WaveReward,CombatUIPreviewMode.Settings,CombatUIPreviewMode.Hall})controller.PreviewUI(mode);
        foreach(var old in UnityEngine.Object.FindObjectsByType<BagPanelUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))old.gameObject.SetActive(false);
        AssetDatabase.SaveAssets();EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);EditorSceneManager.SaveScene(stage.gameObject.scene);
        Debug.Log("STAGE ART APPLIED: female player, slime/pebble, meadow/dirt/hall tiles, void entrance, UI panels and portraits; saved Hall preview.");
    }
    static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+name+".png")??throw new InvalidOperationException("Missing art: "+name);
    static Tile MakeTile(string name,Sprite sprite)
    {
        var path=TilePath+"/"+name+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
        tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(tile);return tile;
    }
    static GameObject GridRoot(Stage stage,string name)
    {
        var existing=stage.transform.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(Grid));go.transform.SetParent(stage.transform,false);
        go.transform.localPosition=Vector3.one*(-stage.MapSize*stage.CellSize*.5f);go.transform.localPosition=new Vector3(go.transform.localPosition.x,go.transform.localPosition.y,0);
        go.GetComponent<Grid>().cellSize=new Vector3(stage.CellSize,stage.CellSize,1);return go;
    }
    static Tilemap Layer(GameObject root,string name,int order)
    {
        var existing=root.transform.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(root.transform,false);
        var map=go.GetComponent<Tilemap>();map.ClearAllTiles();go.GetComponent<TilemapRenderer>().sortingOrder=order;return map;
    }
}
