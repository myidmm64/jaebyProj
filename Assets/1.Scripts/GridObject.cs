using UnityEngine;

public class GridObject : MonoBehaviour
{
    [SerializeField] Vector2Int cell;

    public Stage Stage { get; private set; }
    public Vector2Int Cell => cell;

    public virtual void Init(Stage stage, Vector2Int startCell)
    {
        if (stage == null) throw new System.ArgumentNullException(nameof(stage));
        if (!stage.IsCellEnabled(startCell))
            throw new System.ArgumentException("GridObject requires an enabled stage cell.", nameof(startCell));
        Stage = stage;
        SetCell(startCell);
    }

    public void SetCell(Vector2Int nextCell)
    {
        if (Stage == null) throw new System.InvalidOperationException("Initialize GridObject before setting its cell.");
        if (!Stage.IsCellEnabled(nextCell))
            throw new System.ArgumentException("Cannot place GridObject outside the enabled grid.", nameof(nextCell));
        transform.position = Stage.GetCellCenterWorld(nextCell);
        cell = nextCell;
    }
}
