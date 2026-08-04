using UnityEngine;

/// <summary>
/// Visual-novel presentation for a <see cref="StoryDirector"/>. Returns the
/// choice index the mouse activated, or <see cref="NoClick"/>.
/// </summary>
public static class StoryView
{
    public const int NoClick = -1;
    private const int ActionButtonFontSize = 14;
    private const float NameplateWidth = 154f;

    public static int Draw(
        StoryDirector story,
        CpuDifficulty difficulty,
        RetroTheme theme,
        BattleArtLibrary art)
    {
        DrawBackdrop(art);

        RetroGui.Panel(
            new Rect(172f, 13f, 296f, 28f),
            RetroPalette.LocationFill,
            RetroPalette.Gold);
        GUI.Label(new Rect(172f, 13f, 296f, 28f), story.Script.LocationTitle, theme.StoryLocation);
        GUI.Label(new Rect(468f, 15f, 160f, 24f), "ESC / START  •  MENU", theme.StoryPrompt);

        StoryLine line = story.CurrentLine;

        int clicked = NoClick;
        if (story.HasChoices)
            clicked = DrawChoices(story, theme);

        int dialogueClick = DrawDialoguePanel(story, line, difficulty, theme);
        return dialogueClick != NoClick ? dialogueClick : clicked;
    }

    private static void DrawBackdrop(BattleArtLibrary art)
    {
        if (art.StoryBackdrop != null)
            GUI.DrawTexture(RetroGui.CanvasRect, art.StoryBackdrop, ScaleMode.ScaleAndCrop);
        else
            RetroGui.Fill(RetroGui.CanvasRect, new Color(0.025f, 0.035f, 0.09f));

        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.StoryScrim);
    }

    private static int DrawChoices(StoryDirector story, RetroTheme theme)
    {
        RetroGui.Panel(
            new Rect(164f, 193f, 312f, 112f),
            RetroPalette.PanelFillGlass,
            RetroPalette.BorderCyan);

        int clicked = NoClick;
        for (int i = 0; i < story.ChoiceLabels.Count && i < 2; i++)
        {
            Rect rect = new Rect(180f, 207f + i * 48f, 280f, 36f);
            if (DrawActionButton(rect, story.ChoiceLabels[i], i, story.Selection, theme))
                clicked = i;
        }

        return clicked;
    }

    private static int DrawDialoguePanel(
        StoryDirector story,
        StoryLine line,
        CpuDifficulty difficulty,
        RetroTheme theme)
    {
        RetroGui.Panel(
            new Rect(28f, 327f, 584f, 132f),
            RetroPalette.PanelFillDeep,
            RetroPalette.Gold,
            2f);

        DrawNameplate(line, theme);
        GUI.Label(new Rect(50f, 345f, 540f, 58f), line.Text, theme.StoryDialogue);

        if (story.Beat == StoryBeat.Result)
        {
            int clicked = NoClick;
            if (DrawActionButton(
                    new Rect(84f, 411f, 218f, 34f),
                    story.Script.GetResultPrimaryLabel(story.BattleWon),
                    0,
                    story.Selection,
                    theme))
                clicked = 0;

            if (DrawActionButton(
                    new Rect(338f, 411f, 218f, 34f),
                    story.Script.ResultSecondaryLabel,
                    1,
                    story.Selection,
                    theme))
                clicked = 1;

            return clicked;
        }

        if (story.Beat != StoryBeat.Choice)
        {
            GUI.Label(
                new Rect(360f, 421f, 225f, 22f),
                story.Beat == StoryBeat.Challenge
                    ? $"ENTER BATTLE  •  CPU {MainMenuView.Describe(difficulty)}"
                    : "ENTER / SPACE TO CONTINUE  ▶",
                theme.StoryPrompt);
        }

        return NoClick;
    }

    private static void DrawNameplate(StoryLine line, RetroTheme theme)
    {
        float nameplateX = line.Focus < 0
            ? 45f
            : line.Focus > 0
                ? RetroGui.CanvasWidth - 45f - NameplateWidth
                : (RetroGui.CanvasWidth - NameplateWidth) * 0.5f;

        Color fill = line.Focus < 0
            ? RetroPalette.NameplateLyra
            : line.Focus > 0
                ? RetroPalette.NameplateBram
                : RetroPalette.NameplateNeutral;

        Rect nameplate = new Rect(nameplateX, 309f, NameplateWidth, 31f);
        RetroGui.Panel(nameplate, fill, RetroPalette.Gold);
        GUI.Label(nameplate, line.Speaker, theme.StoryName);
    }

    private static bool DrawActionButton(
        Rect rect,
        string text,
        int index,
        int selection,
        RetroTheme theme)
    {
        return theme.Button(rect, text, index == selection, ActionButtonFontSize);
    }
}
