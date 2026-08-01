using System.Collections.Generic;

/// <summary>One authored dialogue beat.</summary>
/// <remarks>
/// <see cref="Focus"/> stages the speaker: negative is the left character,
/// positive the right, zero a neutral narrator.
/// </remarks>
public readonly struct StoryLine
{
    public StoryLine(string speaker, string text, int focus)
    {
        Speaker = speaker;
        Text = text;
        Focus = focus;
    }

    public string Speaker { get; }
    public string Text { get; }
    public int Focus { get; }

    public static StoryLine Empty => new StoryLine(string.Empty, string.Empty, 0);
}

/// <summary>
/// All authored content for one story chapter. <see cref="StoryDirector"/>
/// drives any implementation, so a Yarn Spinner-backed script can replace the
/// hand-written prologue without touching the flow code.
/// </summary>
public interface IStoryScript
{
    /// <summary>Identifier handed to <see cref="StoryBattleBridge"/>.</summary>
    string BattleId { get; }

    string LocationTitle { get; }

    /// <summary>Banner shown above the battle this chapter starts.</summary>
    string EncounterTitle { get; }

    IReadOnlyList<StoryLine> OpeningLines { get; }

    StoryLine ChoicePrompt { get; }

    IReadOnlyList<string> ChoiceLabels { get; }

    StoryLine GetChallengeLine(int choiceIndex);

    StoryLine GetResultLine(bool playerWon);

    string GetResultPrimaryLabel(bool playerWon);

    string ResultSecondaryLabel { get; }
}
