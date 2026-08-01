using System;
using System.Collections.Generic;

public sealed class SevenBagRandomizer
{
    private readonly Random random;
    private readonly Queue<TetriminoType> queue = new();

    public SevenBagRandomizer(int seed)
    {
        random = new Random(seed);
        EnsureQueue(14);
    }

    public TetriminoType Next()
    {
        EnsureQueue(7);
        TetriminoType result = queue.Dequeue();
        EnsureQueue(7);
        return result;
    }

    public TetriminoType[] Peek(int count)
    {
        EnsureQueue(count);
        TetriminoType[] values = queue.ToArray();
        if (values.Length == count)
            return values;

        TetriminoType[] result = new TetriminoType[count];
        Array.Copy(values, result, count);
        return result;
    }

    private void EnsureQueue(int minimumCount)
    {
        while (queue.Count < minimumCount)
            AddBag();
    }

    private void AddBag()
    {
        List<TetriminoType> bag = new()
        {
            TetriminoType.I,
            TetriminoType.O,
            TetriminoType.T,
            TetriminoType.J,
            TetriminoType.L,
            TetriminoType.S,
            TetriminoType.Z
        };

        for (int i = bag.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (bag[i], bag[swapIndex]) = (bag[swapIndex], bag[i]);
        }

        foreach (TetriminoType type in bag)
            queue.Enqueue(type);
    }
}
