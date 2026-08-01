using System.Collections.Generic;
using UnityEngine;

public static class SrsRotationSystem
{
    private static readonly Vector2Int[] NoKick = { Vector2Int.zero };

    private static readonly Dictionary<int, Vector2Int[]> JlstzKicks = new()
    {
        { Key(0, 1), Tests((0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2)) },
        { Key(1, 0), Tests((0, 0), (1, 0), (1, -1), (0, 2), (1, 2)) },
        { Key(1, 2), Tests((0, 0), (1, 0), (1, -1), (0, 2), (1, 2)) },
        { Key(2, 1), Tests((0, 0), (-1, 0), (-1, 1), (0, -2), (-1, -2)) },
        { Key(2, 3), Tests((0, 0), (1, 0), (1, 1), (0, -2), (1, -2)) },
        { Key(3, 2), Tests((0, 0), (-1, 0), (-1, -1), (0, 2), (-1, 2)) },
        { Key(3, 0), Tests((0, 0), (-1, 0), (-1, -1), (0, 2), (-1, 2)) },
        { Key(0, 3), Tests((0, 0), (1, 0), (1, 1), (0, -2), (1, -2)) }
    };

    private static readonly Dictionary<int, Vector2Int[]> IKicks = new()
    {
        { Key(0, 1), Tests((0, 0), (-2, 0), (1, 0), (-2, -1), (1, 2)) },
        { Key(1, 0), Tests((0, 0), (2, 0), (-1, 0), (2, 1), (-1, -2)) },
        { Key(1, 2), Tests((0, 0), (-1, 0), (2, 0), (-1, 2), (2, -1)) },
        { Key(2, 1), Tests((0, 0), (1, 0), (-2, 0), (1, -2), (-2, 1)) },
        { Key(2, 3), Tests((0, 0), (2, 0), (-1, 0), (2, 1), (-1, -2)) },
        { Key(3, 2), Tests((0, 0), (-2, 0), (1, 0), (-2, -1), (1, 2)) },
        { Key(3, 0), Tests((0, 0), (1, 0), (-2, 0), (1, -2), (-2, 1)) },
        { Key(0, 3), Tests((0, 0), (-1, 0), (2, 0), (-1, 2), (2, -1)) }
    };

    public static IReadOnlyList<Vector2Int> GetKickTests(
        TetriminoType type,
        int fromRotation,
        int toRotation)
    {
        int from = TetrominoDefinitions.NormalizeRotation(fromRotation);
        int to = TetrominoDefinitions.NormalizeRotation(toRotation);
        if (type == TetriminoType.O || from == to)
            return NoKick;

        Dictionary<int, Vector2Int[]> table = type == TetriminoType.I ? IKicks : JlstzKicks;
        return table.TryGetValue(Key(from, to), out Vector2Int[] tests) ? tests : NoKick;
    }

    private static int Key(int from, int to)
    {
        return from * 4 + to;
    }

    private static Vector2Int[] Tests(params (int x, int y)[] values)
    {
        Vector2Int[] tests = new Vector2Int[values.Length];
        for (int i = 0; i < values.Length; i++)
            tests[i] = new Vector2Int(values[i].x, values[i].y);

        return tests;
    }
}
