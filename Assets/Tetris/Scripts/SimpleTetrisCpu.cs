using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct CpuDifficultySettings
{
    public CpuDifficultySettings(
        float actionInterval,
        float thinkDelay,
        float dropDelay,
        int placementChoices)
    {
        ActionInterval = actionInterval;
        ThinkDelay = thinkDelay;
        DropDelay = dropDelay;
        PlacementChoices = placementChoices;
    }

    public float ActionInterval { get; }
    public float ThinkDelay { get; }
    public float DropDelay { get; }
    public int PlacementChoices { get; }
}

public sealed class SimpleTetrisCpu
{
    private readonly struct Placement
    {
        public Placement(int x, int rotation, float score)
        {
            X = x;
            Rotation = rotation;
            Score = score;
        }

        public int X { get; }
        public int Rotation { get; }
        public float Score { get; }
    }

    private readonly TetrisGameSession session;
    private readonly CpuDifficultySettings settings;
    private readonly System.Random random;

    private int observedPieceSerial = -1;
    private int targetX;
    private int targetRotation;
    private float timer;
    private bool hasPlan;
    private bool waitingToDrop;

    public SimpleTetrisCpu(TetrisGameSession session, CpuDifficulty difficulty, int seed)
    {
        this.session = session;
        settings = GetSettings(difficulty);
        random = new System.Random(seed);
    }

    public static CpuDifficultySettings GetSettings(CpuDifficulty difficulty)
    {
        return difficulty switch
        {
            CpuDifficulty.Easy => new CpuDifficultySettings(
                actionInterval: 0.28f,
                thinkDelay: 0.95f,
                dropDelay: 0.5f,
                placementChoices: 8),
            CpuDifficulty.Normal => new CpuDifficultySettings(
                actionInterval: 0.16f,
                thinkDelay: 0.5f,
                dropDelay: 0.25f,
                placementChoices: 4),
            _ => new CpuDifficultySettings(
                actionInterval: 0.085f,
                thinkDelay: 0.2f,
                dropDelay: 0.08f,
                placementChoices: 1)
        };
    }

    public void Tick(float deltaTime)
    {
        if (session == null || !session.IsRunning)
            return;

        if (observedPieceSerial != session.PieceSerial)
        {
            observedPieceSerial = session.PieceSerial;
            hasPlan = FindPlacement(out targetX, out targetRotation);
            waitingToDrop = false;
            timer = WithJitter(settings.ThinkDelay, 0.2f);
        }

        if (!hasPlan)
            return;

        timer -= deltaTime;
        if (timer > 0f)
            return;

        timer = WithJitter(settings.ActionInterval, 0.15f);
        if (session.ActiveRotation != targetRotation)
        {
            session.ApplyCommand(TetrisCommand.RotateClockwise);
            return;
        }

        if (session.ActivePosition.x < targetX)
        {
            if (!session.ApplyCommand(TetrisCommand.MoveRight))
                Replan();
            return;
        }

        if (session.ActivePosition.x > targetX)
        {
            if (!session.ApplyCommand(TetrisCommand.MoveLeft))
                Replan();
            return;
        }

        if (!waitingToDrop)
        {
            waitingToDrop = true;
            timer = WithJitter(settings.DropDelay, 0.2f);
            return;
        }

        session.ApplyCommand(TetrisCommand.HardDrop);
        hasPlan = false;
        waitingToDrop = false;
    }

    private void Replan()
    {
        hasPlan = FindPlacement(out targetX, out targetRotation);
        waitingToDrop = false;
        timer = WithJitter(settings.ThinkDelay, 0.2f);
    }

    private bool FindPlacement(out int selectedX, out int selectedRotation)
    {
        selectedX = session.ActivePosition.x;
        selectedRotation = session.ActiveRotation;
        List<Placement> placements = new();

        TetrisBoardModel board = session.Model;
        for (int rotation = 0; rotation < 4; rotation++)
        {
            for (int x = -2; x < board.Width + 2; x++)
            {
                int landingY = board.FindLandingY(
                    session.ActiveType,
                    x,
                    session.ActivePosition.y,
                    rotation);

                if (landingY == int.MinValue)
                    continue;

                TetrisBoardModel simulated = board.Clone();
                int cleared = simulated.Place(
                    session.ActiveType,
                    new Vector2Int(x, landingY),
                    rotation);

                float score = Evaluate(simulated, cleared);
                placements.Add(new Placement(x, rotation, score));
            }
        }

        if (placements.Count == 0)
            return false;

        placements.Sort((left, right) => left.Score.CompareTo(right.Score));
        int choiceCount = Mathf.Min(settings.PlacementChoices, placements.Count);
        Placement selected = placements[choiceCount == 1 ? 0 : random.Next(choiceCount)];
        selectedX = selected.X;
        selectedRotation = selected.Rotation;
        return true;
    }

    private float WithJitter(float value, float fraction)
    {
        float multiplier = 1f + ((float)random.NextDouble() * 2f - 1f) * fraction;
        return value * multiplier;
    }

    private static float Evaluate(TetrisBoardModel board, int cleared)
    {
        int aggregateHeight = 0;
        int holes = 0;
        int bumpiness = 0;
        int previousHeight = 0;

        for (int x = 0; x < board.Width; x++)
        {
            int columnHeight = 0;
            bool foundBlock = false;

            for (int y = board.VisibleHeight - 1; y >= 0; y--)
            {
                if (board.GetCell(x, y) != 0)
                {
                    if (!foundBlock)
                    {
                        columnHeight = y + 1;
                        foundBlock = true;
                    }
                }
                else if (foundBlock)
                {
                    holes++;
                }
            }

            aggregateHeight += columnHeight;
            if (x > 0)
                bumpiness += Mathf.Abs(columnHeight - previousHeight);

            previousHeight = columnHeight;
        }

        return aggregateHeight * 0.52f
               + holes * 8.5f
               + bumpiness * 0.38f
               - cleared * 5.2f;
    }
}
