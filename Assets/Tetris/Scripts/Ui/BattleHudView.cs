using UnityEngine;

/// <summary>
/// Draws the battle HUD from whatever the <see cref="MatchDirector"/> reports.
/// It reads match state and never mutates it; per-frame presentation motion
/// (score count-up, punches, flashes) lives in <see cref="BattleHudMotion"/>.
///
/// Layout ("RUSH PLATE"): each player's identity runs as an accent-colored
/// spine down their side gutter (name ribbon, hold box, garbage badge, stat
/// chips, portrait, nameplate), the shared NEXT queue sits in the center
/// gutter — both players fight over the same pieces, so it belongs to neither —
/// and the score is a chunky plate directly beneath each board.
/// </summary>
public static class BattleHudView
{
    private const string SoloHelp =
        "MOVE A/D   DOWN S   ROTATE W/Q   DROP SPACE   HOLD SHIFT   START/ESC MENU";

    private const string VersusHelp =
        "P1  A/D • S • W/Q • SPACE • SHIFT      P2  ARROWS • CTRL • ENTER • R-SHIFT      ESC MENU";

    // Versus geometry: boards at (128,80,160,320) and (352,80,160,320); the
    // strip y=48..72 above each board belongs to the world-space garbage dots.
    private static readonly Rect LeftBoard = new Rect(128f, 80f, 160f, 320f);
    private static readonly Rect RightBoard = new Rect(352f, 80f, 160f, 320f);
    private static readonly Rect SoloBoard = new Rect(240f, 80f, 160f, 320f);

    private static readonly Rect LeftPortrait = new Rect(8f, 177f, 108f, 170f);
    private static readonly Rect RightPortrait = new Rect(524f, 177f, 108f, 170f);

    public const int NoClick = -1;

    /// <summary>
    /// Draws the battle HUD. When <paramref name="resultSelection"/> is 0 or 1
    /// the post-match modal is drawn with that option highlighted; the return
    /// value is the option the mouse clicked, or <see cref="NoClick"/>.
    /// </summary>
    public static int Draw(
        MatchDirector match,
        RetroTheme theme,
        BattleArtLibrary art,
        BattleHudMotion motion,
        int resultSelection = -1)
    {
        bool versus = match.PlayerTwo != null;

        DrawTitleBanner(match, theme);

        if (versus)
            DrawVersusHud(match, theme, art, motion);
        else if (match.PlayerOne != null)
            DrawSoloHud(match, theme, motion);

        DrawResultBanner(match, theme);

        int clicked = NoClick;
        if (resultSelection >= 0)
            clicked = DrawResultModal(match, resultSelection, theme);

        GUI.Label(
            new Rect(12f, 460f, 616f, 16f),
            match.Mode == TetrisGameMode.LocalVersus ? VersusHelp : SoloHelp,
            theme.Help);

        DrawIntroOverlay(match, theme);
        return clicked;
    }

    /// <summary>Post-match choice: rematch/retry or back to the title menu.</summary>
    private static int DrawResultModal(MatchDirector match, int selection, RetroTheme theme)
    {
        Rect panel = new Rect(230f, 292f, 180f, 100f);
        RetroGui.Panel(panel, RetroPalette.OverlayPanel, RetroPalette.Gold, 2f);

        string primary = match.Mode == TetrisGameMode.Solo ? "RETRY" : "REMATCH";
        int clicked = NoClick;
        if (theme.Button(new Rect(245f, 302f, 150f, 36f), primary, selection == 0, 14))
            clicked = 0;
        if (theme.Button(new Rect(245f, 346f, 150f, 36f), "BACK TO MENU", selection == 1, 12))
            clicked = 1;

        return clicked;
    }

    // ---------------------------------------------------------------- title

    private static void DrawTitleBanner(MatchDirector match, RetroTheme theme)
    {
        Rect banner = new Rect(178f, 6f, 284f, 28f);

        // Marquee blink while the intro beats play; solid anchor once live.
        Color border = RetroPalette.BorderBlue;
        if (match.Phase == MatchPhase.Ready || match.Phase == MatchPhase.Start)
        {
            border = RetroPalette.Gold;
            border.a = Mathf.Sin(Time.unscaledTime * Mathf.PI * 4f) > 0f ? 1f : 0.35f;
        }

        RetroGui.Panel(banner, RetroPalette.PanelFillDeep, border);
        RetroGui.Fill(new Rect(186f, 10f, 268f, 2f), RetroPalette.Gold);
        GUI.Label(banner, BuildEncounterTitle(match), theme.Title);
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

    // --------------------------------------------------------------- versus

    private static void DrawVersusHud(
        MatchDirector match,
        RetroTheme theme,
        BattleArtLibrary art,
        BattleHudMotion motion)
    {
        BattleCharacterDefinition left = BattleCharacterRoster.Get(match.PlayerOneCharacter);
        BattleCharacterDefinition right = BattleCharacterRoster.Get(match.PlayerTwoCharacter);

        DrawNameRibbon(
            new Rect(4f, 6f, 168f, 28f), match.PlayerOne.DisplayName, "1P",
            RetroPalette.PlayerOneBadge, left.Accent, false, theme,
            match.PlayerOneCallout, match.PlayerOneCalloutAge);
        DrawNameRibbon(
            new Rect(468f, 6f, 168f, 28f), match.PlayerTwo.DisplayName,
            match.Mode == TetrisGameMode.VersusCpu ? "CPU" : "2P",
            RetroPalette.NameplateBram, right.Accent, true, theme,
            match.PlayerTwoCallout, match.PlayerTwoCalloutAge);

        if (match.Mode == TetrisGameMode.VersusCpu)
        {
            GUI.Label(
                new Rect(538f, 36f, 98f, 10f),
                $"CPU • {MainMenuView.Describe(match.Difficulty)}",
                theme.ScoreTag);
        }

        DrawHoldBox(new Rect(8f, 52f, 64f, 60f), match.PlayerOne, motion.PlayerOne, theme);
        DrawHoldBox(new Rect(568f, 52f, 64f, 60f), match.PlayerTwo, motion.PlayerTwo, theme);

        DrawGarbageBadge(
            new Rect(78f, 52f, 46f, 22f), match.PlayerOne.PendingGarbage, theme,
            match.PlayerOneCallout, match.PlayerOneCalloutAge);
        DrawGarbageBadge(
            new Rect(516f, 52f, 46f, 22f), match.PlayerTwo.PendingGarbage, theme,
            match.PlayerTwoCallout, match.PlayerTwoCalloutAge);

        DrawStatChip(new Rect(8f, 120f, 116f, 24f), "LINES",
            motion.PlayerOne.LinesText, motion.PlayerOne.LinesFlash, false, theme);
        DrawStatChip(new Rect(8f, 150f, 116f, 24f), "LEVEL",
            motion.PlayerOne.LevelText, motion.PlayerOne.LevelFlash, true, theme);
        DrawStatChip(new Rect(516f, 120f, 116f, 24f), "LINES",
            motion.PlayerTwo.LinesText, motion.PlayerTwo.LinesFlash, false, theme);
        DrawStatChip(new Rect(516f, 150f, 116f, 24f), "LEVEL",
            motion.PlayerTwo.LevelText, motion.PlayerTwo.LevelFlash, true, theme);

        DrawNextQueue(new Rect(292f, 84f, 56f, 130f), motion.Upcoming, theme);
        DrawVsEmblem(theme);

        DrawPortrait(LeftPortrait, match.PlayerOneCharacter, left.Accent, art,
            match.PlayerOneCallout, match.PlayerOneCalloutAge, motion.PlayerOneVitals);
        DrawPortrait(RightPortrait, match.PlayerTwoCharacter, right.Accent, art,
            match.PlayerTwoCallout, match.PlayerTwoCalloutAge, motion.PlayerTwoVitals);

        // Banded across the bottom of each portrait, directly above the
        // nameplate — the fighter and their vitality read as one unit.
        DrawHealthBar(new Rect(8f, 331f, 108f, 14f), motion.PlayerOneVitals, left.Accent, false, theme);
        DrawHealthBar(new Rect(524f, 331f, 108f, 14f), motion.PlayerTwoVitals, right.Accent, true, theme);

        DrawNameplate(new Rect(8f, 347f, 108f, 25f), left.DisplayName, left.Accent, theme);
        DrawNameplate(new Rect(524f, 347f, 108f, 25f), right.DisplayName, right.Accent, theme);

        DrawSpellPlate(new Rect(8f, 180f, 108f, 14f), left.Ability, theme);
        DrawSpellPlate(new Rect(524f, 180f, 108f, 14f), right.Ability, theme);

        if (match.HasOutcome)
        {
            DrawOutcomeBadge(new Rect(8f, 375f, 108f, 23f), match.PlayerOneWon, match, theme);
            DrawOutcomeBadge(new Rect(524f, 375f, 108f, 23f), !match.PlayerOneWon, match, theme);
        }

        DrawScorePlate(new Rect(128f, 404f, 160f, 40f), match.PlayerOne,
            motion.PlayerOne, left.Accent, theme);
        DrawScorePlate(new Rect(352f, 404f, 160f, 40f), match.PlayerTwo,
            motion.PlayerTwo, right.Accent, theme);

        DrawBattleBlinker(match, theme);

        DrawToast(LeftBoard, match.PlayerOneCallout, match.PlayerOneCalloutAge,
            match.PlayerOneCalloutTimeLeft, false, theme);
        DrawToast(RightBoard, match.PlayerTwoCallout, match.PlayerTwoCalloutAge,
            match.PlayerTwoCalloutTimeLeft, true, theme);
    }

    // ----------------------------------------------------------------- solo

    private static void DrawSoloHud(MatchDirector match, RetroTheme theme, BattleHudMotion motion)
    {
        TetrisGameSession player = match.PlayerOne;

        DrawNameRibbon(
            new Rect(4f, 6f, 168f, 28f), player.DisplayName, "1P",
            RetroPalette.PlayerOneBadge, RetroPalette.Gold, false, theme,
            match.PlayerOneCallout, match.PlayerOneCalloutAge);

        DrawHoldBox(new Rect(160f, 80f, 72f, 56f), player, motion.PlayerOne, theme);
        DrawStatChip(new Rect(160f, 144f, 72f, 24f), "LINES",
            motion.PlayerOne.LinesText, motion.PlayerOne.LinesFlash, false, theme);
        DrawStatChip(new Rect(160f, 174f, 72f, 24f), "LEVEL",
            motion.PlayerOne.LevelText, motion.PlayerOne.LevelFlash, true, theme);

        if (player.PendingGarbage > 0)
        {
            DrawGarbageBadge(
                new Rect(160f, 204f, 72f, 22f), player.PendingGarbage, theme,
                match.PlayerOneCallout, match.PlayerOneCalloutAge);
        }

        DrawHealthBar(new Rect(160f, 234f, 72f, 14f), motion.PlayerOneVitals,
            RetroPalette.Gold, false, theme);

        DrawNextQueue(new Rect(408f, 80f, 56f, 130f), motion.Upcoming, theme);

        DrawScorePlate(new Rect(240f, 404f, 160f, 40f), player,
            motion.PlayerOne, RetroPalette.Gold, theme);

        DrawToast(SoloBoard, match.PlayerOneCallout, match.PlayerOneCalloutAge,
            match.PlayerOneCalloutTimeLeft, false, theme);
    }

    // ------------------------------------------------------------- elements

    private static void DrawNameRibbon(
        Rect rect,
        string name,
        string seatLabel,
        Color seatFill,
        Color accent,
        bool rightAligned,
        RetroTheme theme,
        BattleCallout? callout,
        float calloutAge)
    {
        // A fresh SENT toast flashes the whole ribbon white for a beat.
        Color border = accent;
        if (callout.HasValue && callout.Value.Kind == BattleCalloutKind.Sent && calloutAge < 0.15f)
            border = Color.white;

        RetroGui.Panel(rect, RetroPalette.PanelFillDeep, border, 2f);

        Rect seat = rightAligned
            ? new Rect(rect.xMax - 26f, rect.y, 26f, rect.height)
            : new Rect(rect.x, rect.y, 26f, rect.height);
        RetroGui.Fill(seat, seatFill);
        GUI.Label(seat, seatLabel, theme.SeatTag);

        Rect nameRect = rightAligned
            ? new Rect(rect.x + 2f, rect.y, rect.width - 34f, rect.height)
            : new Rect(rect.x + 32f, rect.y, rect.width - 34f, rect.height);
        GUI.Label(nameRect, name, rightAligned ? theme.NameRibbonRight : theme.NameRibbon);
    }

    private static void DrawHoldBox(
        Rect rect,
        TetrisGameSession session,
        BattleHudMotion.Seat seat,
        RetroTheme theme)
    {
        // Spent hold reads dimmer until the next piece unlocks it again.
        Color border = session.IsHoldLocked ? RetroPalette.BorderBlueSoft : RetroPalette.BorderCyan;
        if (seat.HoldPop > 0f)
            border = Color.white;

        RetroGui.Panel(rect, RetroPalette.ChipFill, border, 2f);
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 13f), "HOLD", theme.BoxHeader);

        Rect pieceArea = new Rect(rect.x, rect.y + 13f, rect.width, rect.height - 13f);
        if (session.HeldType.HasValue)
        {
            float cellSize = seat.HoldPop > 0f ? 10f : 9f;
            RetroGui.TetrominoInBox(session.HeldType.Value, pieceArea, cellSize, 0, 1f);
        }
        else
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.Label(pieceArea, "—", theme.ToastText);
            GUI.color = previous;
        }
    }

    private static void DrawGarbageBadge(
        Rect rect,
        int pending,
        RetroTheme theme,
        BattleCallout? callout,
        float calloutAge)
    {
        if (pending <= 0)
            return;

        bool incomingPunch =
            callout.HasValue && callout.Value.Kind == BattleCalloutKind.Incoming && calloutAge < 0.2f;
        bool blockFlash =
            callout.HasValue && callout.Value.Kind == BattleCalloutKind.Blocked && calloutAge < 0.2f;

        Rect drawRect = incomingPunch
            ? new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f)
            : rect;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 4f);
        Color border = blockFlash
            ? RetroPalette.Success
            : Color.Lerp(RetroPalette.Rose, RetroPalette.DangerFlash, pulse);

        RetroGui.Panel(drawRect, RetroPalette.ChipFill, border, blockFlash || incomingPunch ? 2f : 1f);
        RetroGui.Fill(new Rect(drawRect.x + 6f, drawRect.center.y - 3f, 6f, 6f), RetroPalette.Rose);

        Color previousColor = GUI.color;
        if (pending >= 4)
            GUI.color = RetroPalette.Rose;
        string text = pending >= 6 ? $"{pending}!!" : pending.ToString();
        GUI.Label(
            new Rect(drawRect.x + 14f, drawRect.y, drawRect.width - 20f, drawRect.height),
            text,
            theme.ChipValue);
        GUI.color = previousColor;
    }

    private static void DrawStatChip(
        Rect rect,
        string label,
        string value,
        float flash,
        bool flashBorder,
        RetroTheme theme)
    {
        Color border = RetroPalette.BorderBlueSoft;
        if (flashBorder && flash > 0f)
            border = RetroPalette.StartAccent;

        RetroGui.Panel(rect, RetroPalette.ChipFill, border);
        GUI.Label(new Rect(rect.x + 6f, rect.y, 50f, rect.height), label, theme.ChipLabel);

        Color previous = GUI.color;
        if (!flashBorder && flash > 0f)
            GUI.color = RetroPalette.GoldBright;
        GUI.Label(
            new Rect(rect.xMax - 60f, rect.y, 54f, rect.height),
            value,
            theme.ChipValue);
        GUI.color = previous;
    }

    /// <summary>
    /// The shared queue both players are fighting over: one gold-framed box in
    /// the center gutter, nearest piece big and bright, deeper pieces fading.
    /// </summary>
    private static void DrawNextQueue(Rect rect, TetriminoType[] upcoming, RetroTheme theme)
    {
        RetroGui.Panel(rect, RetroPalette.ChipFill, RetroPalette.GoldText, 2f);
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 14f), "NEXT", theme.BoxHeader);

        if (upcoming == null || upcoming.Length == 0)
            return;

        float x = rect.x + 4f;
        float width = rect.width - 8f;

        RetroGui.TetrominoInBox(upcoming[0], new Rect(x, rect.y + 16f, width, 36f), 9f, 0, 1f);
        RetroGui.Fill(new Rect(x, rect.y + 54f, width, 1f), RetroPalette.BorderBlueSoft);

        if (upcoming.Length > 1)
            RetroGui.TetrominoInBox(upcoming[1], new Rect(x, rect.y + 58f, width, 32f), 7f, 0, 0.85f);
        if (upcoming.Length > 2)
            RetroGui.TetrominoInBox(upcoming[2], new Rect(x, rect.y + 94f, width, 32f), 7f, 0, 0.7f);
    }

    private static void DrawVsEmblem(RetroTheme theme)
    {
        Rect rect = new Rect(296f, 226f, 48f, 30f);
        float pulse = 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f));
        Color border = RetroPalette.Gold;
        border.a = pulse;

        RetroGui.Panel(rect, RetroPalette.PanelFillDeep, border, 2f);
        GUI.Label(rect, "VS", theme.MenuHeading);
    }

    private static void DrawPortrait(
        Rect rect,
        int characterIndex,
        Color accent,
        BattleArtLibrary art,
        BattleCallout? callout,
        float calloutAge,
        BattleVitals vitals)
    {
        // Landing garbage flashes the frame rose — the damage read.
        Color border = accent;
        if (callout.HasValue && callout.Value.Kind == BattleCalloutKind.Incoming && calloutAge < 0.25f)
            border = RetroPalette.Rose;

        RetroGui.Fill(rect, RetroPalette.PortraitBackdrop);
        CharacterPortraitView.Draw(
            characterIndex, rect, art, vitals.Mood, vitals.ReactionStrength);
        RetroGui.Border(rect, border, 2f);
        RetroGui.Fill(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 3f), accent);
    }

    /// <summary>
    /// Fighting-game vitality: solid health, then the grey band of damage the
    /// player can win back by clearing garbage, then the part that is gone.
    /// Player two's bar drains right-to-left so both read outward from center.
    /// </summary>
    private static void DrawHealthBar(
        Rect rect,
        BattleVitals vitals,
        Color accent,
        bool mirrored,
        RetroTheme theme)
    {
        RetroGui.Fill(rect, new Color(0.04f, 0.03f, 0.06f, 0.95f));

        float inset = 2f;
        Rect inner = new Rect(
            rect.x + inset, rect.y + inset,
            rect.width - inset * 2f, rect.height - inset * 2f);

        float health = Mathf.Clamp01(vitals.DisplayedHealth);
        float recoverable = Mathf.Clamp01(vitals.DisplayedRecoverable);

        Color healthColor = health >= 0.62f
            ? new Color(0.35f, 0.92f, 0.5f)
            : health >= 0.34f
                ? new Color(1f, 0.78f, 0.26f)
                : new Color(1f, 0.32f, 0.34f);

        // Critical health strobes so it is impossible to miss.
        if (health < 0.34f)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            healthColor = Color.Lerp(healthColor, Color.white, pulse * 0.35f);
        }

        float healthWidth = inner.width * health;
        float recoverableWidth = inner.width * recoverable;

        if (mirrored)
        {
            RetroGui.Fill(
                new Rect(inner.xMax - healthWidth, inner.y, healthWidth, inner.height),
                healthColor);
            RetroGui.Fill(
                new Rect(inner.xMax - healthWidth - recoverableWidth, inner.y, recoverableWidth, inner.height),
                new Color(0.62f, 0.64f, 0.72f, 0.85f));
        }
        else
        {
            RetroGui.Fill(new Rect(inner.x, inner.y, healthWidth, inner.height), healthColor);
            RetroGui.Fill(
                new Rect(inner.x + healthWidth, inner.y, recoverableWidth, inner.height),
                new Color(0.62f, 0.64f, 0.72f, 0.85f));
        }

        RetroGui.Border(rect, accent, 1f);
    }

    /// <summary>
    /// The character's spell, banded across the top of their portrait so the
    /// reward for a tetris or a gold cell is always legible.
    /// </summary>
    private static void DrawSpellPlate(Rect rect, MagicAbility ability, RetroTheme theme)
    {
        RetroGui.Fill(rect, new Color(0f, 0f, 0f, 0.72f));

        Color previous = GUI.color;
        GUI.color = RetroPalette.Gold;
        GUI.Label(rect, MagicAbilityInfo.DisplayName(ability), theme.ScoreTagCentered);
        GUI.color = previous;
    }

    private static void DrawNameplate(
        Rect rect,
        string name,
        Color accent,
        RetroTheme theme)
    {
        // Fill derives from the character's accent so plates follow the
        // selected character, not the seat they happen to sit in.
        Color fill = new Color(accent.r * 0.26f, accent.g * 0.26f, accent.b * 0.26f, 0.98f);
        RetroGui.Fill(rect, fill);
        GUI.Label(rect, name, theme.MenuHeading);
        RetroGui.Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), accent);
    }

    private static void DrawOutcomeBadge(
        Rect rect,
        bool isWinner,
        MatchDirector match,
        RetroTheme theme)
    {
        Color border = isWinner ? RetroPalette.StartAccent : RetroPalette.Rose;
        if (isWinner && match.Phase == MatchPhase.Result &&
            Mathf.Sin(Time.unscaledTime * Mathf.PI * 6f) < 0f)
        {
            border = Color.white;
        }

        RetroGui.Panel(
            rect,
            isWinner ? RetroPalette.WinnerFill : RetroPalette.LoserFill,
            border,
            2f);
        GUI.Label(
            rect,
            isWinner ? "WINNER" : "LOSER",
            isWinner ? theme.MatchWinner : theme.MatchLoser);
    }

    /// <summary>The hero element: a chunky score plate directly under the board.</summary>
    private static void DrawScorePlate(
        Rect rect,
        TetrisGameSession session,
        BattleHudMotion.Seat seat,
        Color accent,
        RetroTheme theme)
    {
        bool punching = seat.Punch > 0f;
        Rect drawRect = punching
            ? new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f)
            : rect;
        Color border = punching ? RetroPalette.EmberOrange : accent;

        RetroGui.Panel(drawRect, RetroPalette.ScorePlateFill, border, 2f);
        GUI.Label(new Rect(drawRect.x + 6f, drawRect.y + 2f, 60f, 10f), "SCORE", theme.ScoreTag);

        // Fixed-width digits so the count-up never makes the number jitter.
        bool ticking = seat.RoundedScore != session.Score;
        Color previous = GUI.color;
        if (ticking || punching)
            GUI.color = RetroPalette.GoldBright;
        GUI.Label(
            new Rect(drawRect.x + 6f, drawRect.y + 14f, drawRect.width - 12f, 24f),
            seat.ScoreText,
            theme.ScoreValue);
        GUI.color = previous;
    }

    private static void DrawBattleBlinker(MatchDirector match, RetroTheme theme)
    {
        Rect rect = new Rect(296f, 404f, 48f, 24f);
        Color border = RetroPalette.GoldText;
        border.a = 0.6f;
        RetroGui.Panel(rect, RetroPalette.PanelFillDeep, border);

        string text;
        if (match.HasOutcome)
            text = "K.O.!";
        else if (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2.8f) > 0f)
            text = "BATTLE";
        else
            return;

        Color previous = GUI.color;
        GUI.color = RetroPalette.GoldText;
        GUI.Label(rect, text, theme.BoxHeader);
        GUI.color = previous;
    }

    /// <summary>
    /// Transient callout ribbon over the board (the only element allowed
    /// there): slides in from the player's outer edge, fades out at the end.
    /// </summary>
    private static void DrawToast(
        Rect board,
        BattleCallout? callout,
        float age,
        float timeLeft,
        bool fromRight,
        RetroTheme theme)
    {
        if (!callout.HasValue)
            return;

        Color color = callout.Value.Kind switch
        {
            BattleCalloutKind.Sent => RetroPalette.Gold,
            BattleCalloutKind.Blocked => RetroPalette.Success,
            BattleCalloutKind.Magic => RetroPalette.BorderCyan,
            _ => RetroPalette.Rose
        };

        float slide = (1f - Mathf.Clamp01(age / 0.1f)) * 4f * (fromRight ? 1f : -1f);
        float fade = timeLeft < 0.3f ? timeLeft / 0.3f : 1f;

        Rect rect = new Rect(board.x + slide, 132f, board.width, 20f);
        RetroGui.Fill(rect, new Color(color.r, color.g, color.b, 0.22f * fade));
        RetroGui.Border(rect, new Color(color.r, color.g, color.b, 0.85f * fade), 1f);

        Color previous = GUI.color;
        GUI.color = new Color(color.r, color.g, color.b, fade);
        GUI.Label(rect, callout.Value.Text, theme.ToastText);
        GUI.color = previous;
    }

    // ---------------------------------------------------------- full-screen

    /// <summary>The result copy lives here because it is presentation, not rules.</summary>
    public static string BuildResultMessage(MatchDirector match)
    {
        if (!match.HasOutcome)
            return string.Empty;

        return match.Mode == TetrisGameMode.Solo
            ? "GAME OVER"
            : $"{match.WinnerName} IS THE WINNER";
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
