using UnityEngine;

/// <summary>
/// Renders <see cref="OptionsModel"/>. Returns the row the mouse activated so
/// the screen keeps every decision; the view itself changes no state.
/// </summary>
public static class OptionsView
{
    public const int NoClick = -1;

    private const string ValueHint =
        "UP / DOWN TO CHOOSE    •    LEFT / RIGHT TO ADJUST    •    ESC / B BACK";

    private const string ControlsHint =
        "ENTER / A TO REBIND    •    PRESS ANY KEY OR PAD BUTTON    •    ESC / B BACK";

    public static int Draw(OptionsModel model, RetroTheme theme)
    {
        bool wide = model.Page == OptionsPage.Controls;
        Rect panel = MenuChromeView.DrawFrame(theme, wide);

        int clicked = model.Page switch
        {
            OptionsPage.Root => DrawRoot(model, theme, panel),
            OptionsPage.Audio => DrawAudio(model, theme, panel),
            OptionsPage.Graphics => DrawGraphics(model, theme, panel),
            _ => DrawControls(model, theme, panel)
        };

        MenuChromeView.DrawFooter(
            theme,
            model.Page == OptionsPage.Controls ? ControlsHint : ValueHint);

        if (model.Listening.HasValue)
            DrawCaptureOverlay(model, theme);

        return clicked;
    }

    // ----------------------------------------------------------------- pages

    private static int DrawRoot(OptionsModel model, RetroTheme theme, Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 15f, panel.width, 28f), "OPTIONS", theme.MenuHeading);

        int clicked = NoClick;
        if (Item(theme, new Rect(178f, 168f, 284f, 46f), "AUDIO",
                "Music and effect volume", 0, model.Selection))
            clicked = 0;
        if (Item(theme, new Rect(178f, 232f, 284f, 46f), "GRAPHICS",
                "Display mode, resolution and quality", 1, model.Selection))
            clicked = 1;
        if (Item(theme, new Rect(178f, 296f, 284f, 46f), "CONTROLS",
                "Rebind the keyboard and pad", 2, model.Selection))
            clicked = 2;
        if (Item(theme, new Rect(178f, 366f, 284f, 38f), "BACK",
                string.Empty, 3, model.Selection))
            clicked = 3;

        return clicked;
    }

    private static int DrawAudio(OptionsModel model, RetroTheme theme, Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 15f, panel.width, 28f), "AUDIO", theme.MenuHeading);

        int clicked = NoClick;
        if (Slider(theme, RowRect(0), "MUSIC", GameSettings.MusicVolume, 0, model.Selection))
            clicked = 0;
        if (Slider(theme, RowRect(1), "EFFECTS", GameSettings.SfxVolume, 1, model.Selection))
            clicked = 1;
        if (Value(theme, RowRect(2), "MUTE ALL", OnOff(GameSettings.Muted), 2, model.Selection))
            clicked = 2;
        if (Item(theme, new Rect(178f, 366f, 284f, 38f), "BACK",
                string.Empty, 3, model.Selection))
            clicked = 3;

        return clicked;
    }

    private static int DrawGraphics(OptionsModel model, RetroTheme theme, Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 15f, panel.width, 28f), "GRAPHICS", theme.MenuHeading);

        int clicked = NoClick;
        if (Value(theme, RowRect(0), "FULLSCREEN", OnOff(GameSettings.Fullscreen), 0, model.Selection))
            clicked = 0;
        if (Value(theme, RowRect(1), "RESOLUTION", GameSettings.ResolutionLabel, 1, model.Selection))
            clicked = 1;
        if (Value(theme, RowRect(2), "V-SYNC", OnOff(GameSettings.VSync), 2, model.Selection))
            clicked = 2;
        if (Value(theme, RowRect(3), "QUALITY", GameSettings.QualityLabel, 3, model.Selection))
            clicked = 3;
        if (Item(theme, new Rect(178f, 380f, 284f, 34f), "BACK",
                string.Empty, 4, model.Selection))
            clicked = 4;

        if (Application.isEditor)
        {
            GUI.Label(
                new Rect(160f, 356f, 320f, 18f),
                "RESOLUTION AND FULLSCREEN APPLY IN A BUILT PLAYER",
                theme.MenuFooter);
        }

        return clicked;
    }

    /// <summary>
    /// The rebind table: one row per action showing its key and its pad button
    /// side by side, on the wide panel so both fit without truncation.
    /// </summary>
    private static int DrawControls(OptionsModel model, RetroTheme theme, Rect panel)
    {
        GUI.Label(new Rect(panel.x, panel.y + 8f, panel.width, 24f), "CONTROLS", theme.MenuHeading);

        int clicked = NoClick;
        float x = panel.x + 22f;
        float width = panel.width - 44f;

        if (Value(theme, new Rect(x, panel.y + 36f, width, 22f), "EDITING",
                model.EditingPlayerOne ? "PLAYER ONE" : "PLAYER TWO", 0, model.Selection))
            clicked = 0;

        GUI.Label(new Rect(x + width - 168f, panel.y + 60f, 80f, 14f), "KEY", theme.ChipLabel);
        GUI.Label(new Rect(x + width - 84f, panel.y + 60f, 80f, 14f), "PAD", theme.ChipLabel);

        PlayerInputBindings bindings = model.EditingBindings;
        for (int i = 0; i < GameActionInfo.Count; i++)
        {
            GameAction action = (GameAction)i;
            int row = i + 1;
            Rect rect = new Rect(x, panel.y + 76f + i * 23f, width, 21f);

            if (BindingRow(
                    theme, rect,
                    GameActionInfo.DisplayName(action),
                    bindings.KeyLabel(action),
                    bindings.PadLabel(action),
                    row, model.Selection,
                    model.Listening == action))
            {
                clicked = row;
            }
        }

        float bottom = panel.y + 76f + GameActionInfo.Count * 23f + 6f;
        if (Item(theme, new Rect(x, bottom, width * 0.48f, 28f), "RESET DEFAULTS",
                string.Empty, GameActionInfo.Count + 1, model.Selection, 12))
            clicked = GameActionInfo.Count + 1;
        if (Item(theme, new Rect(x + width * 0.52f, bottom, width * 0.48f, 28f), "BACK",
                string.Empty, GameActionInfo.Count + 2, model.Selection, 12))
            clicked = GameActionInfo.Count + 2;

        return clicked;
    }

    /// <summary>The modal that tells the player the game is listening.</summary>
    private static void DrawCaptureOverlay(OptionsModel model, RetroTheme theme)
    {
        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.OverlayScrim);

        Rect panel = new Rect(162f, 190f, 316f, 100f);
        float pulse = 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
        Color accent = RetroPalette.ReadyAccent;

        RetroGui.Fill(panel, RetroPalette.OverlayPanel);
        RetroGui.Border(panel, new Color(accent.r, accent.g, accent.b, pulse), 3f);

        GUI.Label(
            new Rect(panel.x, panel.y + 16f, panel.width, 26f),
            GameActionInfo.DisplayName(model.Listening.Value),
            theme.MenuHeading);
        GUI.Label(
            new Rect(panel.x, panel.y + 44f, panel.width, 24f),
            "PRESS ANY KEY OR PAD BUTTON",
            theme.MatchRole);
        GUI.Label(
            new Rect(panel.x, panel.y + 70f, panel.width, 20f),
            "ESC TO CANCEL",
            theme.MenuFooter);
    }

    // -------------------------------------------------------------- elements

    /// <summary>Audio and graphics rows share one geometry so they line up.</summary>
    private static Rect RowRect(int index)
    {
        return new Rect(178f, 168f + index * 46f, 284f, 36f);
    }

    private static bool Item(
        RetroTheme theme,
        Rect rect,
        string label,
        string detail,
        int index,
        int selection,
        int fontSize = 0)
    {
        bool clicked = theme.Button(rect, label, index == selection, fontSize);

        if (!string.IsNullOrEmpty(detail))
        {
            GUI.Label(
                new Rect(rect.x - 18f, rect.yMax + 2f, rect.width + 36f, 18f),
                detail,
                theme.MenuDetail);
        }

        return clicked;
    }

    /// <summary>A label on the left and its current value on the right.</summary>
    private static bool Value(
        RetroTheme theme,
        Rect rect,
        string label,
        string value,
        int index,
        int selection)
    {
        bool selected = index == selection;
        DrawRowPlate(rect, selected);

        GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width * 0.55f, rect.height),
            label, theme.ChipValueLeft);

        // Arrows only on the focused row, so the page does not read as a wall
        // of chevrons.
        string text = selected ? $"‹ {value} ›" : value;
        GUI.Label(new Rect(rect.x + rect.width * 0.5f - 10f, rect.y, rect.width * 0.5f, rect.height),
            text, theme.ChipValue);

        return ClickedIn(rect);
    }

    private static bool Slider(
        RetroTheme theme,
        Rect rect,
        string label,
        float value,
        int index,
        int selection)
    {
        bool selected = index == selection;
        DrawRowPlate(rect, selected);

        GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width * 0.45f, rect.height),
            label, theme.ChipValueLeft);

        Rect track = new Rect(rect.x + rect.width * 0.45f, rect.center.y - 4f, rect.width * 0.36f, 8f);
        RetroGui.Fill(track, RetroPalette.ManaTrack);
        RetroGui.Fill(
            new Rect(track.x, track.y, track.width * Mathf.Clamp01(value), track.height),
            selected ? RetroPalette.ManaFillBright : RetroPalette.ManaFill);
        RetroGui.Border(track, RetroPalette.ManaBorder, 1f);

        GUI.Label(
            new Rect(track.xMax + 6f, rect.y, rect.width - track.width - rect.width * 0.45f - 12f, rect.height),
            Mathf.RoundToInt(value * 100f).ToString(),
            theme.ChipValue);

        return ClickedIn(rect);
    }

    private static bool BindingRow(
        RetroTheme theme,
        Rect rect,
        string label,
        string keyLabel,
        string padLabel,
        int index,
        int selection,
        bool listening)
    {
        bool selected = index == selection;
        DrawRowPlate(rect, selected);

        GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 190f, rect.height),
            label, theme.ChipValueLeft);

        DrawBindingChip(new Rect(rect.xMax - 172f, rect.y + 2f, 78f, rect.height - 4f),
            listening ? "..." : keyLabel, selected, theme);
        DrawBindingChip(new Rect(rect.xMax - 88f, rect.y + 2f, 78f, rect.height - 4f),
            listening ? "..." : padLabel, selected, theme);

        return ClickedIn(rect);
    }

    private static void DrawBindingChip(Rect rect, string text, bool selected, RetroTheme theme)
    {
        RetroGui.Panel(
            rect,
            RetroPalette.ChipFill,
            selected ? RetroPalette.GoldText : RetroPalette.BorderBlueSoft);
        GUI.Label(rect, text, theme.SeatTag);
    }

    private static void DrawRowPlate(Rect rect, bool selected)
    {
        RetroGui.Fill(rect, selected ? RetroPalette.CardSelected : RetroPalette.CardIdle);
        RetroGui.Border(
            rect,
            selected ? RetroPalette.GoldFrame : RetroPalette.BorderBlueSoft,
            selected ? 2f : 1f);
    }

    /// <summary>
    /// Rows are drawn as plates rather than <c>GUI.Button</c>s, so the mouse
    /// click has to be read off the event directly.
    /// </summary>
    private static bool ClickedIn(Rect rect)
    {
        Event current = Event.current;
        return current.type == EventType.MouseDown &&
               current.button == 0 &&
               rect.Contains(current.mousePosition);
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}
