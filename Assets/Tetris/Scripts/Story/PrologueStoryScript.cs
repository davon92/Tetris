using System.Collections.Generic;

/// <summary>
/// The hand-authored Moon Gate prologue. Deliberately code-driven scaffolding
/// that proves the story → battle → result seam before Yarn Spinner arrives;
/// migrating means writing a new <see cref="IStoryScript"/>, nothing else.
/// </summary>
public sealed class PrologueStoryScript : IStoryScript
{
    private static readonly StoryLine[] Opening =
    {
        new StoryLine(
            "NARRATOR",
            "On the eve of the Starlight Festival, the ancient Moon Gate refuses to open.",
            0),
        new StoryLine(
            "LYRA",
            "The gate is humming in seven colors. It wants a puzzle spell—let me try.",
            -1),
        new StoryLine(
            "BRAM",
            "You? The festival trials need a steady hand, not another lucky constellation.",
            1)
    };

    private static readonly string[] Choices =
    {
        "THEN WATCH ME PROVE IT.",
        "WE COULD SOLVE IT TOGETHER."
    };

    public string ChapterId => "prologue-moon-gate";

    public string BattleId => "chapter1_opening";

    public string LocationTitle => "PROLOGUE  •  THE MOON GATE";

    public string EncounterTitle => "MOON GATE DUEL";

    public IReadOnlyList<StoryLine> OpeningLines => Opening;

    public StoryLine ChoicePrompt => new StoryLine("LYRA", "How should I answer him?", -1);

    public IReadOnlyList<string> ChoiceLabels => Choices;

    public string ResultSecondaryLabel => "RETURN TO MENU";

    public StoryLine GetChallengeLine(int choiceIndex)
    {
        return choiceIndex == 0
            ? new StoryLine("BRAM", "Bold. Win this duel and I’ll stand aside.", 1)
            : new StoryLine("BRAM", "Together? Earn my trust in one clean duel first.", 1);
    }

    public StoryLine GetResultLine(bool playerWon)
    {
        return playerWon
            ? new StoryLine("BRAM", "All right. That wasn’t luck. The Moon Gate chose you.", 1)
            : new StoryLine("LYRA", "I saw the pattern too late. I can solve it if I try again.", -1);
    }

    public string GetResultPrimaryLabel(bool playerWon)
    {
        return playerWon ? "FINISH PROLOGUE" : "REMATCH";
    }
}
