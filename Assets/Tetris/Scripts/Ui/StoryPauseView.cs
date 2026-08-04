using UnityEngine;

/// <summary>
/// The story-mode pause overlay: root menu, slot browser, and the two
/// confirmation modals. Returns the index the mouse activated on whichever
/// page is up, or <see cref="NoClick"/>; the screen decides what that means.
/// </summary>
public static class StoryPauseView
{
    public const int NoClick = -1;

    public const string Hint =
        "ARROWS TO CHOOSE    •    ENTER / A TO CONFIRM    •    ESC / B BACK";

    private static readonly Rect RootPanel = new Rect(206f, 118f, 228f, 216f);
    private static readonly Rect ModalPanel = new Rect(160f, 168f, 320f, 144f);

    private static readonly string[] RootLabels =
    {
        "RESUME",
        "SAVE",
        "LOAD",
        "RETURN TO TITLE"
    };

    public static int Draw(StoryPauseModel pause, SaveSlotCatalog catalog, RetroTheme theme, float playtimeSeconds)
    {
        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.OverlayScrim);

        switch (pause.Page)
        {
            case StoryPausePage.Root:
                return DrawRoot(pause, theme, playtimeSeconds);

            case StoryPausePage.Slots:
                return DrawSlots(pause, catalog, theme);

            case StoryPausePage.ConfirmExit:
                return DrawModal(
                    pause,
                    theme,
                    "RETURN TO TITLE?",
                    "Unsaved progress in this chapter will be lost.");

            case StoryPausePage.ConfirmOverwrite:
                return DrawModal(
                    pause,
                    theme,
                    "OVERWRITE SLOT " + SaveSlotCatalog.SlotLabel(pause.PendingSlot) + "?",
                    "The adventure stored there will be replaced.");

            default:
                return NoClick;
        }
    }

    private static int DrawRoot(StoryPauseModel pause, RetroTheme theme, float playtimeSeconds)
    {
        RetroGui.Panel(RootPanel, RetroPalette.OverlayPanel, RetroPalette.GoldFrame, 2f);

        GUI.Label(new Rect(RootPanel.x, RootPanel.y + 10f, RootPanel.width, 24f), "PAUSED", theme.MenuHeading);
        GUI.Label(
            new Rect(RootPanel.x, RootPanel.y + 34f, RootPanel.width, 16f),
            "PLAYTIME  " + SaveSlotCatalog.FormatPlaytime(playtimeSeconds),
            theme.MenuFooter);

        int clicked = NoClick;
        for (int i = 0; i < RootLabels.Length; i++)
        {
            Rect rect = new Rect(RootPanel.x + 16f, RootPanel.y + 56f + i * 38f, RootPanel.width - 32f, 32f);
            if (theme.Button(rect, RootLabels[i], i == pause.Selection, 14))
                clicked = i;
        }

        GUI.Label(new Rect(80f, 444f, 480f, 20f), Hint, theme.MenuFooter);
        return clicked;
    }

    private static int DrawSlots(StoryPauseModel pause, SaveSlotCatalog catalog, RetroTheme theme)
    {
        RetroGui.Panel(SaveSlotView.Panel, RetroPalette.OverlayPanel, RetroPalette.GoldFrame, 2f);

        string heading = pause.Slots.Mode == SaveSlotMenuMode.Save
            ? "SAVE ADVENTURE"
            : "LOAD ADVENTURE";

        int clicked = SaveSlotView.Draw(pause.Slots, catalog, theme, heading);
        GUI.Label(new Rect(80f, 444f, 480f, 20f), Hint, theme.MenuFooter);
        return clicked;
    }

    private static int DrawModal(StoryPauseModel pause, RetroTheme theme, string title, string body)
    {
        RetroGui.Panel(ModalPanel, RetroPalette.OverlayPanel, RetroPalette.GoldFrame, 2f);

        GUI.Label(new Rect(ModalPanel.x, ModalPanel.y + 14f, ModalPanel.width, 24f), title, theme.MenuHeading);
        GUI.Label(
            new Rect(ModalPanel.x + 20f, ModalPanel.y + 46f, ModalPanel.width - 40f, 34f),
            body,
            theme.MenuDetail);

        int clicked = NoClick;
        if (theme.Button(new Rect(ModalPanel.x + 24f, ModalPanel.yMax - 50f, 124f, 32f),
                "YES", pause.Selection == StoryPauseModel.ConfirmYes, 14))
            clicked = StoryPauseModel.ConfirmYes;

        if (theme.Button(new Rect(ModalPanel.xMax - 148f, ModalPanel.yMax - 50f, 124f, 32f),
                "NO", pause.Selection == StoryPauseModel.ConfirmNo, 14))
            clicked = StoryPauseModel.ConfirmNo;

        return clicked;
    }
}
