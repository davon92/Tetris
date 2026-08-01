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

        int clicked = model.Page == MainMenuPage.Root
            ? DrawRootPage(model, theme)
            : DrawDifficultyPage(model, theme);

        MenuChromeView.DrawFooter(theme, MenuChromeView.ListHint);
        return clicked;
    }

    private static int DrawRootPage(MainMenuModel model, RetroTheme theme)
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "CHOOSE YOUR MODE", theme.MenuHeading);

        int clicked = NoClick;
        if (DrawItem(theme, new Rect(178f, 163f, 284f, 50f),
                "STORY MODE", "Begin the first adventure battle", 0, model.Selection))
            clicked = 0;

        if (DrawItem(theme, new Rect(178f, 247f, 284f, 50f),
                "VS CPU",
                $"Choose a rival difficulty  •  Current: {Describe(model.Difficulty)}",
                1, model.Selection))
            clicked = 1;

        if (DrawItem(theme, new Rect(178f, 331f, 284f, 50f),
                "VS PLAYER", "Two players on one screen", 2, model.Selection))
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
