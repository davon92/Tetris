using UnityEngine;

/// <summary>Which control the mouse hit this frame, if any.</summary>
public readonly struct CharacterSelectClick
{
    private CharacterSelectClick(int cardIndex, bool back, bool confirm)
    {
        CardIndex = cardIndex;
        Back = back;
        Confirm = confirm;
    }

    /// <summary>Index of the clicked roster card, or -1.</summary>
    public int CardIndex { get; }

    public bool Back { get; }
    public bool Confirm { get; }

    public static CharacterSelectClick None => new CharacterSelectClick(-1, false, false);

    public CharacterSelectClick WithCard(int index) => new CharacterSelectClick(index, Back, Confirm);
    public CharacterSelectClick WithBack() => new CharacterSelectClick(CardIndex, true, Confirm);
    public CharacterSelectClick WithConfirm() => new CharacterSelectClick(CardIndex, Back, true);
}

/// <summary>Renders the horizontal roster picker for both versus routes.</summary>
public static class CharacterSelectView
{
    private const float CardWidth = 78f;
    private const float CardGap = 10f;
    private const float CardsStartX = 61f;
    private const int SmallButtonFontSize = 14;

    public static CharacterSelectClick Draw(
        CharacterSelectModel model,
        CpuDifficulty difficulty,
        RetroTheme theme,
        BattleArtLibrary art)
    {
        MenuChromeView.DrawFrame(theme, wide: true);

        GUI.Label(
            new Rect(70f, 116f, 500f, 26f),
            BuildHeading(model),
            theme.MenuHeading);
        // Seat-tinted, and on the side of the screen that seat will play from,
        // so the orange/indigo pairing is already learned by the time the
        // battle HUD uses it.
        DrawSeatStatus(
            new Rect(46f, 143f, 250f, 20f), BuildLeftStatus(model), RetroPalette.SeatOne, theme);
        DrawSeatStatus(
            new Rect(344f, 143f, 250f, 20f),
            BuildRightStatus(model, difficulty),
            RetroPalette.SeatTwo,
            theme);

        CharacterSelectClick click = CharacterSelectClick.None;
        for (int i = 0; i < BattleCharacterRoster.Count; i++)
        {
            Rect cardRect = new Rect(
                CardsStartX + i * (CardWidth + CardGap),
                166f,
                CardWidth,
                139f);
            if (DrawCard(i, cardRect, model, theme, art))
                click = click.WithCard(i);
        }

        DrawInfoPanel(model, theme);

        if (theme.Button(new Rect(82f, 400f, 178f, 32f), "BACK", false, SmallButtonFontSize))
            click = click.WithBack();

        if (theme.Button(new Rect(380f, 400f, 178f, 32f), "CONFIRM", true, SmallButtonFontSize))
            click = click.WithConfirm();

        MenuChromeView.DrawFooter(theme, MenuChromeView.RosterHint);
        return click;
    }

    private static void DrawSeatStatus(Rect rect, string text, Color seat, RetroTheme theme)
    {
        Color previous = GUI.color;
        GUI.color = seat;
        GUI.Label(rect, text, theme.SeatHelp);
        GUI.color = previous;
    }

    private static void DrawInfoPanel(CharacterSelectModel model, RetroTheme theme)
    {
        BattleCharacter selected = BattleCharacterRoster.Get(model.Cursor);
        bool unlocked = BattleCharacterRoster.IsUnlocked(model.Cursor);

        RetroGui.Panel(
            new Rect(78f, 318f, 484f, 72f),
            RetroPalette.PanelFillInfo,
            unlocked ? selected.Accent : RetroPalette.LockedAccent);

        GUI.Label(
            new Rect(92f, 325f, 456f, 24f),
            unlocked ? selected.DisplayName : "LOCKED CHALLENGER",
            theme.CharacterName);
        GUI.Label(
            new Rect(92f, 350f, 456f, 18f),
            unlocked ? selected.Title : "WIN ADVENTURES TO REVEAL THIS CHARACTER",
            theme.CharacterTitle);
        GUI.Label(
            new Rect(92f, 370f, 456f, 16f),
            string.IsNullOrEmpty(model.Message)
                ? $"SLOT {model.Cursor + 1} / {BattleCharacterRoster.Count}"
                : model.Message,
            theme.MenuFooter);
    }

    private static bool DrawCard(
        int characterIndex,
        Rect cardRect,
        CharacterSelectModel model,
        RetroTheme theme,
        BattleArtLibrary art)
    {
        BattleCharacter character = BattleCharacterRoster.Get(characterIndex);
        bool unlocked = BattleCharacterRoster.IsUnlocked(characterIndex);
        bool selected = model.Cursor == characterIndex;
        bool chosenByPlayerOne =
            model.Stage == CharacterSelectStage.PlayerTwo && model.PlayerOneIndex == characterIndex;

        RetroGui.Fill(cardRect, selected ? RetroPalette.CardSelected : RetroPalette.CardIdle);

        Rect portraitRect = new Rect(cardRect.x + 4f, cardRect.y + 4f, cardRect.width - 8f, 105f);
        if (!unlocked && art.LockedPortrait != null)
            GUI.DrawTexture(portraitRect, art.LockedPortrait, ScaleMode.ScaleAndCrop);
        else
            CharacterPortraitView.Draw(characterIndex, portraitRect, art);

        Color borderColor = selected
            ? RetroPalette.GoldBright
            : unlocked
                ? character.Accent
                : RetroPalette.LockedBorder;
        RetroGui.Border(cardRect, borderColor, selected ? 3f : 1f);

        RetroGui.Fill(
            new Rect(cardRect.x + 3f, cardRect.y + 112f, cardRect.width - 6f, 23f),
            RetroPalette.CardNameplate);
        GUI.Label(
            new Rect(cardRect.x + 2f, cardRect.y + 112f, cardRect.width - 4f, 23f),
            unlocked ? character.DisplayName : "LOCKED",
            theme.CharacterTitle);

        if (chosenByPlayerOne)
        {
            Rect badge = new Rect(cardRect.x + 3f, cardRect.y + 3f, 30f, 18f);
            RetroGui.Fill(badge, RetroPalette.SeatOne);
            GUI.Label(badge, "P1", theme.SeatTag);
        }

        return GUI.Button(cardRect, GUIContent.none, GUIStyle.none);
    }

    private static string BuildHeading(CharacterSelectModel model)
    {
        if (model.Stage == CharacterSelectStage.PlayerOne)
            return "PLAYER 1 — CHOOSE YOUR CHAMPION";

        return model.VersusMode == TetrisGameMode.VersusCpu
            ? "CHOOSE THE CPU RIVAL"
            : "PLAYER 2 — CHOOSE YOUR CHAMPION";
    }

    private static string BuildLeftStatus(CharacterSelectModel model)
    {
        return model.Stage == CharacterSelectStage.PlayerOne
            ? "P1  CHOOSING"
            : $"P1  {BattleCharacterRoster.Get(model.PlayerOneIndex).DisplayName}";
    }

    private static string BuildRightStatus(CharacterSelectModel model, CpuDifficulty difficulty)
    {
        bool versusCpu = model.VersusMode == TetrisGameMode.VersusCpu;
        if (model.Stage == CharacterSelectStage.PlayerOne)
            return versusCpu ? $"CPU  {MainMenuView.Describe(difficulty)}" : "P2  WAITING";

        return versusCpu ? "CPU  CHOOSING" : "P2  CHOOSING";
    }
}
