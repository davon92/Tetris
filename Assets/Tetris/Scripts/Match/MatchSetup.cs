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
        string encounterTitle = null)
    {
        Mode = mode;
        Difficulty = difficulty;
        PlayerOneCharacter = playerOneCharacter;
        PlayerTwoCharacter = playerTwoCharacter;
        IsStoryBattle = isStoryBattle;
        EncounterTitle = encounterTitle ?? string.Empty;
    }

    public TetrisGameMode Mode { get; }
    public CpuDifficulty Difficulty { get; }
    public int PlayerOneCharacter { get; }
    public int PlayerTwoCharacter { get; }
    public bool IsStoryBattle { get; }

    /// <summary>
    /// Chapter-supplied banner. Empty falls back to a title derived from the mode,
    /// which keeps encounter copy in the story layer instead of the HUD.
    /// </summary>
    public string EncounterTitle { get; }

    public static MatchSetup Solo()
    {
        return new MatchSetup(TetrisGameMode.Solo, CpuDifficulty.Easy, 0, 1, false);
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
