using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TetrisBoardModel
{
    private readonly int[,] cells;
    private readonly List<int> lastClearedRows = new();
    private readonly List<int> lastClearedCells = new();

    public int Width { get; }
    public int Height { get; }
    public int VisibleHeight { get; }
    public IReadOnlyList<int> LastClearedRows => lastClearedRows;

    /// <summary>
    /// The cell values the most recent clear removed, row-major over
    /// <see cref="LastClearedRows"/> — the value at <c>row * Width + x</c>.
    /// Views read it to send one particle per block in the colour that block
    /// actually was. Only valid until the next placement, so read it from the
    /// clear event rather than caching the list.
    /// </summary>
    public IReadOnlyList<int> LastClearedCells => lastClearedCells;

    public TetrisBoardModel(int width = 10, int height = 24, int visibleHeight = 20)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (visibleHeight <= 0 || height < visibleHeight)
            throw new ArgumentOutOfRangeException(nameof(visibleHeight));

        Width = width;
        Height = height;
        VisibleHeight = visibleHeight;
        cells = new int[width, height];
    }

    private TetrisBoardModel(TetrisBoardModel source)
    {
        Width = source.Width;
        Height = source.Height;
        VisibleHeight = source.VisibleHeight;
        cells = (int[,])source.cells.Clone();
        lastClearedRows.AddRange(source.lastClearedRows);
        lastClearedCells.AddRange(source.lastClearedCells);
    }

    public int GetCell(int x, int y)
    {
        return IsInside(x, y) ? cells[x, y] : 0;
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsValid(TetriminoType type, Vector2Int position, int rotation)
    {
        Vector2Int[] pieceCells = TetrominoDefinitions.GetCells(type, rotation);
        foreach (Vector2Int cell in pieceCells)
        {
            int x = position.x + cell.x;
            int y = position.y + cell.y;
            if (!IsInside(x, y) || cells[x, y] != 0)
                return false;
        }

        return true;
    }

    /// <summary>Cell value used for garbage rows.</summary>
    public const int GarbageCell = 8;

    /// <summary>Cell value for a mana cell — clearing its row pays the owner extra charge.</summary>
    public const int ManaCell = 9;

    /// <summary>
    /// How many mana cells the most recent clear removed. The charge a clear is
    /// worth scales with this, so it is a count and not a flag.
    /// </summary>
    public int LastClearManaCells { get; private set; }

    /// <summary>True when the most recent clear removed at least one mana cell.</summary>
    public bool LastClearContainedMana => LastClearManaCells > 0;

    public int Place(TetriminoType type, Vector2Int position, int rotation)
    {
        return Place(type, position, rotation, -1);
    }

    /// <summary>
    /// Places a piece; <paramref name="manaCellIndex"/> marks that index of the
    /// piece's cells as a mana cell instead of its normal color.
    /// </summary>
    public int Place(TetriminoType type, Vector2Int position, int rotation, int manaCellIndex)
    {
        if (!IsValid(type, position, rotation))
            throw new InvalidOperationException("Cannot place a tetromino in an invalid position.");

        int cellValue = (int)type + 1;
        Vector2Int[] pieceCells = TetrominoDefinitions.GetCells(type, rotation);
        for (int i = 0; i < pieceCells.Length; i++)
        {
            int x = position.x + pieceCells[i].x;
            int y = position.y + pieceCells[i].y;
            cells[x, y] = i == manaCellIndex ? ManaCell : cellValue;
        }

        return ClearFullLines();
    }

    public int FindLandingY(TetriminoType type, int x, int startY, int rotation)
    {
        Vector2Int position = new Vector2Int(x, startY);
        if (!IsValid(type, position, rotation))
            return int.MinValue;

        while (IsValid(type, position + Vector2Int.down, rotation))
            position += Vector2Int.down;

        return position.y;
    }

    public bool AddGarbage(int lineCount, Func<int, int> chooseHole)
    {
        int lines = Mathf.Clamp(lineCount, 0, Height);
        if (lines == 0)
            return false;

        bool overflowed = false;
        for (int y = Height - lines; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                overflowed |= cells[x, y] != 0;
        }

        for (int y = Height - 1; y >= lines; y--)
        {
            for (int x = 0; x < Width; x++)
                cells[x, y] = cells[x, y - lines];
        }

        for (int y = 0; y < lines; y++)
        {
            int hole = Mathf.Clamp(chooseHole(y), 0, Width - 1);
            for (int x = 0; x < Width; x++)
                cells[x, y] = x == hole ? 0 : 8;
        }

        return overflowed;
    }

    public bool HasCellsAboveVisibleHeight()
    {
        for (int y = VisibleHeight; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (cells[x, y] != 0)
                    return true;
            }
        }

        return false;
    }

    public TetrisBoardModel Clone()
    {
        return new TetrisBoardModel(this);
    }

    /// <summary>
    /// Lightning: empties whole columns top to bottom. Nothing falls, so the
    /// result is a clean vertical well only an I-piece fills comfortably.
    /// Returns the cells that were destroyed, for the effects layer.
    /// </summary>
    public List<Vector2Int> CarveColumns(int startX, int columnCount)
    {
        List<Vector2Int> destroyed = new();
        for (int x = startX; x < startX + columnCount; x++)
        {
            if (x < 0 || x >= Width)
                continue;

            for (int y = 0; y < Height; y++)
            {
                if (cells[x, y] == 0)
                    continue;

                destroyed.Add(new Vector2Int(x, y));
                cells[x, y] = 0;
            }
        }

        return destroyed;
    }

    /// <summary>
    /// Blows a tapered crater out of the stack, centered on <paramref name="centerX"/>
    /// and widest through the middle (the shipped 4x4 is the classic 2-4-4-2).
    /// Cells above the crater are deliberately left floating — the overhangs are
    /// the damage, since they seal holes that need S/Z/J/L tucks rather than a
    /// clean refill.
    /// </summary>
    public List<Vector2Int> CarveCrater(int centerX, int centerY, int width = 4, int height = 4)
    {
        width = Mathf.Max(2, width);
        height = Mathf.Max(1, height);

        List<Vector2Int> destroyed = new();
        float middle = (height - 1) * 0.5f;

        for (int row = 0; row < height; row++)
        {
            int y = centerY + height / 2 - row;
            if (y < 0 || y >= Height)
                continue;

            // Rows step in by two per whole row away from the middle, which
            // reproduces 2-4-4-2 at 4x4 and tapers sensibly at any other size.
            int taper = Mathf.FloorToInt(Mathf.Abs(row - middle));
            int rowWidth = Mathf.Max(1, width - taper * 2);
            int startX = centerX - rowWidth / 2;
            for (int x = startX; x < startX + rowWidth; x++)
            {
                if (x < 0 || x >= Width || cells[x, y] == 0)
                    continue;

                destroyed.Add(new Vector2Int(x, y));
                cells[x, y] = 0;
            }
        }

        return destroyed;
    }

    /// <summary>Highest occupied row plus one — how full the board is.</summary>
    public int GetStackHeight()
    {
        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                if (cells[x, y] != 0)
                    return y + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// Rows that are purely garbage. This is the damage the player can win
    /// back by clearing, so the HUD shows it as recoverable grey health.
    /// </summary>
    public int CountGarbageRows()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            if (IsGarbageRow(y))
                count++;
        }

        return count;
    }

    /// <summary>Column height measured from the floor, used to aim a crater.</summary>
    public int GetColumnHeight(int x)
    {
        for (int y = Height - 1; y >= 0; y--)
        {
            if (cells[x, y] != 0)
                return y + 1;
        }

        return 0;
    }

    /// <summary>
    /// The tallest column, so an attack lands where it hurts. Ties break
    /// toward the middle — otherwise a flat board always aims at column 0 and
    /// half of a wide attack falls off the edge.
    /// </summary>
    public int FindTallestColumn()
    {
        int center = Width / 2;
        int bestX = center;
        int bestHeight = -1;
        for (int x = 0; x < Width; x++)
        {
            int height = GetColumnHeight(x);
            if (height < bestHeight)
                continue;

            if (height == bestHeight &&
                Mathf.Abs(x - center) >= Mathf.Abs(bestX - center))
            {
                continue;
            }

            bestHeight = height;
            bestX = x;
        }

        return bestX;
    }

    /// <summary>
    /// Heal: dissolves whole garbage rows from the bottom up and everything
    /// above settles down. Returns how many rows were removed.
    /// </summary>
    public int MendGarbageRows(int maxRows)
    {
        int removed = 0;
        for (int y = 0; y < Height && removed < maxRows; y++)
        {
            if (!IsGarbageRow(y))
                continue;

            for (int shiftY = y; shiftY < Height - 1; shiftY++)
            {
                for (int x = 0; x < Width; x++)
                    cells[x, shiftY] = cells[x, shiftY + 1];
            }

            for (int x = 0; x < Width; x++)
                cells[x, Height - 1] = 0;

            removed++;
            y--;
        }

        return removed;
    }

    private bool IsGarbageRow(int y)
    {
        bool sawGarbage = false;
        for (int x = 0; x < Width; x++)
        {
            if (cells[x, y] == GarbageCell)
                sawGarbage = true;
            else if (cells[x, y] != 0)
                return false;
        }

        return sawGarbage;
    }

    private int ClearFullLines()
    {
        lastClearedRows.Clear();
        lastClearedCells.Clear();
        LastClearManaCells = 0;
        int writeRow = 0;
        int cleared = 0;

        for (int readRow = 0; readRow < Height; readRow++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (cells[x, readRow] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                cleared++;
                lastClearedRows.Add(readRow);
                for (int x = 0; x < Width; x++)
                {
                    lastClearedCells.Add(cells[x, readRow]);
                    if (cells[x, readRow] == ManaCell)
                        LastClearManaCells++;
                }

                continue;
            }

            if (writeRow != readRow)
            {
                for (int x = 0; x < Width; x++)
                    cells[x, writeRow] = cells[x, readRow];
            }

            writeRow++;
        }

        for (int y = writeRow; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                cells[x, y] = 0;
        }

        return cleared;
    }
}
