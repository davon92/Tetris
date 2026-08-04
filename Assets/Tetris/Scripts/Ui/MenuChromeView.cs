using UnityEngine;

/// <summary>
/// The framing shared by every menu page: backdrop, drifting tetrominoes,
/// wordmark and footer hint. Pages draw their own contents inside the panel
/// rect this returns.
/// </summary>
public static class MenuChromeView
{
    public const string Wordmark = "MAGICAL BLOCK ADVENTURE";
    public const string Tagline = "A STORY-DRIVEN PUZZLE BATTLE";

    public const string ListHint =
        "ARROWS / W S TO CHOOSE    •    ENTER / A TO CONFIRM    •    MOUSE SUPPORTED";

    public const string RosterHint =
        "LEFT / RIGHT TO CHOOSE    •    ENTER / A TO CONFIRM    •    ESC / B BACK";

    public const string ConfirmHint =
        "LEFT / RIGHT TO CHOOSE    •    ENTER / A TO CONFIRM    •    ESC / B CANCEL";

    private static readonly Rect NarrowPanel = new Rect(142f, 112f, 356f, 316f);
    private static readonly Rect WidePanel = new Rect(28f, 105f, 584f, 338f);

    public static Rect DrawFrame(RetroTheme theme, bool wide)
    {
        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.Backdrop);
        RetroGui.Fill(new Rect(12f, 12f, 616f, 456f), RetroPalette.BackdropFrame);
        RetroGui.Fill(new Rect(15f, 15f, 610f, 450f), RetroPalette.Backdrop);

        int bob = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 2f) * 2f);
        RetroGui.Tetromino(TetriminoType.T, new Vector2(72f, 90f + bob), 18f, 0, 0.9f);
        RetroGui.Tetromino(TetriminoType.I, new Vector2(552f, 85f - bob), 16f, 1, 0.85f);
        RetroGui.Tetromino(TetriminoType.L, new Vector2(86f, 384f - bob), 14f, 3, 0.65f);
        RetroGui.Tetromino(TetriminoType.S, new Vector2(548f, 386f + bob), 14f, 0, 0.65f);

        GUI.Label(new Rect(70f, 31f, 500f, 44f), Wordmark, theme.MenuTitle);
        GUI.Label(new Rect(70f, 73f, 500f, 22f), Tagline, theme.MenuSubtitle);

        Rect panel = wide ? WidePanel : NarrowPanel;
        GUI.DrawTexture(panel, theme.PanelBackground);
        RetroGui.Border(panel, RetroPalette.GoldFrame, 2f);
        return panel;
    }

    public static void DrawFooter(RetroTheme theme, string hint)
    {
        GUI.Label(new Rect(80f, 444f, 480f, 20f), hint, theme.MenuFooter);
    }
}
