using System;
using UnityEngine;

public enum CharacterSelectStage
{
    PlayerOne,
    PlayerTwo
}

public enum CharacterSelectIntent
{
    /// <summary>Stay on the screen; the cursor or a message changed.</summary>
    None,

    /// <summary>Player one locked in; the screen now waits on the second pick.</summary>
    AwaitingSecondPick,

    /// <summary>Both champions are chosen and the match can start.</summary>
    Ready,

    /// <summary>Back was pressed on the first pick; leave the screen.</summary>
    Leave
}

/// <summary>
/// Cursor and confirmation rules for the versus character-select screen.
/// Pure C# apart from the roster lookup, so the branch table is unit-testable.
/// </summary>
public sealed class CharacterSelectModel
{
    public const string LockedMessage = "LOCKED  •  WIN ADVENTURES TO UNLOCK";

    public CharacterSelectStage Stage { get; private set; }
    public int Cursor { get; private set; }
    public int PlayerOneIndex { get; private set; }
    public int PlayerTwoIndex { get; private set; } = 1;
    public string Message { get; private set; } = string.Empty;
    public TetrisGameMode VersusMode { get; private set; } = TetrisGameMode.VersusCpu;

    public void Begin(TetrisGameMode versusMode)
    {
        if (versusMode != TetrisGameMode.VersusCpu && versusMode != TetrisGameMode.LocalVersus)
        {
            throw new ArgumentException(
                "Character select is only available for versus modes.",
                nameof(versusMode));
        }

        VersusMode = versusMode;
        Stage = CharacterSelectStage.PlayerOne;
        Cursor = Mathf.Clamp(PlayerOneIndex, 0, BattleCharacterRoster.Count - 1);
        Message = string.Empty;
    }

    public void Move(int direction)
    {
        int count = BattleCharacterRoster.Count;
        Cursor = (Cursor + direction % count + count) % count;
        Message = string.Empty;
    }

    public void MoveTo(int index)
    {
        Cursor = Mathf.Clamp(index, 0, BattleCharacterRoster.Count - 1);
        Message = string.Empty;
    }

    public CharacterSelectIntent Confirm()
    {
        if (!BattleCharacterRoster.IsUnlocked(Cursor))
        {
            Message = LockedMessage;
            return CharacterSelectIntent.None;
        }

        Message = string.Empty;

        if (Stage == CharacterSelectStage.PlayerOne)
        {
            PlayerOneIndex = Cursor;
            Stage = CharacterSelectStage.PlayerTwo;
            // Default the opponent to someone other than the first pick.
            Cursor = PlayerOneIndex == 1 ? 0 : 1;
            return CharacterSelectIntent.AwaitingSecondPick;
        }

        PlayerTwoIndex = Cursor;
        return CharacterSelectIntent.Ready;
    }

    public CharacterSelectIntent Back()
    {
        Message = string.Empty;
        if (Stage == CharacterSelectStage.PlayerOne)
            return CharacterSelectIntent.Leave;

        Stage = CharacterSelectStage.PlayerOne;
        Cursor = PlayerOneIndex;
        return CharacterSelectIntent.None;
    }
}
