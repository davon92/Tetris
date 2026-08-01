using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 40; // internal
    [SerializeField] private int visibleHeight = 20;
    private bool[,] occupiedCells;
    
    private void Awake()
    {
        occupiedCells = new bool[width, height];
    }
    
    public bool IsInsideBoard(Vector2Int cell)
    {
        return cell.x >= 0 &&
               cell.x < width &&
               cell.y >= 0 &&
               cell.y < height;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        return occupiedCells[cell.x, cell.y];
    }

    public bool IsValidPosition(TetriminoPiece piece, Vector2Int boardPosition)
    {
        foreach (Vector2Int cell in piece.Cells)
        {
            Vector2Int occupiedCell = boardPosition + cell;

            if (!IsInsideBoard(occupiedCell))
                return false;
            if(IsCellOccupied(occupiedCell))
                return false;
        }

        return true;
    }

    public void LockPiece(TetriminoPiece piece, Vector2Int position)
    {
        
    }
}