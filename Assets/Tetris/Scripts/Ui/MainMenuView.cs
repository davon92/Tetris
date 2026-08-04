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

        // The modal owns the frame while it is up: the root list stays visible
        // underneath for context, but it is drawn disabled. Discarding its
        // return value is not enough — an enabled row would take the mouse
        // press before the modal answer covering it ever saw the event.
        bool modalActive = model.QuitConfirmActive;
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !modalActive;

        int clicked = model.Page switch
        {
            MainMenuPage.Story => DrawStoryPage(model, theme),
            MainMenuPage.SoloMode => DrawSoloModePage(model, theme),
            MainMenuPage.VersusMode => DrawVersusModePage(model, theme),
            MainMenuPage.CpuDifficulty => DrawDifficultyPage(model, theme),
            _ => DrawRootPage(model, theme)
        };

        GUI.enabled = wasEnabled;

        if (modalActive)
            clicked = DrawQuitConfirm(model, theme);

        if (!string.IsNullOrEmpty(model.Message))
            GUI.Label(new Rect(160f, 400f, 320f, 20f), model.Message, theme.Notice);

        MenuChromeView.DrawFooter(
            theme,
            model.QuitConfirmActive ? MenuChromeView.ConfirmHint : MenuChromeView.ListHint);
        return clicked;
    }

    private static int DrawRootPage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 124f, 320f, 26f), "CHOOSE YOUR MODE", theme.MenuHeading);

        // Five rows now, so the pitch drops to 58px on a 34px button: 34 + 3 +
        // the 20px detail line lands exactly on the next row's top edge. Quit
        // is the exception — it carries no detail, and that is what buys the
        // room for a fifth row inside the 316px panel.
        int selection = model.QuitConfirmActive ? MainMenuModel.QuitRow : model.Selection;

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 152f, 284f, 34f),
                "STORY MODE", "New game, or continue a saved adventure",
                MainMenuModel.StoryRow, selection))
            clicked = MainMenuModel.StoryRow;

        if (DrawItem(theme, new Rect(178f, 210f, 284f, 34f),
                "SOLO", "Race the clock or play endless",
                MainMenuModel.SoloRow, selection))
            clicked = MainMenuModel.SoloRow;

        if (DrawItem(theme, new Rect(178f, 268f, 284f, 34f),
                "VERSUS", "Fight a rival or a second player",
                MainMenuModel.VersusRow, selection))
            clicked = MainMenuModel.VersusRow;

        if (DrawItem(theme, new Rect(178f, 326f, 284f, 34f),
                "OPTIONS", "Audio, graphics and controls",
                MainMenuModel.OptionsRow, selection))
            clicked = MainMenuModel.OptionsRow;

        if (DrawItem(theme, new Rect(178f, 388f, 284f, 32f),
                "QUIT", string.Empty, MainMenuModel.QuitRow, selection))
            clicked = MainMenuModel.QuitRow;

        return clicked;
    }

    /// <summary>
    /// The confirmation modal. Yes sits left of No, and the caller has already
    /// put the cursor on No, so the destructive answer is never the default.
    /// </summary>
    private static int DrawQuitConfirm(MainMenuModel model, RetroTheme theme)
    {
        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.OverlayScrim);

        Rect panel = new Rect(196f, 186f, 248f, 128f);
        RetroGui.Panel(panel, RetroPalette.OverlayPanel, RetroPalette.Gold, 2f);

        GUI.Label(new Rect(196f, 202f, 248f, 22f), "QUIT GAME?", theme.MenuHeading);
        GUI.Label(
            new Rect(206f, 230f, 228f, 20f),
            "Are you sure you want to quit?",
            theme.MenuDetail);

        int clicked = NoClick;
        if (theme.Button(
                new Rect(216f, 258f, 92f, 36f),
                "YES",
                model.Selection == MainMenuModel.QuitYesRow))
            clicked = MainMenuModel.QuitYesRow;

        if (theme.Button(
                new Rect(332f, 258f, 92f, 36f),
                "NO",
                model.Selection == MainMenuModel.QuitNoRow))
            clicked = MainMenuModel.QuitNoRow;

        return clicked;
    }

    private static int DrawStoryPage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "STORY MODE", theme.MenuHeading);

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 168f, 284f, 44f),
                "NEW GAME", "Start the prologue from the beginning",
                MainMenuModel.NewGameRow, model.Selection))
            clicked = MainMenuModel.NewGameRow;

        if (DrawItem(theme, new Rect(178f, 240f, 284f, 44f),
                "LOAD GAME",
                model.HasAnySave
                    ? "Continue a saved adventure"
                    : "No saved adventures yet",
                MainMenuModel.LoadGameRow, model.Selection))
            clicked = MainMenuModel.LoadGameRow;

        if (DrawItem(theme, new Rect(178f, 344f, 284f, 38f),
                "BACK", string.Empty, 2, model.Selection))
            clicked = 2;

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
