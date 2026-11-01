using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Stage : MonoBehaviour
{
    [Header("Prototype Map Area")]
    [SerializeField, Min(2)] int mapSize = 8;
    [SerializeField, Min(0.01f)] float cellSize = 1f;
    [SerializeField, HideInInspector] bool[] enabledCells;
    [SerializeField, HideInInspector] int gridDataSize;
    [Header("Scene Characters")]
    [SerializeField] Player player;
    [SerializeField] Enemy enemy;

    public int MapSize => mapSize;
    public float CellSize => cellSize;
    public BoundsInt MapBounds => new BoundsInt(0, 0, 0, mapSize, mapSize, 1);
    public Player Player => player;
    public Enemy Enemy => enemy;
    public bool IsInitialized { get; private set; }

    public void Init()
    {
        IsInitialized = false;
        if (mapSize < 2 || cellSize <= 0f)
            throw new System.InvalidOperationException("Stage requires Map Size >= 2 and Cell Size > 0.");
        if (player == null || enemy == null)
            throw new System.InvalidOperationException("Assign both Player and Enemy to Stage in the Inspector.");

        SyncGridSize();
        var cells = new List<Vector2Int>();
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                if (IsCellEnabled(new Vector2Int(x, y))) cells.Add(new Vector2Int(x, y));
        if (cells.Count < 2)
            throw new System.InvalidOperationException("Enable at least two stage cells for Player and Enemy.");

        int row = mapSize / 2;
        var left = cells.OrderBy(cell => cell.x).ThenBy(cell => Mathf.Abs(cell.y - row)).First();
        var right = cells.Where(cell => cell != left).OrderByDescending(cell => cell.x)
            .ThenBy(cell => Mathf.Abs(cell.y - left.y)).First();
        player.Init(this, left);
        enemy.Init(this, right);
        IsInitialized = true;
    }

    public bool ContainsCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < mapSize && cell.y >= 0 && cell.y < mapSize;
    }

    public bool IsCellEnabled(Vector2Int cell)
    {
        if (!ContainsCell(cell)) return false;
        // Existing scenes without grid data start with every cell enabled.
        if (enabledCells == null || gridDataSize <= 0) return true;
        if (cell.x >= gridDataSize || cell.y >= gridDataSize) return true;
        int index = cell.x + cell.y * gridDataSize;
        return index >= enabledCells.Length || enabledCells[index];
    }

    void SyncGridSize()
    {
        if (gridDataSize == mapSize && enabledCells != null && enabledCells.Length == mapSize * mapSize) return;
        var resized = new bool[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                resized[x + y * mapSize] = IsCellEnabled(new Vector2Int(x, y));
        enabledCells = resized;
        gridDataSize = mapSize;
    }

    /// <summary>Cell (0,0) is bottom-left; the square map is centered on this Stage.</summary>
    public Vector3 GetCellCenterWorld(Vector2Int cell)
    {
        if (!ContainsCell(cell)) throw new System.ArgumentOutOfRangeException(nameof(cell));
        var local = new Vector3((cell.x + 0.5f - mapSize * 0.5f) * cellSize,
            (cell.y + 0.5f - mapSize * 0.5f) * cellSize, 0f);
        return transform.TransformPoint(local);
    }

    void OnValidate()
    {
        mapSize = Mathf.Max(2, mapSize);
        cellSize = Mathf.Max(0.01f, cellSize);
        SyncGridSize();
    }

    void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        float half = mapSize * cellSize * 0.5f;
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
            {
                if (IsCellEnabled(new Vector2Int(x, y))) continue;
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
                Gizmos.DrawCube(new Vector3(-half + (x + .5f) * cellSize,
                    -half + (y + .5f) * cellSize, 0), new Vector3(cellSize, cellSize, .01f));
            }
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        for (int i = 0; i <= mapSize; i++)
        {
            float offset = -half + i * cellSize;
            Gizmos.DrawLine(new Vector3(offset, -half, 0), new Vector3(offset, half, 0));
            Gizmos.DrawLine(new Vector3(-half, offset, 0), new Vector3(half, offset, 0));
        }
        Gizmos.matrix = previous;
    }
}
