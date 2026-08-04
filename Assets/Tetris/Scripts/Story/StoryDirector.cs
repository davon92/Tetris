using System.Collections.Generic;
using UnityEngine;

public enum StoryBeat
{
    /// <summary>Nothing is being presented — either idle or the battle owns the screen.</summary>
    None,
    Opening,
    Choice,
    Challenge,
    Result
}

public enum StoryIntent
{
    None,
    StartBattle,
    ReturnToMenu
}

/// <summary>
/// Walks an <see cref="IStoryScript"/> through its beats and reports when the
/// chapter wants a battle or wants to hand control back to the menu.
/// Contains no Unity input, rendering, or battle knowledge.
/// </summary>
public sealed class StoryDirector
{
    private readonly IStoryScript script;
    private int lineIndex;
    private float playtimeSeconds;

    public StoryDirector(IStoryScript script)
    {
        this.script = script;
    }

    public IStoryScript Script => script;

    public StoryBeat Beat { get; private set; } = StoryBeat.None;

    /// <summary>
    /// Seconds spent inside this chapter, battles included and pauses
    /// excluded. Written into save slots and shown on the pause menu.
    /// </summary>
    public float PlaytimeSeconds => playtimeSeconds;

    /// <summary>True from <see cref="Begin"/> until <see cref="Cancel"/>, battle included.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>True while dialogue is on screen rather than a battle.</summary>
    public bool IsPresenting => Beat != StoryBeat.None;

    public int Selection { get; private set; }
    public int Response { get; private set; }
    public bool BattleWon { get; private set; }

    public bool HasChoices => Beat == StoryBeat.Choice;

    public IReadOnlyList<string> ChoiceLabels => script.ChoiceLabels;

    public StoryLine CurrentLine
    {
        get
        {
            switch (Beat)
            {
                case StoryBeat.Opening:
                    return script.OpeningLines[
                        Mathf.Clamp(lineIndex, 0, script.OpeningLines.Count - 1)];
                case StoryBeat.Choice:
                    return script.ChoicePrompt;
                case StoryBeat.Challenge:
                    return script.GetChallengeLine(Response);
                case StoryBeat.Result:
                    return script.GetResultLine(BattleWon);
                default:
                    return StoryLine.Empty;
            }
        }
    }

    public void Begin()
    {
        IsRunning = true;
        BattleWon = false;
        lineIndex = 0;
        Selection = 0;
        Response = 0;
        playtimeSeconds = 0f;
        Beat = StoryBeat.Opening;
    }

    public void Cancel()
    {
        IsRunning = false;
        Beat = StoryBeat.None;
        lineIndex = 0;
        Selection = 0;
        Response = 0;
        playtimeSeconds = 0f;
    }

    /// <summary>Accrues chapter playtime. Ignored while the chapter is idle.</summary>
    public void AddPlaytime(float seconds)
    {
        if (IsRunning && seconds > 0f)
            playtimeSeconds += seconds;
    }

    /// <summary>
    /// Snapshots the chapter for a save slot. A capture taken while a battle
    /// owns the screen is stored as the challenge beat, so loading it drops the
    /// player back at the line that started the fight rather than nowhere.
    /// </summary>
    public StoryProgress Capture()
    {
        return new StoryProgress(
            script.ChapterId,
            Beat == StoryBeat.None ? StoryBeat.Challenge : Beat,
            lineIndex,
            Response,
            Selection,
            BattleWon,
            playtimeSeconds);
    }

    /// <summary>True when this director owns the chapter the progress came from.</summary>
    public bool CanRestore(in StoryProgress progress)
    {
        return string.Equals(progress.ChapterId, script.ChapterId, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Puts the chapter back at a saved beat. Every index is clamped against
    /// the current script so an older save can never index past authored
    /// content after the chapter is edited.
    /// </summary>
    public bool Restore(in StoryProgress progress)
    {
        if (!CanRestore(progress))
            return false;

        IsRunning = true;
        Beat = progress.Beat == StoryBeat.None ? StoryBeat.Opening : progress.Beat;
        lineIndex = Mathf.Clamp(progress.LineIndex, 0, Mathf.Max(0, script.OpeningLines.Count - 1));
        Response = Mathf.Clamp(progress.Response, 0, Mathf.Max(0, script.ChoiceLabels.Count - 1));
        Selection = Mathf.Clamp(progress.Selection, 0, 1);
        BattleWon = progress.BattleWon;
        playtimeSeconds = Mathf.Max(0f, progress.PlaytimeSeconds);
        return true;
    }

    /// <summary>Hands the screen to the battle while the chapter stays running.</summary>
    public void EnterBattle()
    {
        Beat = StoryBeat.None;
    }

    public void ReportResult(bool playerWon)
    {
        BattleWon = playerWon;
        Selection = 0;
        Beat = StoryBeat.Result;
    }

    /// <summary>Two-option beats toggle; single-option beats ignore the input.</summary>
    public void MoveSelection()
    {
        if (Beat == StoryBeat.Choice || Beat == StoryBeat.Result)
            Selection = 1 - Selection;
    }

    public void SetSelection(int selection)
    {
        Selection = Mathf.Clamp(selection, 0, 1);
    }

    public StoryIntent Confirm()
    {
        switch (Beat)
        {
            case StoryBeat.Opening:
                lineIndex++;
                if (lineIndex >= script.OpeningLines.Count)
                {
                    Beat = StoryBeat.Choice;
                    Selection = 0;
                }

                return StoryIntent.None;

            case StoryBeat.Choice:
                Response = Selection;
                Beat = StoryBeat.Challenge;
                return StoryIntent.None;

            case StoryBeat.Challenge:
                return StoryIntent.StartBattle;

            case StoryBeat.Result:
                return !BattleWon && Selection == 0
                    ? StoryIntent.StartBattle
                    : StoryIntent.ReturnToMenu;

            default:
                return StoryIntent.None;
        }
    }
}
