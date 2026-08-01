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

    public StoryDirector(IStoryScript script)
    {
        this.script = script;
    }

    public IStoryScript Script => script;

    public StoryBeat Beat { get; private set; } = StoryBeat.None;

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
        Beat = StoryBeat.Opening;
    }

    public void Cancel()
    {
        IsRunning = false;
        Beat = StoryBeat.None;
        lineIndex = 0;
        Selection = 0;
        Response = 0;
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
