using UnityEngine;

/// <summary>
/// Renders <see cref="MainMenuModel"/>. Returns the index the mouse activated
/// so the screen keeps every decision; the view itself changes no state.
/// </summary>
public static class MainMenuView
{
    public const int NoClick = -1;

    public static int Draw(MainMenuModel model, RetroTheme theme)
    {
        MenuChromeView.DrawFrame(theme, wide: false);

        int clicked = model.Page switch
        {
            MainMenuPage.SoloMode => DrawSoloModePage(model, theme),
            MainMenuPage.VersusMode => DrawVersusModePage(model, theme),
            MainMenuPage.CpuDifficulty => DrawDifficultyPage(model, theme),
            _ => DrawRootPage(model, theme)
        };

        MenuChromeView.DrawFooter(theme, MenuChromeView.ListHint);
        return clicked;
    }

    private static int DrawRootPage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 124f, 320f, 26f), "CHOOSE YOUR MODE", theme.MenuHeading);

        // Back to four rows now that both versus modes live behind one item, so
        // the 42px/66px pitch fits every detail line clear of the row beneath.
        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 152f, 284f, 42f),
                "STORY MODE", "Begin the first adventure battle",
                MainMenuModel.StoryRow, model.Selection))
            clicked = MainMenuModel.StoryRow;

        if (DrawItem(theme, new Rect(178f, 218f, 284f, 42f),
                "SOLO", "Race the clock or play endless",
                MainMenuModel.SoloRow, model.Selection))
            clicked = MainMenuModel.SoloRow;

        if (DrawItem(theme, new Rect(178f, 284f, 284f, 42f),
                "VERSUS", "Fight a rival or a second player",
                MainMenuModel.VersusRow, model.Selection))
            clicked = MainMenuModel.VersusRow;

        if (DrawItem(theme, new Rect(178f, 350f, 284f, 42f),
                "OPTIONS", "Audio, graphics and controls",
                MainMenuModel.OptionsRow, model.Selection))
            clicked = MainMenuModel.OptionsRow;

        return clicked;
    }

    private static int DrawVersusModePage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "SELECT VERSUS MODE", theme.MenuHeading);

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 168f, 284f, 44f),
                "VS CPU",
                $"Fight a rival  •  Difficulty: {Describe(model.Difficulty)}",
                MainMenuModel.VersusCpuRow, model.Selection))
            clicked = MainMenuModel.VersusCpuRow;

        if (DrawItem(theme, new Rect(178f, 240f, 284f, 44f),
                "VS PLAYER", "Two players on one screen",
                MainMenuModel.VersusPlayerRow, model.Selection))
            clicked = MainMenuModel.VersusPlayerRow;

        if (DrawItem(theme, new Rect(178f, 344f, 284f, 38f),
                "BACK", string.Empty, 2, model.Selection))
            clicked = 2;

        return clicked;
    }

    private static int DrawSoloModePage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "SELECT SOLO MODE", theme.MenuHeading);

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 168f, 284f, 44f),
                "SPRINT",
                $"Clear {SoloRun.DefaultSprintTarget} lines as fast as you can",
                0, model.Selection))
            clicked = 0;

        if (DrawItem(theme, new Rect(178f, 240f, 284f, 44f),
                "MARATHON", "Endless play until the stack tops out", 1, model.Selection))
            clicked = 1;

        if (DrawItem(theme, new Rect(178f, 344f, 284f, 38f),
                "BACK", string.Empty, 2, model.Selection))
            clicked = 2;

        return clicked;
    }

    private static int DrawDifficultyPage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "SELECT CPU DIFFICULTY", theme.MenuHeading);

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 162f, 284f, 43f),
                "EASY", "Patient, forgiving, and imperfect", 0, model.Selection))
            clicked = 0;

        if (DrawItem(theme, new Rect(178f, 226f, 284f, 43f),
                "NORMAL", "Steadier decisions and faster play", 1, model.Selection))
            clicked = 1;

        if (DrawItem(theme, new Rect(178f, 290f, 284f, 43f),
                "HARD", "Fast, efficient placement", 2, model.Selection))
            clicked = 2;

        if (DrawItem(theme, new Rect(178f, 362f, 284f, 38f),
                "BACK", string.Empty, 3, model.Selection))
            clicked = 3;

        return clicked;
    }

    private static bool DrawItem(
        RetroTheme theme,
        Rect rect,
        string label,
        string detail,
        int index,
        int selection)
    {
        bool clicked = theme.Button(rect, label, index == selection);

        if (!string.IsNullOrEmpty(detail))
        {
            GUI.Label(
                new Rect(rect.x - 18f, rect.yMax + 3f, rect.width + 36f, 20f),
                detail,
                theme.MenuDetail);
        }

        return clicked;
    }

    public static string Describe(CpuDifficulty difficulty)
    {
        return difficulty.ToString().ToUpperInvariant();
    }
}
