using UnityEngine;

/// <summary>
/// Draws the ten-slot browser used by both the title screen's load route and
/// story mode's pause menu. Returns the index the mouse activated — a slot,
/// <see cref="SaveSlotMenuModel.BackIndex"/>, or <see cref="NoClick"/> — so the
/// screen keeps every decision.
/// </summary>
public static class SaveSlotView
{
    public const int NoClick = -1;

    /// <summary>Shared so the pause overlay and the title page line up exactly.</summary>
    public static readonly Rect Panel = new Rect(28f, 105f, 584f, 333f);

    /// <summary>Characters of dialogue preview a card can show before clipping.</summary>
    private const int CardPreviewLength = 32;

    private const float CardWidth = 272f;
    private const float CardHeight = 46f;
    private const float RowStep = 50f;
    private const float FirstRowY = 138f;
    private const float LeftColumnX = 44f;
    private const float RightColumnX = 324f;

    public static int Draw(
        SaveSlotMenuModel model,
        SaveSlotCatalog catalog,
        RetroTheme theme,
        string heading)
    {
        GUI.Label(new Rect(Panel.x, Panel.y + 7f, Panel.width, 20f), heading, theme.MenuHeading);

        int clicked = NoClick;
        for (int i = 0; i < SaveSlotCatalog.SlotCount; i++)
        {
            if (DrawCard(RectFor(i), catalog.GetSlot(i), i == model.Cursor, model.Mode, theme))
                clicked = i;
        }

        if (!string.IsNullOrEmpty(model.Message))
            GUI.Label(new Rect(Panel.x, 388f, Panel.width, 18f), model.Message, theme.Notice);

        if (theme.Button(new Rect(250f, 408f, 140f, 26f), "BACK", model.IsBackSelected, 14))
            clicked = SaveSlotMenuModel.BackIndex;

        return clicked;
    }

    /// <summary>Column-major so each column reads as a run of five slots.</summary>
    public static Rect RectFor(int slot)
    {
        int row = slot % SaveSlotMenuModel.Rows;
        int column = slot / SaveSlotMenuModel.Rows;
        return new Rect(
            column == 0 ? LeftColumnX : RightColumnX,
            FirstRowY + row * RowStep,
            CardWidth,
            CardHeight);
    }

    private static bool DrawCard(
        Rect rect,
        SaveSlotInfo info,
        bool selected,
        SaveSlotMenuMode mode,
        RetroTheme theme)
    {
        // An invisible button underneath keeps the whole card clickable while
        // the labels above it stay free to lay themselves out.
        bool clicked = GUI.Button(rect, GUIContent.none, selected ? theme.SelectedMenuButton : theme.MenuButton);

        RetroGui.Border(
            rect,
            selected ? RetroPalette.Gold : RetroPalette.BorderBlueSoft,
            selected ? 2f : 1f);

        GUI.Label(
            new Rect(rect.x + 6f, rect.y + 4f, 26f, 18f),
            SaveSlotCatalog.SlotLabel(info.Index),
            theme.SlotIndex);

        if (info.IsEmpty)
        {
            GUI.Label(
                new Rect(rect.x + 36f, rect.y + 4f, 200f, 16f),
                mode == SaveSlotMenuMode.Save ? "EMPTY  •  SAVE HERE" : "EMPTY",
                theme.SlotTitle);
            return clicked;
        }

        GUI.Label(
            new Rect(rect.x + 36f, rect.y + 4f, 148f, 16f),
            info.ChapterTitle,
            theme.SlotTitle);

        GUI.Label(
            new Rect(rect.xMax - 84f, rect.y + 4f, 78f, 16f),
            info.PlaytimeText,
            theme.SlotMeta);

        GUI.Label(
            new Rect(rect.x + 36f, rect.y + 22f, 160f, 18f),
            Describe(info),
            theme.SlotDetail);

        GUI.Label(
            new Rect(rect.xMax - 72f, rect.y + 22f, 66f, 16f),
            info.SavedAtText,
            theme.SlotMeta);

        return clicked;
    }

    /// <summary>
    /// One line of context for the card. The stored preview is longer than the
    /// card is wide, so it is clipped here rather than at save time — a wider
    /// layout later can use the whole thing.
    /// </summary>
    private static string Describe(SaveSlotInfo info)
    {
        string preview = string.IsNullOrEmpty(info.Speaker)
            ? info.PreviewText
            : $"{info.Speaker}: {info.PreviewText}";

        return preview.Length <= CardPreviewLength
            ? preview
            : preview.Substring(0, CardPreviewLength - 1).TrimEnd() + "…";
    }
}
