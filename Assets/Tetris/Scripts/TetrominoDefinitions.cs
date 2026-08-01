using System.Collections.Generic;
using UnityEngine;

public static class TetrominoDefinitions
{
    private static readonly Dictionary<TetriminoType, Vector2Int[]> SpawnCells = new()
    {
        {
            TetriminoType.I,
            new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0)
            }
        },
        {
            TetriminoType.O,
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            }
        },
        {
            TetriminoType.T,
            new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1)
            }
        },
        {
            TetriminoType.J,
            new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 1)
            }
        },
        {
            TetriminoType.L,
            new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1)
            }
        },
        {
            TetriminoType.S,
            new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            }
        },
        {
            TetriminoType.Z,
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int(0, 1)
            }
        }
    };

    private static readonly Color[] Colors =
    {
        new Color(0.15f, 0.9f, 0.95f),
        new Color(1f, 0.85f, 0.15f),
        new Color(0.68f, 0.3f, 0.92f),
        new Color(0.2f, 0.42f, 0.95f),
        new Color(1f, 0.52f, 0.12f),
        new Color(0.3f, 0.88f, 0.35f),
        new Color(0.95f, 0.2f, 0.28f)
    };

    public static Vector2Int[] GetCells(TetriminoType type, int rotation)
    {
        int normalizedRotation = NormalizeRotation(rotation);
        if (type == TetriminoType.I)
            return GetIPieceCells(normalizedRotation);

        Vector2Int[] source = SpawnCells[type];
        Vector2Int[] result = new Vector2Int[source.Length];

        int turns = type == TetriminoType.O ? 0 : normalizedRotation;
        for (int i = 0; i < source.Length; i++)
        {
            Vector2Int cell = source[i];
            for (int turn = 0; turn < turns; turn++)
                cell = new Vector2Int(cell.y, -cell.x);

            result[i] = cell;
        }

        return result;
    }

    private static Vector2Int[] GetIPieceCells(int rotation)
    {
        return rotation switch
        {
            0 => new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0)
            },
            1 => new[]
            {
                new Vector2Int(1, 1),
                new Vector2Int(1, 0),
                new Vector2Int(1, -1),
                new Vector2Int(1, -2)
            },
            2 => new[]
            {
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
                new Vector2Int(2, -1)
            },
            _ => new[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, -2)
            }
        };
    }

    public static Color GetColor(TetriminoType type)
    {
        return Colors[(int)type];
    }

    public static Color GetColor(int cellValue)
    {
        if (cellValue >= 1 && cellValue <= Colors.Length)
            return Colors[cellValue - 1];

        return new Color(0.48f, 0.52f, 0.6f);
    }

    public static int NormalizeRotation(int rotation)
    {
        int normalized = rotation % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }
}
