using UnityEngine;

/// <summary>Timing knobs for the ready/start/result presentation beats.</summary>
public sealed class MatchSettings
{
    public MatchSettings(
        float readyDuration,
        float startDuration,
        float resultDuration,
        float sharedPieceCloseClaimWindow)
    {
        ReadyDuration = Mathf.Max(0f, readyDuration);
        StartDuration = Mathf.Max(0f, startDuration);
        ResultDuration = Mathf.Max(0f, resultDuration);
        SharedPieceCloseClaimWindow = sharedPieceCloseClaimWindow;
    }

    public float ReadyDuration { get; }
    public float StartDuration { get; }
    public float ResultDuration { get; }
    public float SharedPieceCloseClaimWindow { get; }
}

/// <summary>Everything needed to start one match, in one value.</summary>
public readonly struct MatchSetup
{
    public MatchSetup(
        TetrisGameMode mode,
        CpuDifficulty difficulty,
        int playerOneCharacter,
        int playerTwoCharacter,
        bool isStoryBattle,
        string encounterTitle = null,
        int soloLineTarget = 0)
    {
        Mode = mode;
        Difficulty = difficulty;
        PlayerOneCharacter = playerOneCharacter;
        PlayerTwoCharacter = playerTwoCharacter;
        IsStoryBattle = isStoryBattle;
        EncounterTitle = encounterTitle ?? string.Empty;
        SoloLineTarget = soloLineTarget;
    }

    public TetrisGameMode Mode { get; }
    public CpuDifficulty Difficulty { get; }
    public int PlayerOneCharacter { get; }
    public int PlayerTwoCharacter { get; }
    public bool IsStoryBattle { get; }

    /// <summary>Lines a sprint has to clear. 0 means the run is endless.</summary>
    public int SoloLineTarget { get; }

    /// <summary>
    /// The one-board modes. Asked as a question about the setup rather than
    /// compared against a mode everywhere, so adding a third solo variant is
    /// one edit here instead of a hunt through the director and the HUD.
    /// </summary>
    public bool IsSolo => Mode == TetrisGameMode.Marathon || Mode == TetrisGameMode.Sprint;

    /// <summary>
    /// Chapter-supplied banner. Empty falls back to a title derived from the mode,
    /// which keeps encounter copy in the story layer instead of the HUD.
    /// </summary>
    public string EncounterTitle { get; }

    /// <summary>Endless solo. The second character index is unused but harmless.</summary>
    public static MatchSetup Marathon()
    {
        return new MatchSetup(TetrisGameMode.Marathon, CpuDifficulty.Easy, 0, 1, false);
    }

    /// <summary>Solo race to a line target.</summary>
    public static MatchSetup Sprint(int lineTarget = SoloRun.DefaultSprintTarget)
    {
        return new MatchSetup(
            TetrisGameMode.Sprint, CpuDifficulty.Easy, 0, 1, false, null, lineTarget);
    }
}

/// <summary>Where a match is inside its ready → start → play → result arc.</summary>
public enum MatchPhase
{
    Idle,
    Ready,
    Start,
    Playing,
    Result,
    Finished
}
