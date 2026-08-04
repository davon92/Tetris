using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accumulates the numbers an analytics pass will eventually want: time
/// played, matches played per mode, and player one's win/loss record per
/// character. Counters move in memory every frame and only reach the store on
/// meaningful beats (<see cref="Flush"/>), so nothing writes to disk mid-match.
/// </summary>
public sealed class GameStats
{
    public const string StoreKey = "stats";

    /// <summary>Seconds of play between background flushes.</summary>
    public const float AutoFlushInterval = 60f;

    private readonly IJsonStore store;

    private GameStatsData data = new GameStatsData();
    private bool dirty;
    private float sinceFlush;

    public GameStats(IJsonStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        Load();
    }

    public GameStatsData Data => data;

    /// <summary>
    /// Counts one launch. Separate from the constructor so tests can build a
    /// stats object without inflating the session count.
    /// </summary>
    public void BeginSession()
    {
        data.sessionsStarted++;
        dirty = true;
    }

    /// <summary>
    /// Advances the clocks. <paramref name="storyRunning"/> is false while the
    /// story is paused or not playing, which keeps the story clock honest
    /// without stopping the total.
    /// </summary>
    public void AddPlaytime(float seconds, bool storyRunning)
    {
        if (seconds <= 0f)
            return;

        data.totalPlaytimeSeconds += seconds;
        if (storyRunning)
            data.storyPlaytimeSeconds += seconds;

        dirty = true;
        sinceFlush += seconds;
        if (sinceFlush >= AutoFlushInterval)
            Flush();
    }

    /// <summary>
    /// Records a finished match. Marathon and Sprint have no opponent, so they
    /// count as played without a win or a loss and without touching a
    /// character record.
    /// </summary>
    public void RecordMatch(
        TetrisGameMode mode,
        int playerOneCharacter,
        bool playerOneWon,
        bool isStoryBattle)
    {
        data.matchesPlayed++;

        ModeStatsEntry modeEntry = ModeFor(mode);
        modeEntry.played++;

        if (IsVersus(mode))
        {
            if (playerOneWon)
                modeEntry.wins++;
            else
                modeEntry.losses++;

            RecordCharacter(playerOneCharacter, playerOneWon);
        }

        if (isStoryBattle)
        {
            if (playerOneWon)
                data.storyBattlesWon++;
            else
                data.storyBattlesLost++;
        }

        dirty = true;
        Flush();
    }

    public void RecordStorySave()
    {
        data.storySavesWritten++;
        dirty = true;
    }

    public CharacterStatsEntry GetCharacter(int rosterIndex)
    {
        return rosterIndex >= 0 && rosterIndex < BattleCharacterRoster.Count
            ? CharacterFor(BattleCharacterRoster.Get(rosterIndex).Id)
            : new CharacterStatsEntry();
    }

    public ModeStatsEntry GetMode(TetrisGameMode mode)
    {
        return ModeFor(mode);
    }

    public void Load()
    {
        data = Read() ?? new GameStatsData();
        data.characters ??= Array.Empty<CharacterStatsEntry>();
        data.modes ??= Array.Empty<ModeStatsEntry>();
        dirty = false;
        sinceFlush = 0f;
    }

    /// <summary>Writes pending counters. A no-op when nothing changed.</summary>
    public void Flush()
    {
        sinceFlush = 0f;
        if (!dirty)
            return;

        dirty = !store.Write(StoreKey, JsonUtility.ToJson(data, true));
    }

    /// <summary>Only a match with an opponent can be won or lost.</summary>
    private static bool IsVersus(TetrisGameMode mode)
    {
        return mode == TetrisGameMode.VersusCpu || mode == TetrisGameMode.LocalVersus;
    }

    private void RecordCharacter(int rosterIndex, bool won)
    {
        if (rosterIndex < 0 || rosterIndex >= BattleCharacterRoster.Count)
            return;

        CharacterStatsEntry entry = CharacterFor(BattleCharacterRoster.Get(rosterIndex).Id);
        if (won)
            entry.wins++;
        else
            entry.losses++;
    }

    private CharacterStatsEntry CharacterFor(string characterId)
    {
        for (int i = 0; i < data.characters.Length; i++)
        {
            if (string.Equals(data.characters[i].characterId, characterId, StringComparison.Ordinal))
                return data.characters[i];
        }

        CharacterStatsEntry entry = new CharacterStatsEntry { characterId = characterId };
        data.characters = Append(data.characters, entry);
        return entry;
    }

    private ModeStatsEntry ModeFor(TetrisGameMode mode)
    {
        string key = mode.ToString();
        for (int i = 0; i < data.modes.Length; i++)
        {
            if (string.Equals(data.modes[i].mode, key, StringComparison.Ordinal))
                return data.modes[i];
        }

        ModeStatsEntry entry = new ModeStatsEntry { mode = key };
        data.modes = Append(data.modes, entry);
        return entry;
    }

    private static T[] Append<T>(T[] source, T item)
    {
        List<T> list = new List<T>(source) { item };
        return list.ToArray();
    }

    private GameStatsData Read()
    {
        if (!store.TryRead(StoreKey, out string json) || string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            GameStatsData loaded = JsonUtility.FromJson<GameStatsData>(json);
            return loaded != null && loaded.version > 0 ? loaded : null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not parse the statistics document: {exception.Message}");
            return null;
        }
    }
}
