using UnityEngine;

/// <summary>
/// Draws the battle HUD from whatever the <see cref="MatchDirector"/> reports.
/// It reads match state and never mutates it, so replacing IMGUI with UI
/// Toolkit later means rewriting this file and nothing else.
/// </summary>
public static class BattleHudView
{
    private const string SoloHelp =
        "MOVE A/D   DOWN S   ROTATE W/Q   DROP SPACE   HOLD SHIFT   START/ESC MENU";

    private const string VersusHelp =
        "P1  A/D • S • W/Q • SPACE • SHIFT      P2  ARROWS • CTRL • ENTER • R-SHIFT      ESC MENU";

    private static readonly Rect LeftPortrait = new Rect(8f, 177f, 108f, 170f);
    private static readonly Rect RightPortrait = new Rect(524f, 177f, 108f, 170f);

    public static void Draw(MatchDirector match, RetroTheme theme, BattleArtLibrary art)
    {
        Rect titlePanel = new Rect(178f, 7f, 284f, 29f);
        RetroGui.Panel(titlePanel, RetroPalette.PanelFill, RetroPalette.BorderBlue);
        GUI.Label(titlePanel, BuildEncounterTitle(match), theme.Title);

        if (match.PlayerOne != null)
            DrawSessionHud(match, match.PlayerOne, 8f, false, theme);

        if (match.PlayerTwo != null)
        {
            DrawSessionHud(
                match,
                match.PlayerTwo,
                522f,
                match.Mode == TetrisGameMode.VersusCpu,
                theme);
        }

        DrawNextPreview(match, theme);

        if (match.PlayerTwo != null)
        {
            GUI.Label(new Rect(302f, 212f, 36f, 36f), "VS", theme.MenuHeading);
            DrawCharacterPortraits(match, theme, art);
        }

        DrawResultBanner(match, theme);

        GUI.Label(
            new Rect(12f, 451f, 616f, 20f),
            match.Mode == TetrisGameMode.LocalVersus ? VersusHelp : SoloHelp,
            theme.Help);

        DrawIntroOverlay(match, theme);
    }

    private static string BuildEncounterTitle(MatchDirector match)
    {
        if (!string.IsNullOrEmpty(match.EncounterTitle))
            return match.EncounterTitle;

        switch (match.Mode)
        {
            case TetrisGameMode.Solo:
                return "SOLO TRIAL";
            case TetrisGameMode.VersusCpu:
                return "RIVAL BATTLE";
            default:
                return "LOCAL VERSUS";
        }
    }

    /// <summary>The result copy lives here because it is presentation, not rules.</summary>
    public static string BuildResultMessage(MatchDirector match)
    {
        if (!match.HasOutcome)
            return string.Empty;

        if (match.Mode == TetrisGameMode.Solo)
            return "GAME OVER\nPRESS R TO RETRY";

        return match.IsStoryBattle
            ? $"{match.WinnerName} IS THE WINNER"
            : $"{match.WinnerName} IS THE WINNER\nPRESS R TO REMATCH";
    }

    private static void DrawSessionHud(
        MatchDirector match,
        TetrisGameSession session,
        float x,
        bool showDifficulty,
        RetroTheme theme)
    {
        string hold = session.HeldType.HasValue ? session.HeldType.Value.ToString() : "—";
        string text =
            session.DisplayName +
            (showDifficulty ? $"  {MainMenuView.Describe(match.Difficulty)}\n" : "\n") +
            $"SCORE  {session.Score:N0}\n" +
            $"LINES  {session.Lines}\n" +
            $"LEVEL  {session.Level}\n" +
            $"HOLD   {hold}\n" +
            $"GARBAGE {session.PendingGarbage}";

        RetroGui.Panel(
            new Rect(x, 48f, 110f, 113f),
            RetroPalette.PanelFillSoft,
            RetroPalette.BorderBlueSoft);
        GUI.Label(new Rect(x + 7f, 54f, 98f, 102f), text, theme.Hud);
    }

    private static void DrawNextPreview(MatchDirector match, RetroTheme theme)
    {
        bool shared = match.SharedQueue != null && match.PlayerTwo != null;
        if (!shared && match.PlayerOne == null)
            return;

        TetriminoType next = shared ? match.SharedQueue.NextType : match.PlayerOne.NextType;
        GUI.Label(new Rect(282f, 39f, 76f, 14f), "NEXT", theme.MenuFooter);
        RetroGui.Tetromino(next, new Vector2(320f, 64f), 6f, 0, 1f);
    }

    private static void DrawCharacterPortraits(
        MatchDirector match,
        RetroTheme theme,
        BattleArtLibrary art)
    {
        BattleCharacterDefinition left = BattleCharacterRoster.Get(match.PlayerOneCharacter);
        BattleCharacterDefinition right = BattleCharacterRoster.Get(match.PlayerTwoCharacter);

        RetroGui.Fill(LeftPortrait, RetroPalette.PortraitBackdrop);
        RetroGui.Fill(RightPortrait, RetroPalette.PortraitBackdrop);
        CharacterPortraitView.Draw(match.PlayerOneCharacter, LeftPortrait, art);
        CharacterPortraitView.Draw(match.PlayerTwoCharacter, RightPortrait, art);
        RetroGui.Border(LeftPortrait, left.Accent, 2f);
        RetroGui.Border(RightPortrait, right.Accent, 2f);

        RetroGui.Fill(new Rect(8f, 347f, 108f, 25f), RetroPalette.PortraitNameLeft);
        RetroGui.Fill(new Rect(524f, 347f, 108f, 25f), RetroPalette.PortraitNameRight);
        GUI.Label(new Rect(8f, 347f, 108f, 25f), left.DisplayName, theme.MenuHeading);
        GUI.Label(new Rect(524f, 347f, 108f, 25f), right.DisplayName, theme.MenuHeading);

        if (!match.HasOutcome)
            return;

        DrawOutcomeBadge(new Rect(8f, 375f, 108f, 23f), match.PlayerOneWon, theme);
        DrawOutcomeBadge(new Rect(524f, 375f, 108f, 23f), !match.PlayerOneWon, theme);
    }

    private static void DrawOutcomeBadge(Rect rect, bool isWinner, RetroTheme theme)
    {
        RetroGui.Panel(
            rect,
            isWinner ? RetroPalette.WinnerFill : RetroPalette.LoserFill,
            isWinner ? RetroPalette.StartAccent : RetroPalette.Rose);
        GUI.Label(
            rect,
            isWinner ? "WINNER" : "LOSER",
            isWinner ? theme.MatchWinner : theme.MatchLoser);
    }

    private static void DrawResultBanner(MatchDirector match, RetroTheme theme)
    {
        string message = BuildResultMessage(match);
        if (string.IsNullOrEmpty(message))
            return;

        Rect panel = new Rect(125f, 195f, 390f, 86f);
        RetroGui.Panel(panel, RetroPalette.ResultFill, RetroPalette.StartAccent, 2f);
        GUI.Label(
            new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f),
            message,
            theme.Title);
    }

    private static void DrawIntroOverlay(MatchDirector match, RetroTheme theme)
    {
        if (match.Phase != MatchPhase.Ready && match.Phase != MatchPhase.Start)
            return;

        RetroGui.Fill(RetroGui.CanvasRect, RetroPalette.OverlayScrim);

        bool isReady = match.Phase == MatchPhase.Ready;
        Color accent = isReady ? RetroPalette.ReadyAccent : RetroPalette.StartAccent;
        float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 8f) * 0.18f;
        Rect panel = new Rect(142f, 174f, 356f, 132f);

        RetroGui.Fill(panel, RetroPalette.OverlayPanel);
        RetroGui.Border(panel, new Color(accent.r, accent.g, accent.b, pulse), 3f);
        RetroGui.Fill(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, 2f), accent);

        GUI.Label(
            new Rect(panel.x + 12f, panel.y + 20f, panel.width - 24f, 56f),
            isReady ? "READY" : "START!",
            theme.MatchCallout);
        GUI.Label(
            new Rect(panel.x + 18f, panel.y + 83f, panel.width - 36f, 25f),
            BuildMatchupLabel(match),
            theme.MatchRole);
    }

    private static string BuildMatchupLabel(MatchDirector match)
    {
        string one = match.PlayerOne != null ? match.PlayerOne.DisplayName : "P1";
        if (match.PlayerTwo == null)
            return match.PlayerOne != null ? match.PlayerOne.DisplayName : "PLAYER";

        return $"{one}  VS  {match.PlayerTwo.DisplayName}";
    }
}
