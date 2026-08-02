using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The roster the game reads. It prefers the authored
/// <see cref="BattleCharacterLibrary"/> asset in Resources; with no asset it
/// synthesizes the shipped fighters in memory, so a fresh clone still boots and
/// a designer can bake the defaults to assets and start tuning from there.
/// </summary>
public static class BattleCharacterRoster
{
    private const string UnlockKeyPrefix = "battle-character-unlocked-";

    private static BattleCharacter[] characters;

    public static int Count => Resolve().Length;

    public static BattleCharacter Get(int index)
    {
        BattleCharacter[] roster = Resolve();
        if (index < 0 || index >= roster.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return roster[index];
    }

    /// <summary>
    /// Drops the cached roster so the next read picks the library up again.
    /// The editor baker calls this after writing assets.
    /// </summary>
    public static void Invalidate()
    {
        characters = null;
    }

    public static int FindIndex(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return -1;

        BattleCharacter[] roster = Resolve();
        for (int i = 0; i < roster.Length; i++)
        {
            if (string.Equals(roster[i].Id, characterId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public static bool IsUnlocked(int index)
    {
        BattleCharacter character = Get(index);
        return character.UnlockedByDefault ||
               PlayerPrefs.GetInt(UnlockKeyPrefix + character.Id, 0) == 1;
    }

    public static bool Unlock(string characterId)
    {
        int index = FindIndex(characterId);
        if (index < 0)
            return false;

        PlayerPrefs.SetInt(UnlockKeyPrefix + Get(index).Id, 1);
        PlayerPrefs.Save();
        return true;
    }

    private static BattleCharacter[] Resolve()
    {
        // A destroyed asset compares equal to null, so this also recovers from
        // a domain reload that left the cache holding dead references.
        if (characters != null && characters.Length > 0 && characters[0] != null)
            return characters;

        characters = LoadAuthored() ?? BuildDefaults();
        return characters;
    }

    private static BattleCharacter[] LoadAuthored()
    {
        BattleCharacterLibrary library =
            Resources.Load<BattleCharacterLibrary>(BattleCharacterLibrary.ResourceName);
        if (library == null)
            return null;

        List<BattleCharacter> authored = new();
        foreach (BattleCharacter character in library.Characters)
        {
            if (character != null)
                authored.Add(character);
        }

        if (authored.Count == 0)
        {
            Debug.LogWarning(
                $"{BattleCharacterLibrary.ResourceName} has no characters assigned; " +
                "falling back to the built-in roster.");
            return null;
        }

        return authored.ToArray();
    }

    /// <summary>
    /// The shipped roster, built in memory. This is also what
    /// <c>Tetris/Bake Default Battle Content</c> writes out as assets.
    /// </summary>
    public static BattleCharacter[] BuildDefaults()
    {
        MagicAbilityDefinition lightning =
            MagicAbilityDefinition.CreateBuiltIn(BuiltInAbility.Lightning);
        MagicAbilityDefinition starburst =
            MagicAbilityDefinition.CreateBuiltIn(BuiltInAbility.Starburst);
        MagicAbilityDefinition mending =
            MagicAbilityDefinition.CreateBuiltIn(BuiltInAbility.MendingLight);

        return new[]
        {
            BattleCharacter.Create(
                "lyra", "LYRA", "STAR-MAGE COURIER",
                BattleCharacterPortrait.Lyra,
                new Color(0.95f, 0.48f, 0.86f),
                unlockedByDefault: true,
                startingGarbage: 0,
                manaCapacity: 100,
                starburst, mending),
            BattleCharacter.Create(
                "bram", "BRAM", "STORM-MAGE RIVAL",
                BattleCharacterPortrait.Bram,
                new Color(0.3f, 0.85f, 1f),
                unlockedByDefault: true,
                startingGarbage: 0,
                manaCapacity: 100,
                lightning, mending),
            CreateLocked("hidden-01", lightning, mending),
            CreateLocked("hidden-02", starburst, mending),
            CreateLocked("hidden-03", lightning, mending),
            CreateLocked("hidden-04", starburst, mending)
        };
    }

    private static BattleCharacter CreateLocked(
        string id,
        MagicAbilityDefinition offensive,
        MagicAbilityDefinition defensive)
    {
        return BattleCharacter.Create(
            id, "???", "WIN ADVENTURES TO UNLOCK",
            BattleCharacterPortrait.Locked,
            new Color(0.56f, 0.48f, 0.78f),
            unlockedByDefault: false,
            startingGarbage: 0,
            manaCapacity: 100,
            offensive, defensive);
    }
}
