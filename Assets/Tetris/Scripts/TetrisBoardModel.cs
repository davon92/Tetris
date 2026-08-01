using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TetrisBoardModel
{
    private readonly int[,] cells;
    private readonly List<int> lastClearedRows = new();

    public int Width { get; }
    public int Height { get; }
    public int VisibleHeight { get; }
    public IReadOnlyList<int> LastClearedRows => lastClearedRows;

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

    public int Place(TetriminoType type, Vector2Int position, int rotation)
    {
        if (!IsValid(type, position, rotation))
            throw new InvalidOperationException("Cannot place a tetromino in an invalid position.");

        int cellValue = (int)type + 1;
        foreach (Vector2Int cell in TetrominoDefinitions.GetCells(type, rotation))
        {
            int x = position.x + cell.x;
            int y = position.y + cell.y;
            cells[x, y] = cellValue;
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

    private int ClearFullLines()
    {
        lastClearedRows.Clear();
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
