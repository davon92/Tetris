using System;
using UnityEngine;

public enum BattleCharacterPortrait
{
    Lyra,
    Bram,
    Locked
}

public readonly struct BattleCharacterDefinition
{
    public BattleCharacterDefinition(
        string id,
        string displayName,
        string title,
        BattleCharacterPortrait portrait,
        Color accent,
        bool unlockedByDefault)
    {
        Id = id;
        DisplayName = displayName;
        Title = title;
        Portrait = portrait;
        Accent = accent;
        UnlockedByDefault = unlockedByDefault;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Title { get; }
    public BattleCharacterPortrait Portrait { get; }
    public Color Accent { get; }
    public bool UnlockedByDefault { get; }
}

public static class BattleCharacterRoster
{
    private const string UnlockKeyPrefix = "battle-character-unlocked-";

    private static readonly BattleCharacterDefinition[] Characters =
    {
        new(
            "lyra",
            "LYRA",
            "STAR-MAGE COURIER",
            BattleCharacterPortrait.Lyra,
            new Color(0.95f, 0.48f, 0.86f),
            true),
        new(
            "bram",
            "BRAM",
            "STORM-MAGE RIVAL",
            BattleCharacterPortrait.Bram,
            new Color(0.3f, 0.85f, 1f),
            true),
        CreateLocked("hidden-01"),
        CreateLocked("hidden-02"),
        CreateLocked("hidden-03"),
        CreateLocked("hidden-04")
    };

    public static int Count => Characters.Length;

    public static BattleCharacterDefinition Get(int index)
    {
        if (index < 0 || index >= Characters.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return Characters[index];
    }

    public static int FindIndex(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return -1;

        for (int i = 0; i < Characters.Length; i++)
        {
            if (string.Equals(Characters[i].Id, characterId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public static bool IsUnlocked(int index)
    {
        BattleCharacterDefinition character = Get(index);
        return character.UnlockedByDefault ||
               PlayerPrefs.GetInt(UnlockKeyPrefix + character.Id, 0) == 1;
    }

    public static bool Unlock(string characterId)
    {
        int index = FindIndex(characterId);
        if (index < 0)
            return false;

        BattleCharacterDefinition character = Characters[index];
        PlayerPrefs.SetInt(UnlockKeyPrefix + character.Id, 1);
        PlayerPrefs.Save();
        return true;
    }

    private static BattleCharacterDefinition CreateLocked(string id)
    {
        return new BattleCharacterDefinition(
            id,
            "???",
            "WIN ADVENTURES TO UNLOCK",
            BattleCharacterPortrait.Locked,
            new Color(0.56f, 0.48f, 0.78f),
            false);
    }
}
