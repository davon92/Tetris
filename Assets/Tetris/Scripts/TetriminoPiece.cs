using System;
using System.Linq;
using UnityEngine;

public class TetriminoPiece : MonoBehaviour
{
    [SerializeField] private TetriminoData data;
    [SerializeField] private Transform[] blocks;

    private bool runtimeConfigured;
    private TetriminoType runtimeType;
    private Grid runtimeGrid;

    public TetriminoType Type =>
        runtimeConfigured ? runtimeType : data != null ? data.type : default;

    public Vector2Int[] Cells => TetrominoDefinitions.GetCells(Type, Rotation);
    public Vector2Int BoardPosition { get; private set; }
    public int Rotation { get; private set; }

    public void ConfigureRuntime(
        TetriminoType type,
        int sortingOrder,
        float alpha = 1f,
        Grid grid = null)
    {
        runtimeConfigured = true;
        runtimeType = type;
        runtimeGrid = grid;
        EnsureBlockReferences();

        foreach (SpriteRenderer renderer in GetVisualRenderers())
        {
            renderer.sortingOrder = sortingOrder;
            Color color = renderer.color;
            renderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        RefreshVisuals();
    }

    public void SetState(Vector2Int position, int rotation)
    {
        BoardPosition = position;
        Rotation = TetrominoDefinitions.NormalizeRotation(rotation);
        transform.localPosition = runtimeGrid != null
            ? runtimeGrid.GetCellCenterLocal(new Vector3Int(position.x, position.y, 0))
            : new Vector3(position.x + 0.5f, position.y + 0.5f, 0f);
        RefreshVisuals();
    }

    public void SetBoardPosition(Vector2Int position)
    {
        SetState(position, Rotation);
    }

    public SpriteRenderer[] GetVisualRenderers()
    {
        EnsureBlockReferences();
        return blocks
            .Where(block => block != null)
            .Select(block => block.GetComponent<SpriteRenderer>())
            .Where(renderer => renderer != null)
            .ToArray();
    }

    public void SyncSerializedSrsData()
    {
        data ??= new TetriminoData();
        data.cells = TetrominoDefinitions.GetCells(data.type, 0);
        runtimeConfigured = false;
        Rotation = 0;
        EnsureBlockReferences();
        RefreshVisuals();
    }

    private void OnValidate()
    {
        if (data != null && (data.cells == null || data.cells.Length != 4))
            data.cells = TetrominoDefinitions.GetCells(data.type, 0);

        EnsureBlockReferences();
        RefreshVisuals();
    }

    private void EnsureBlockReferences()
    {
        if (blocks != null && blocks.Length == 4 && blocks.All(block => block != null))
            return;

        blocks = GetComponentsInChildren<SpriteRenderer>(true)
            .Take(4)
            .Select(renderer => renderer.transform)
            .ToArray();
    }

    private void RefreshVisuals()
    {
        if (blocks == null)
            return;

        Vector2Int[] cells = Cells;
        int count = Mathf.Min(cells.Length, blocks.Length);
        for (int i = 0; i < count; i++)
        {
            if (blocks[i] == null)
                continue;

            Vector2Int cell = cells[i];
            blocks[i].localPosition = runtimeGrid != null
                ? runtimeGrid.CellToLocal(new Vector3Int(cell.x, cell.y, 0))
                : new Vector3(cell.x, cell.y, 0f);
        }
    }
}
