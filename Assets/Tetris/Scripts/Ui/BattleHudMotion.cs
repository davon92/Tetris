using System;
using UnityEngine;

/// <summary>
/// Per-seat presentation state for the battle HUD: the displayed score chases
/// the real score, plates punch on big gains, and small chips flash when their
/// value changes. Also caches the formatted strings and the upcoming-piece
/// queue so the IMGUI draw path stays allocation-free. Owned by
/// <see cref="BattleScreen"/> so <see cref="BattleHudView"/> stays stateless.
/// </summary>
public sealed class BattleHudMotion
{
    private const float CatchUpSharpness = 9f;
    private const float PunchDuration = 0.2f;
    private const int PunchThreshold = 300;
    private const float LinesFlashDuration = 0.2f;
    private const float LevelFlashDuration = 0.3f;
    private const float HoldPopDuration = 0.15f;
    private const int UpcomingCount = 3;

    public sealed class Seat
    {
        public float DisplayedScore;
        public float PunchTimer;
        public float LinesFlashTimer;
        public float LevelFlashTimer;
        public float HoldPopTimer;

        public int LastScore;
        public int LastLines;
        public int LastLevel;
        public TetriminoType? LastHeld;

        public string ScoreText = "0000000";
        public string LinesText = "0";
        public string LevelText = "1";
        private int lastFormattedScore;

        /// <summary>0 when idle, 1 at the moment a big score gain landed.</summary>
        public float Punch => Normalized(PunchTimer, PunchDuration);

        public float LinesFlash => Normalized(LinesFlashTimer, LinesFlashDuration);
        public float LevelFlash => Normalized(LevelFlashTimer, LevelFlashDuration);
        public float HoldPop => Normalized(HoldPopTimer, HoldPopDuration);

        public int RoundedScore => Mathf.RoundToInt(DisplayedScore);

        internal void RefreshText()
        {
            int rounded = RoundedScore;
            if (rounded != lastFormattedScore || ScoreText == null)
            {
                lastFormattedScore = rounded;
                ScoreText = rounded.ToString("D7");
            }
        }

        internal void ResetText()
        {
            lastFormattedScore = 0;
            ScoreText = "0000000";
            LinesText = "0";
            LevelText = "1";
        }

        private static float Normalized(float timer, float duration)
        {
            return timer <= 0f ? 0f : timer / duration;
        }
    }

    public Seat PlayerOne { get; } = new Seat();
    public Seat PlayerTwo { get; } = new Seat();

    public BattleVitals PlayerOneVitals { get; } = new BattleVitals();
    public BattleVitals PlayerTwoVitals { get; } = new BattleVitals();

    /// <summary>Routes a session's reaction to the matching seat's face.</summary>
    public BattleVitals VitalsFor(TetrisGameSession session, MatchDirector match)
    {
        return match != null && session == match.PlayerTwo ? PlayerTwoVitals : PlayerOneVitals;
    }

    /// <summary>Upcoming pieces for the NEXT queue, nearest first. Never null.</summary>
    public TetriminoType[] Upcoming { get; private set; } = Array.Empty<TetriminoType>();

    private TetrisGameSession upcomingSourceOne;
    private TetrisGameSession upcomingSourceTwo;
    private int upcomingSerialKey = int.MinValue;

    public void Reset()
    {
        ResetSeat(PlayerOne);
        ResetSeat(PlayerTwo);
        PlayerOneVitals.Reset();
        PlayerTwoVitals.Reset();
        Upcoming = Array.Empty<TetriminoType>();
        upcomingSourceOne = null;
        upcomingSourceTwo = null;
        upcomingSerialKey = int.MinValue;
    }

    public void Tick(TetrisGameSession one, TetrisGameSession two, float deltaTime)
    {
        TickSeat(PlayerOne, one, deltaTime);
        TickSeat(PlayerTwo, two, deltaTime);
        PlayerOneVitals.Tick(one, deltaTime);
        PlayerTwoVitals.Tick(two, deltaTime);
        RefreshUpcoming(one, two);
    }

    /// <summary>
    /// The queue only changes when a piece is claimed (either player's spawn in
    /// versus advances the shared bag), so peek again only when a spawn serial
    /// or the session instances change instead of allocating every frame.
    /// </summary>
    private void RefreshUpcoming(TetrisGameSession one, TetrisGameSession two)
    {
        if (one == null)
        {
            Upcoming = Array.Empty<TetriminoType>();
            upcomingSourceOne = null;
            upcomingSourceTwo = null;
            upcomingSerialKey = int.MinValue;
            return;
        }

        int serialKey = one.PieceSerial * 4096 + (two != null ? two.PieceSerial : -1);
        bool sameSources =
            ReferenceEquals(upcomingSourceOne, one) && ReferenceEquals(upcomingSourceTwo, two);
        if (sameSources && serialKey == upcomingSerialKey)
            return;

        upcomingSourceOne = one;
        upcomingSourceTwo = two;
        upcomingSerialKey = serialKey;
        Upcoming = one.PeekUpcoming(UpcomingCount);
    }

    private static void ResetSeat(Seat seat)
    {
        seat.DisplayedScore = 0f;
        seat.PunchTimer = 0f;
        seat.LinesFlashTimer = 0f;
        seat.LevelFlashTimer = 0f;
        seat.HoldPopTimer = 0f;
        seat.LastScore = 0;
        seat.LastLines = 0;
        seat.LastLevel = 1;
        seat.LastHeld = null;
        seat.ResetText();
    }

    private static void TickSeat(Seat seat, TetrisGameSession session, float deltaTime)
    {
        if (session == null)
        {
            ResetSeat(seat);
            return;
        }

        if (session.Score != seat.LastScore)
        {
            if (session.Score - seat.LastScore >= PunchThreshold)
                seat.PunchTimer = PunchDuration;
            if (session.Score < seat.LastScore)
                seat.DisplayedScore = session.Score;
            seat.LastScore = session.Score;
        }

        if (session.Lines != seat.LastLines)
        {
            if (session.Lines > seat.LastLines)
                seat.LinesFlashTimer = LinesFlashDuration;
            seat.LastLines = session.Lines;
            seat.LinesText = session.Lines.ToString();
        }

        if (session.Level != seat.LastLevel)
        {
            if (session.Level > seat.LastLevel)
                seat.LevelFlashTimer = LevelFlashDuration;
            seat.LastLevel = session.Level;
            seat.LevelText = session.Level.ToString();
        }

        if (!Equals(session.HeldType, seat.LastHeld))
        {
            if (session.HeldType.HasValue)
                seat.HoldPopTimer = HoldPopDuration;
            seat.LastHeld = session.HeldType;
        }

        seat.PunchTimer = Mathf.Max(0f, seat.PunchTimer - deltaTime);
        seat.LinesFlashTimer = Mathf.Max(0f, seat.LinesFlashTimer - deltaTime);
        seat.LevelFlashTimer = Mathf.Max(0f, seat.LevelFlashTimer - deltaTime);
        seat.HoldPopTimer = Mathf.Max(0f, seat.HoldPopTimer - deltaTime);

        float blend = 1f - Mathf.Exp(-CatchUpSharpness * deltaTime);
        seat.DisplayedScore = Mathf.Lerp(seat.DisplayedScore, session.Score, blend);
        if (Mathf.Abs(seat.DisplayedScore - session.Score) < 1f)
            seat.DisplayedScore = session.Score;
        seat.RefreshText();
    }
}
