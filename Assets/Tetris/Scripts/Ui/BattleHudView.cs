using UnityEngine;

/// <summary>
/// Draws the battle HUD from whatever the <see cref="MatchDirector"/> reports.
/// It reads match state and never mutates it; per-frame presentation motion
/// (score count-up, punches, flashes) lives in <see cref="BattleHudMotion"/>.
///
/// Layout ("RUSH PLATE"): the character a player picked runs as an accent-
/// colored spine down their side gutter (hold box, garbage badge, stat chips,
/// portrait, health bar, nameplate), the shared NEXT queue sits in the center
/// gutter — both players fight over the same pieces, so it belongs to neither —
/// and a chunky seat plate carrying the seat banner and score sits directly
/// beneath each board.
///
/// Seat identity (PLAYER 1 / PLAYER 2 / CPU) is drawn in seat colours rather
/// than character accents: two players can pick the same character, and when
/// they do the accents stop telling the sides apart. The seat reads five ways —
/// an edge spine, the ribbon chip, a frame around the playfield, the banner on
/// the plate under it, and that seat's own key list — plus a full-width callout
/// over each board during READY/START, when a player is first hunting for their
/// side.
/// </summary>
public static class BattleHudView
{
    private const string CpuKeys = "COMPUTER CONTROLLED";

    /// <summary>
    /// Built from the live bindings rather than written out, so a rebind in the
    /// options screen is reflected here instead of quietly lying to the player.
    /// </summary>
    private static string BuildSeatKeys(PlayerInputBindings seat)
    {
        return
            $"{seat.KeyLabel(GameAction.MoveLeft)}/{seat.KeyLabel(GameAction.MoveRight)} • " +
            $"{seat.KeyLabel(GameAction.SoftDrop)} • " +
            $"{seat.KeyLabel(GameAction.RotateClockwise)}/{seat.KeyLabel(GameAction.RotateCounterClockwise)} • " +
            $"{seat.KeyLabel(GameAction.HardDrop)} • {seat.KeyLabel(GameAction.Hold)} • " +
            $"{seat.KeyLabel(GameAction.CastOffensive)}/{seat.KeyLabel(GameAction.CastDefensive)}";
    }

    /// <summary>
    /// Just the keys that steer a piece. The READY callout is one board wide
    /// and is answering "which board is mine, and how do I move it" — hold and
    /// the two spells can wait for the help row, which has the width for them.
    /// </summary>
    private static string BuildSeatMoveKeys(PlayerInputBindings seat)
    {
        return
            $"{seat.KeyLabel(GameAction.MoveLeft)}/{seat.KeyLabel(GameAction.MoveRight)} • " +
            $"{seat.KeyLabel(GameAction.RotateClockwise)}/{seat.KeyLabel(GameAction.RotateCounterClockwise)} • " +
            $"{seat.KeyLabel(GameAction.HardDrop)}";
    }

    // Versus geometry: boards at (128,80,160,320) and (352,80,160,320); the
    // strip y=48..72 above each board belongs to the world-space garbage dots.
    private static readonly Rect LeftBoard = new Rect(128f, 80f, 160f, 320f);
    private static readonly Rect RightBoard = new Rect(352f, 80f, 160f, 320f);
    private static readonly Rect SoloBoard = new Rect(240f, 80f, 160f, 320f);

    private static readonly Rect LeftPortrait = new Rect(8f, 177f, 108f, 170f);
    private static readonly Rect RightPortrait = new Rect(524f, 177f, 108f, 170f);

    // The seat frame hugs the playfield 3px out, and the seat plate picks up
    // exactly where the frame stops, so frame and plate read as one bracket
    // wrapped around the board.
    private const float SeatFrameInset = 3f;
    private const float SeatPlateTop = 402f;
    private const float SeatPlateHeight = 56f;
    private const float SeatBannerHeight = 18f;
    private const float HelpRowTop = 460f;

    // Named because the mana motes have to fly to the same rects the bars are
    // drawn in; a bar that moved without its target would break the effect.
    private static readonly Rect LeftManaBar = new Rect(8f, 333f, 108f, 14f);
    private static readonly Rect RightManaBar = new Rect(524f, 333f, 108f, 14f);
    private static readonly Rect SoloManaBar = new Rect(160f, 250f, 72f, 14f);

    /// <summary>How many stale samples trail a mana mote, and how far back each sits.</summary>
    private const int TrailSamples = 2;

    private const float TrailSpacing = 0.055f;

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

        DrawHelpRow(match, versus, theme);

        DrawIntroOverlay(match, theme);
        return clicked;
    }

    /// <summary>
    /// In versus the row is split per seat and tinted, P1's keys growing right
    /// from the left edge and P2's growing left from the right, so each half is
    /// bound to a side by colour and direction. The full rebindable list is far
    /// wider than one 166px board column, which is why this keeps the canvas
    /// width rather than sitting under the boards like the seat plates do.
    /// </summary>
    private static void DrawHelpRow(MatchDirector match, bool versus, RetroTheme theme)
    {
        if (!versus)
        {
            GUI.Label(
                new Rect(12f, HelpRowTop, 616f, 16f),
                $"P1  {BuildSeatKeys(PlayerInputProfiles.One)}      ESC MENU",
                theme.Help);
            return;
        }

        bool cpu = match.Mode == TetrisGameMode.VersusCpu;
        DrawSeatHelp(
            new Rect(12f, HelpRowTop, 275f, 16f),
            BuildSeatKeys(PlayerInputProfiles.One),
            RetroPalette.SeatOne,
            theme.SeatHelpLeft);
        GUI.Label(new Rect(292f, HelpRowTop, 56f, 16f), "ESC MENU", theme.Help);
        DrawSeatHelp(
            new Rect(353f, HelpRowTop, 275f, 16f),
            cpu ? CpuKeys : BuildSeatKeys(PlayerInputProfiles.Two),
            RetroPalette.SeatTwo,
            theme.SeatHelpRight);
    }

    private static void DrawSeatHelp(Rect rect, string text, Color seat, GUIStyle style)
    {
        Color previous = GUI.color;
        GUI.color = seat;
        GUI.Label(rect, text, style);
        GUI.color = previous;
    }

    /// <summary>The seat's full-width column: the board plus its frame inset.</summary>
    private static Rect SeatColumn(Rect board, float y, float height)
    {
        return new Rect(
            board.x - SeatFrameInset,
            y,
            board.width + SeatFrameInset * 2f,
            height);
    }

    /// <summary>Post-match choice: rematch/retry or back to the title menu.</summary>
    private static int DrawResultModal(MatchDirector match, int selection, RetroTheme theme)
    {
        Rect panel = new Rect(230f, 292f, 180f, 100f);
        RetroGui.Panel(panel, RetroPalette.OverlayPanel, RetroPalette.Gold, 2f);

        string primary = match.SoloRun != null ? "RETRY" : "REMATCH";
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
            case TetrisGameMode.Marathon:
                return "SOLO MARATHON";
            case TetrisGameMode.Sprint:
                return "LINE SPRINT";
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
        BattleCharacter left = BattleCharacterRoster.Get(match.PlayerOneCharacter);
        BattleCharacter right = BattleCharacterRoster.Get(match.PlayerTwoCharacter);
        bool cpu = match.Mode == TetrisGameMode.VersusCpu;

        // Seat chrome first: it frames everything else on that side.
        RetroGui.Fill(new Rect(0f, 0f, 5f, RetroGui.CanvasHeight), RetroPalette.SeatOne);
        RetroGui.Fill(
            new Rect(RetroGui.CanvasWidth - 5f, 0f, 5f, RetroGui.CanvasHeight),
            RetroPalette.SeatTwo);
        DrawSeatFrame(LeftBoard, RetroPalette.SeatOne);
        DrawSeatFrame(RightBoard, RetroPalette.SeatTwo);

        DrawNameRibbon(
            new Rect(4f, 6f, 168f, 28f), match.PlayerOne.DisplayName, "P1",
            RetroPalette.SeatOne, false, theme,
            match.PlayerOneCallout, match.PlayerOneCalloutAge);
        DrawNameRibbon(
            new Rect(468f, 6f, 168f, 28f), match.PlayerTwo.DisplayName,
            cpu ? "CPU" : "P2",
            RetroPalette.SeatTwo, true, theme,
            match.PlayerTwoCallout, match.PlayerTwoCalloutAge);

        if (cpu)
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
        // nameplate — the fighter, their vitality and their charge read as one
        // unit, health over mana in that order.
        DrawHealthBar(new Rect(8f, 320f, 108f, 12f), motion.PlayerOneVitals, left.Accent, false, theme);
        DrawHealthBar(new Rect(524f, 320f, 108f, 12f), motion.PlayerTwoVitals, right.Accent, true, theme);

        // The CPU seat gets no key prompt — nobody is pressing anything there.
        bool humanTwo = match.Mode == TetrisGameMode.LocalVersus;
        DrawManaBar(LeftManaBar, motion.PlayerOneVitals,
            motion.PlayerOneMotes.AbsorbStrength, false, theme);
        DrawManaBar(RightManaBar, motion.PlayerTwoVitals,
            motion.PlayerTwoMotes.AbsorbStrength, true, theme);

        DrawNameplate(new Rect(8f, 347f, 108f, 25f), left.DisplayName, left.Accent, theme);
        DrawNameplate(new Rect(524f, 347f, 108f, 25f), right.DisplayName, right.Accent, theme);

        // Offensive over defensive, banded across the top of the portrait, each
        // lighting up the moment its own cost is covered. The stat chips run to
        // y=174, so these have to start below that.
        DrawSpellPlate(new Rect(8f, 180f, 108f, 13f), left.OffensiveAbility,
            motion.PlayerOneVitals.OffensiveReady,
            PlayerInputProfiles.One.KeyLabel(GameAction.CastOffensive), theme);
        DrawSpellPlate(new Rect(8f, 194f, 108f, 13f), left.DefensiveAbility,
            motion.PlayerOneVitals.DefensiveReady,
            PlayerInputProfiles.One.KeyLabel(GameAction.CastDefensive), theme);

        DrawSpellPlate(new Rect(524f, 180f, 108f, 13f), right.OffensiveAbility,
            motion.PlayerTwoVitals.OffensiveReady,
            humanTwo ? PlayerInputProfiles.Two.KeyLabel(GameAction.CastOffensive) : null, theme);
        DrawSpellPlate(new Rect(524f, 194f, 108f, 13f), right.DefensiveAbility,
            motion.PlayerTwoVitals.DefensiveReady,
            humanTwo ? PlayerInputProfiles.Two.KeyLabel(GameAction.CastDefensive) : null, theme);

        if (match.HasOutcome)
        {
            DrawOutcomeBadge(new Rect(8f, 375f, 108f, 23f), match.PlayerOneWon, match, theme);
            DrawOutcomeBadge(new Rect(524f, 375f, 108f, 23f), !match.PlayerOneWon, match, theme);
        }

        DrawSeatPlate(SeatPlateRect(LeftBoard), "PLAYER 1", RetroPalette.SeatOne,
            match.PlayerOne, motion.PlayerOne, theme);
        DrawSeatPlate(SeatPlateRect(RightBoard), cpu ? "CPU" : "PLAYER 2", RetroPalette.SeatTwo,
            match.PlayerTwo, motion.PlayerTwo, theme);

        DrawBattleBlinker(match, theme);

        // Late in the pass so the motes fly over the gutter furniture they
        // cross rather than disappearing behind the portraits and chips.
        DrawManaMotes(LeftBoard, LeftManaBar, motion.PlayerOneMotes,
            motion.PlayerOneVitals, false);
        DrawManaMotes(RightBoard, RightManaBar, motion.PlayerTwoMotes,
            motion.PlayerTwoVitals, true);

        DrawToast(LeftBoard, match.PlayerOneCallout, match.PlayerOneCalloutAge,
            match.PlayerOneCalloutTimeLeft, false, theme);
        DrawToast(RightBoard, match.PlayerTwoCallout, match.PlayerTwoCalloutAge,
            match.PlayerTwoCalloutTimeLeft, true, theme);
    }

    // ----------------------------------------------------------------- solo

    private static void DrawSoloHud(MatchDirector match, RetroTheme theme, BattleHudMotion motion)
    {
        TetrisGameSession player = match.PlayerOne;
        bool racing = match.SoloRun != null && match.SoloRun.IsRace;

        DrawSeatFrame(SoloBoard, RetroPalette.SeatOne);

        DrawNameRibbon(
            new Rect(4f, 6f, 168f, 28f), player.DisplayName, "P1",
            RetroPalette.SeatOne, false, theme,
            match.PlayerOneCallout, match.PlayerOneCalloutAge);

        DrawHoldBox(new Rect(160f, 80f, 72f, 56f), player, motion.PlayerOne, theme);

        // In a sprint the same chip carries the target, because what the player
        // needs is the distance left, not the count so far.
        DrawStatChip(new Rect(160f, 144f, 72f, 24f), "LINES",
            racing ? motion.GoalText : motion.PlayerOne.LinesText,
            motion.PlayerOne.LinesFlash, false, theme);
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
        DrawManaBar(SoloManaBar, motion.PlayerOneVitals,
            motion.PlayerOneMotes.AbsorbStrength, false, theme);

        // Solo has no opponent, so only the defensive spell is castable.
        DrawSpellPlate(
            new Rect(160f, 266f, 72f, 13f),
            BattleCharacterRoster.Get(match.PlayerOneCharacter).DefensiveAbility,
            motion.PlayerOneVitals.DefensiveReady,
            PlayerInputProfiles.One.KeyLabel(GameAction.CastDefensive),
            theme);

        DrawNextQueue(new Rect(408f, 80f, 56f, 130f), motion.Upcoming, theme);

        // The clock is the sprint's result, so it gets a plate of its own under
        // the spell band. Marathon is scored, not timed, and keeps its old HUD.
        if (racing)
            DrawClockPlate(new Rect(160f, 285f, 72f, 38f), match, motion, theme);

        DrawSeatPlate(SeatPlateRect(SoloBoard), "PLAYER 1", RetroPalette.SeatOne,
            player, motion.PlayerOne, theme);

        DrawManaMotes(SoloBoard, SoloManaBar, motion.PlayerOneMotes,
            motion.PlayerOneVitals, false);

        DrawToast(SoloBoard, match.PlayerOneCallout, match.PlayerOneCalloutAge,
            match.PlayerOneCalloutTimeLeft, false, theme);
    }

    /// <summary>
    /// The running sprint clock. It goes gold and stops moving the moment the
    /// target is met, so the number the player came for is the one left frozen
    /// on screen behind the result banner.
    /// </summary>
    private static void DrawClockPlate(
        Rect rect,
        MatchDirector match,
        BattleHudMotion motion,
        RetroTheme theme)
    {
        bool finished = match.SoloRun.IsComplete;
        RetroGui.Panel(
            rect,
            RetroPalette.ScorePlateFill,
            finished ? RetroPalette.GoldBright : RetroPalette.BorderCyan,
            2f);

        GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, 60f, 10f), "TIME", theme.ScoreTag);

        Color previous = GUI.color;
        if (finished)
            GUI.color = RetroPalette.GoldBright;
        GUI.Label(
            new Rect(rect.x + 6f, rect.y + 12f, rect.width - 12f, 22f),
            motion.ClockText,
            theme.ChipValue);
        GUI.color = previous;
    }

    // ------------------------------------------------------------- elements

    /// <summary>
    /// Character name plus the seat chip that owns it. The ribbon is bordered
    /// in the seat colour, not the character accent, so a mirror match still
    /// reads as two different sides.
    /// </summary>
    private static void DrawNameRibbon(
        Rect rect,
        string name,
        string seatLabel,
        Color seat,
        bool rightAligned,
        RetroTheme theme,
        BattleCallout? callout,
        float calloutAge)
    {
        // A fresh SENT toast flashes the whole ribbon white for a beat.
        Color border = seat;
        if (callout.HasValue && callout.Value.Kind == BattleCalloutKind.Sent && calloutAge < 0.15f)
            border = Color.white;

        RetroGui.Panel(rect, RetroPalette.PanelFillDeep, border, 2f);

        const float chipWidth = 40f;
        Rect chip = rightAligned
            ? new Rect(rect.xMax - chipWidth - 2f, rect.y + 2f, chipWidth, rect.height - 4f)
            : new Rect(rect.x + 2f, rect.y + 2f, chipWidth, rect.height - 4f);
        RetroGui.Fill(chip, seat);
        GUI.Label(chip, seatLabel, theme.SeatTag);

        float textInset = chipWidth + 8f;
        Rect nameRect = rightAligned
            ? new Rect(rect.x + 6f, rect.y, rect.width - textInset - 6f, rect.height)
            : new Rect(rect.x + textInset, rect.y, rect.width - textInset - 6f, rect.height);
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
    /// The blue charge bar under the health bar. Line clears fill it — bigger
    /// clears and mana cells pay more — and a full bar goes rainbow and pulses,
    /// which is the tell that the cast key will actually fire.
    /// Player two's bar fills right-to-left so both read outward from center.
    /// </summary>
    private static void DrawManaBar(
        Rect rect,
        BattleVitals vitals,
        float absorb,
        bool mirrored,
        RetroTheme theme)
    {
        // The bar swells for a beat at the moment it tops out.
        float charge = vitals.ChargeStrength;
        if (charge > 0f)
        {
            float grow = Mathf.Round(charge * 2f);
            rect = new Rect(
                rect.x - grow, rect.y - grow,
                rect.width + grow * 2f, rect.height + grow * 2f);
        }

        RetroGui.Fill(rect, RetroPalette.ManaTrack);

        Rect inner = ManaBarInner(rect);
        float fillWidth = inner.width * Mathf.Clamp01(vitals.DisplayedMana);
        Rect fill = mirrored
            ? new Rect(inner.xMax - fillWidth, inner.y, fillWidth, inner.height)
            : new Rect(inner.x, inner.y, fillWidth, inner.height);

        if (!vitals.SpellReady)
        {
            DrawChargingMana(fill, mirrored);
            DrawManaAbsorb(inner, fill, absorb, mirrored);
            DrawCostTicks(inner, vitals, mirrored);
            RetroGui.Border(rect, RetroPalette.ManaBorder, 1f);
            return;
        }

        DrawChargedMana(fill);
        DrawManaAbsorb(inner, fill, absorb, mirrored);
        DrawCostTicks(inner, vitals, mirrored);

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
        RetroGui.Border(inner, new Color(1f, 1f, 1f, 0.2f + 0.35f * pulse), 1f);
        RetroGui.Border(rect, Color.Lerp(RetroPalette.ManaBorder, Color.white, pulse), 1f);
    }

    /// <summary>The track inside the bar's frame — where the fill and the notches live.</summary>
    private static Rect ManaBarInner(Rect rect)
    {
        const float Inset = 2f;
        return new Rect(
            rect.x + Inset, rect.y + Inset,
            rect.width - Inset * 2f, rect.height - Inset * 2f);
    }

    /// <summary>
    /// The point the motes fly at: the leading edge of the fill, so they land
    /// exactly where the bar is growing rather than at some fixed spot on it.
    /// </summary>
    private static Vector2 ManaFillEdge(Rect rect, float charge, bool mirrored)
    {
        Rect inner = ManaBarInner(rect);
        float offset = inner.width * Mathf.Clamp01(charge);
        return new Vector2(
            mirrored ? inner.xMax - offset : inner.x + offset,
            inner.center.y);
    }

    /// <summary>A white bloom at the fill edge for each mote the bar just swallowed.</summary>
    private static void DrawManaAbsorb(Rect inner, Rect fill, float absorb, bool mirrored)
    {
        if (absorb <= 0f)
            return;

        float width = Mathf.Round(2f + 4f * absorb);
        float edge = mirrored ? fill.x : fill.xMax;
        float x = Mathf.Clamp(edge - width * 0.5f, inner.x, inner.xMax - width);

        RetroGui.Fill(
            new Rect(Mathf.Round(x), inner.y, width, inner.height),
            new Color(1f, 1f, 1f, 0.65f * absorb));
    }

    /// <summary>
    /// A notch on the bar for each spell's cost, so the player can see how far
    /// off the next cast is instead of guessing. The notch goes bright once the
    /// bar has passed it and that spell is affordable.
    /// </summary>
    private static void DrawCostTicks(Rect inner, BattleVitals vitals, bool mirrored)
    {
        DrawCostTick(inner, vitals.OffensiveCost, vitals.OffensiveReady, mirrored);
        DrawCostTick(inner, vitals.DefensiveCost, vitals.DefensiveReady, mirrored);
    }

    private static void DrawCostTick(Rect inner, float cost, bool afforded, bool mirrored)
    {
        // A cost of 0 means the slot is empty, and a full-bar cost sits under
        // the border where a notch would only look like a rendering seam.
        if (cost <= 0f || cost >= 0.999f)
            return;

        float x = mirrored
            ? inner.xMax - inner.width * cost
            : inner.x + inner.width * cost;

        RetroGui.Fill(
            new Rect(Mathf.Round(x) - 1f, inner.y, 1f, inner.height),
            afforded ? new Color(1f, 1f, 1f, 0.85f) : new Color(0f, 0f, 0f, 0.55f));
    }

    /// <summary>Filling up: flat blue with a crest, and a bright leading lip.</summary>
    private static void DrawChargingMana(Rect fill, bool mirrored)
    {
        if (fill.width <= 0f)
            return;

        RetroGui.Fill(fill, RetroPalette.ManaFill);
        RetroGui.Fill(new Rect(fill.x, fill.y, fill.width, 2f), RetroPalette.ManaFillBright);

        if (fill.width < 2f)
            return;

        Rect lip = mirrored
            ? new Rect(fill.x, fill.y, 2f, fill.height)
            : new Rect(fill.xMax - 2f, fill.y, 2f, fill.height);
        RetroGui.Fill(lip, new Color(0.8f, 0.94f, 1f, 0.9f));
    }

    /// <summary>
    /// Charged: a scrolling rainbow with a shine sweeping over it. IMGUI has no
    /// gradients, so the rainbow is a handful of flat slices with a moving hue —
    /// cheap, and it only ever runs while a spell is actually armed. The hue
    /// comes from <see cref="ManaVisuals"/>, the same source the mana blocks and
    /// their motes draw from, because the resemblance is the whole point.
    /// </summary>
    private static void DrawChargedMana(Rect fill)
    {
        const int Slices = 14;
        if (fill.width <= 0f)
            return;

        float sliceWidth = fill.width / Slices;
        for (int i = 0; i < Slices; i++)
        {
            RetroGui.Fill(
                new Rect(fill.x + sliceWidth * i, fill.y, sliceWidth + 1f, fill.height),
                ManaVisuals.Hue(i / (float)Slices * 0.8f));
        }

        float sweep = Mathf.Repeat(Time.unscaledTime * 0.7f, 1.4f) / 1.4f;
        float shineWidth = Mathf.Min(6f, fill.width);
        RetroGui.Fill(
            new Rect(Mathf.Lerp(fill.x, fill.xMax - shineWidth, sweep), fill.y, shineWidth, fill.height),
            new Color(1f, 1f, 1f, 0.4f));
    }

    /// <summary>
    /// Flies the clear's motes from the blocks they came from into the mana
    /// bar. Each arcs out and away first, then accelerates in — the pull is
    /// what says the bar is taking the charge, not just receiving it.
    /// </summary>
    private static void DrawManaMotes(
        Rect board,
        Rect bar,
        ManaMoteField field,
        BattleVitals vitals,
        bool mirrored)
    {
        if (field.Count == 0)
            return;

        Vector2 target = ManaFillEdge(bar, vitals.DisplayedMana, mirrored);

        for (int i = 0; i < field.Count; i++)
        {
            ManaMoteField.Mote mote = field[i];
            float progress = mote.Progress;
            if (progress < 0f)
                continue;

            Vector2 start = new Vector2(
                board.x + mote.U * board.width,
                board.yMax - mote.V * board.height);
            Vector2 control = (start + target) * 0.5f + new Vector2(mote.ArcX, mote.ArcY);

            Color color = mote.IsMana
                ? ManaVisuals.Cell(mote.Phase, 1f)
                : mote.Color;

            // The trail is drawn first and behind, oldest sample faintest.
            for (int step = TrailSamples; step >= 1; step--)
            {
                float trailProgress = progress - step * TrailSpacing;
                if (trailProgress <= 0f)
                    continue;

                DrawMoteQuad(
                    Bezier(start, control, target, Ease(trailProgress)),
                    MoteSize(mote, trailProgress) - step,
                    new Color(color.r, color.g, color.b, 0.4f / step));
            }

            // Only the last stretch fades, so the mote reads as landing in the
            // bar rather than expiring somewhere short of it.
            float alpha = 1f - 0.45f * Mathf.InverseLerp(0.88f, 1f, progress);
            DrawMoteQuad(
                Bezier(start, control, target, Ease(progress)),
                MoteSize(mote, progress),
                new Color(color.r, color.g, color.b, alpha));

            if (mote.IsMana)
            {
                DrawMoteQuad(
                    Bezier(start, control, target, Ease(progress)),
                    MoteSize(mote, progress) - 2f,
                    new Color(1f, 1f, 1f, 0.8f * alpha));
            }
        }
    }

    /// <summary>
    /// Quadratic ease-in: the mote hangs at the block it came from for a beat
    /// before the bar yanks it in. Linear motion reads as drifting.
    /// </summary>
    private static float Ease(float progress)
    {
        return progress * progress;
    }

    private static Vector2 Bezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    private static float MoteSize(ManaMoteField.Mote mote, float progress)
    {
        return Mathf.Round(Mathf.Lerp(mote.Size, 1f, progress));
    }

    /// <summary>Pixel-snapped so the motes stay as crisp as the rest of the HUD.</summary>
    private static void DrawMoteQuad(Vector2 center, float size, Color color)
    {
        if (size < 1f || color.a <= 0f)
            return;

        RetroGui.Fill(
            new Rect(
                Mathf.Round(center.x - size * 0.5f),
                Mathf.Round(center.y - size * 0.5f),
                size,
                size),
            color);
    }

    /// <summary>
    /// One of the character's two spells, banded across the top of their
    /// portrait so what the mana bar is paying toward is always legible. It
    /// takes the spell's own accent, dims while unaffordable, and pulses with
    /// its cast key the moment the bar can pay for it.
    /// </summary>
    private static void DrawSpellPlate(
        Rect rect,
        MagicAbilityDefinition ability,
        bool ready,
        string castPrompt,
        RetroTheme theme)
    {
        RetroGui.Fill(rect, new Color(0f, 0f, 0f, 0.72f));
        if (ability == null)
            return;

        Color accent = ability.Accent;
        Color previous = GUI.color;

        if (ready)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            accent = Color.Lerp(accent, Color.white, pulse);
            RetroGui.Border(rect, accent, 1f);
        }
        else
        {
            accent = new Color(accent.r, accent.g, accent.b, 0.45f);
        }

        GUI.color = accent;
        GUI.Label(rect, ability.DisplayName, theme.ScoreTagCentered);

        // The key rides on the plate rather than the bar: with two spells, the
        // bar cannot say which key does what.
        if (!string.IsNullOrEmpty(castPrompt))
        {
            GUI.Label(
                new Rect(rect.xMax - 14f, rect.y, 12f, rect.height),
                castPrompt,
                theme.ScoreTagCentered);
        }

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

    /// <summary>
    /// A seat-coloured bracket around the playfield. Nothing else on screen is
    /// this close to the cells a player is watching, so it is the cue that
    /// survives once their eyes never leave the board.
    /// </summary>
    private static void DrawSeatFrame(Rect board, Color seat)
    {
        // Runs from just above the playfield down to the plate top, so the
        // frame's bottom edge and the plate's top edge stack into one line.
        float top = board.y - SeatFrameInset;
        Rect frame = SeatColumn(board, top, SeatPlateTop - top);
        RetroGui.Border(frame, new Color(seat.r, seat.g, seat.b, 0.85f), 2f);
    }

    /// <summary>Where a board's seat plate sits: flush under its seat frame.</summary>
    private static Rect SeatPlateRect(Rect board)
    {
        return SeatColumn(board, SeatPlateTop, SeatPlateHeight);
    }

    /// <summary>
    /// The hero element: one plate under each board carrying the seat banner
    /// (PLAYER 1 / PLAYER 2 / CPU) above that seat's score, so "whose board is
    /// this" and "how am I doing" land in the same glance.
    /// </summary>
    private static void DrawSeatPlate(
        Rect rect,
        string seatLabel,
        Color seatColor,
        TetrisGameSession session,
        BattleHudMotion.Seat seat,
        RetroTheme theme)
    {
        bool punching = seat.Punch > 0f;
        Rect drawRect = punching
            ? new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f)
            : rect;

        RetroGui.Fill(drawRect, RetroPalette.ScorePlateFill);
        // The punch grows the plate but never the banner: the one label a lost
        // player is looking for must not move.
        RetroGui.Border(drawRect, punching ? Color.white : seatColor, 2f);

        Rect banner = new Rect(rect.x, rect.y, rect.width, SeatBannerHeight);
        RetroGui.Fill(banner, seatColor);
        GUI.Label(banner, seatLabel, theme.SeatBanner);

        GUI.Label(new Rect(rect.x + 6f, banner.yMax + 2f, 60f, 10f), "SCORE", theme.ScoreTag);

        // Fixed-width digits so the count-up never makes the number jitter.
        bool ticking = seat.RoundedScore != session.Score;
        Color previous = GUI.color;
        if (ticking || punching)
            GUI.color = RetroPalette.GoldBright;
        GUI.Label(
            new Rect(rect.x + 6f, banner.yMax + 12f, rect.width - 12f, 24f),
            seat.ScoreText,
            theme.ScoreValue);
        GUI.color = previous;
    }

    private static void DrawBattleBlinker(MatchDirector match, RetroTheme theme)
    {
        // Sits on the seat banner row so the bottom band reads
        // PLAYER 1 | BATTLE | PLAYER 2 straight across.
        Rect rect = new Rect(296f, SeatPlateTop, 48f, SeatBannerHeight);
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

        if (match.SoloRun == null)
            return $"{match.WinnerName} IS THE WINNER";

        // A finished sprint reports the clock — that is the whole score. Any
        // other way a solo run ends is a top-out.
        return match.SoloRun.IsComplete
            ? $"{match.SoloRun.LineTarget} LINES IN {SoloRun.FormatTime(match.SoloRun.Elapsed)}"
            : "GAME OVER";
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

        // Name each side over its own board before the first piece drops. This
        // is the beat where a player decides which half of the screen is theirs,
        // and the scrim means nothing else is competing for the look.
        if (match.PlayerTwo != null)
        {
            bool cpu = match.Mode == TetrisGameMode.VersusCpu;
            DrawSeatCallout(
                LeftBoard,
                "PLAYER 1",
                BuildSeatMoveKeys(PlayerInputProfiles.One),
                RetroPalette.SeatOne,
                theme);
            DrawSeatCallout(
                RightBoard,
                cpu ? "CPU" : "PLAYER 2",
                cpu ? CpuKeys : BuildSeatMoveKeys(PlayerInputProfiles.Two),
                RetroPalette.SeatTwo,
                theme);
        }

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

    /// <summary>
    /// "This board is yours, and these are your keys" — parked high enough on
    /// the board to clear the READY/START panel below it.
    /// </summary>
    private static void DrawSeatCallout(
        Rect board,
        string seatLabel,
        string keys,
        Color seat,
        RetroTheme theme)
    {
        Rect panel = SeatColumn(board, 92f, 58f);
        RetroGui.Fill(panel, RetroPalette.OverlayPanel);
        RetroGui.Border(panel, seat, 3f);
        RetroGui.Fill(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 2f), seat);

        Color previous = GUI.color;
        GUI.color = seat;
        GUI.Label(new Rect(panel.x, panel.y + 12f, panel.width, 24f), seatLabel, theme.SeatCallout);
        GUI.color = previous;

        GUI.Label(new Rect(panel.x + 4f, panel.y + 36f, panel.width - 8f, 16f), keys, theme.Help);
    }

    private static string BuildMatchupLabel(MatchDirector match)
    {
        string one = match.PlayerOne != null ? match.PlayerOne.DisplayName : "P1";
        if (match.PlayerTwo == null)
            return match.PlayerOne != null ? match.PlayerOne.DisplayName : "PLAYER";

        return $"{one}  VS  {match.PlayerTwo.DisplayName}";
    }
}
