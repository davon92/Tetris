using System;

/// <summary>Win/loss record for one roster character in player one's seat.</summary>
[Serializable]
public sealed class CharacterStatsEntry
{
    public string characterId = string.Empty;
    public int wins;
    public int losses;

    public int Played => wins + losses;

    public float WinRate => Played == 0 ? 0f : (float)wins / Played;
}

/// <summary>Totals for one <see cref="TetrisGameMode"/>.</summary>
[Serializable]
public sealed class ModeStatsEntry
{
    public string mode = string.Empty;
    public int played;
    public int wins;
    public int losses;
}

/// <summary>
/// The serialised analytics document. Everything is a flat counter so new
/// fields can be appended without invalidating existing files — a missing
/// field simply deserialises to zero.
/// </summary>
[Serializable]
public sealed class GameStatsData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    /// <summary>Wall-clock seconds spent in the application across all sessions.</summary>
    public float totalPlaytimeSeconds;

    /// <summary>Seconds spent inside story mode, pauses excluded.</summary>
    public float storyPlaytimeSeconds;

    public int sessionsStarted;
    public int matchesPlayed;
    public int storyBattlesWon;
    public int storyBattlesLost;
    public int storySavesWritten;

    public CharacterStatsEntry[] characters = Array.Empty<CharacterStatsEntry>();
    public ModeStatsEntry[] modes = Array.Empty<ModeStatsEntry>();
}
