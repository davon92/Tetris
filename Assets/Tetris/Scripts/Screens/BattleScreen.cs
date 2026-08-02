using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives one battle: player input, the ready/start/result presentation beats,
/// the post-match rematch/menu modal, and the dev shortcuts (1/2/3 mode
/// switch, R retry) carried over from the original implementation.
/// </summary>
public sealed class BattleScreen : IGameScreen
{
    private const int ResultOptionCount = 2;

    private readonly MatchDirector match;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;
    private readonly BattleArtLibrary art;
    // The shared profiles, so a rebind in the options screen takes effect the
    // next time a match reads them without any re-wiring.
    private readonly PlayerInputRouter playerOneInput = new PlayerInputRouter(PlayerInputProfiles.One);
    private readonly PlayerInputRouter playerTwoInput = new PlayerInputRouter(PlayerInputProfiles.Two);
    private readonly BattleHudMotion hudMotion = new BattleHudMotion();

    private MatchPhase lastPhase = MatchPhase.Idle;
    private int resultSelection;
    private TetrisGameSession subscribedOne;
    private TetrisGameSession subscribedTwo;

    public BattleScreen(MatchDirector match, IGameFlow flow, RetroTheme theme, BattleArtLibrary art)
    {
        this.match = match;
        this.flow = flow;
        this.theme = theme;
        this.art = art;
    }

    /// <summary>
    /// Story battles hand the outcome back to the story flow, so only free
    /// matches offer the rematch/menu choice.
    /// </summary>
    private bool ResultModalActive =>
        match.Phase == MatchPhase.Finished && !match.IsStoryBattle;

    public void Enter()
    {
        playerOneInput.Reset();
        playerTwoInput.Reset();
        hudMotion.Reset();
        lastPhase = match.Phase;
        resultSelection = 0;
        SubscribeReactions();
    }

    public void Exit()
    {
        UnsubscribeReactions();
    }

    /// <summary>
    /// Faces react to what happens to their own board. Sessions are rebuilt per
    /// match, so this re-binds whenever the pair changes.
    /// </summary>
    private void SubscribeReactions()
    {
        if (subscribedOne == match.PlayerOne && subscribedTwo == match.PlayerTwo)
            return;

        UnsubscribeReactions();
        subscribedOne = match.PlayerOne;
        subscribedTwo = match.PlayerTwo;
        Bind(subscribedOne, true);
        Bind(subscribedTwo, true);
    }

    private void UnsubscribeReactions()
    {
        Bind(subscribedOne, false);
        Bind(subscribedTwo, false);
        subscribedOne = null;
        subscribedTwo = null;
    }

    private void Bind(TetrisGameSession session, bool subscribe)
    {
        if (session == null)
            return;

        if (subscribe)
        {
            session.GarbageApplied += OnHurt;
            session.AbilityResolved += OnSpellLanded;
            session.AbilityCast += OnCast;
        }
        else
        {
            session.GarbageApplied -= OnHurt;
            session.AbilityResolved -= OnSpellLanded;
            session.AbilityCast -= OnCast;
        }
    }

    private void OnHurt(TetrisGameSession session, int lines)
    {
        hudMotion.VitalsFor(session, match).React(PortraitMood.Hurt);
    }

    /// <summary>A mend lands on the caster, so it reads as a cast, not a hit.</summary>
    private void OnSpellLanded(
        TetrisGameSession session,
        MagicAbilityDefinition ability,
        Vector2Int[] cells)
    {
        if (ability.Effect == MagicEffect.Mend)
        {
            GameAudio.Play(GameSfx.Heal);
            return;
        }

        hudMotion.VitalsFor(session, match).React(PortraitMood.Hurt);
        GameAudio.Play(GameSfx.SpellHit);
    }

    private void OnCast(TetrisGameSession session, MagicAbilityDefinition ability)
    {
        hudMotion.VitalsFor(session, match).React(PortraitMood.Casting);
        GameAudio.Play(GameSfx.SpellCast);
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        hudMotion.Tick(match.PlayerOne, match.PlayerTwo, match.SoloRun, deltaTime);

        // The shortcuts must run even while the presentation owns the frame:
        // R on the result screen has to work.
        bool presentationOwnsFrame = match.AdvancePhase(Time.unscaledDeltaTime);

        SubscribeReactions();

        if (match.Phase != lastPhase)
        {
            if (match.Phase == MatchPhase.Finished)
                resultSelection = 0;

            if (match.Phase == MatchPhase.Result && match.HasOutcome)
            {
                hudMotion.PlayerOneVitals.SetOutcome(match.PlayerOneWon);
                if (match.PlayerTwo != null)
                    hudMotion.PlayerTwoVitals.SetOutcome(!match.PlayerOneWon);
            }

            PlayPhaseStinger(match.Phase);
            lastPhase = match.Phase;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                flow.BeginMatch(TetrisGameMode.Marathon);
                return;
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                flow.BeginMatch(TetrisGameMode.VersusCpu);
                return;
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                flow.BeginMatch(TetrisGameMode.LocalVersus);
                return;
            }

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                flow.BeginMatch(TetrisGameMode.Sprint);
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                flow.RestartMatch();
                return;
            }
        }

        if (ResultModalActive)
        {
            if (input.Up || input.Down)
            {
                resultSelection = (resultSelection + 1) % ResultOptionCount;
                GameAudio.Play(GameSfx.MenuMove);
            }

            if (input.Confirm)
            {
                GameAudio.Play(GameSfx.MenuConfirm);
                ActivateResultOption(resultSelection);
                return;
            }

            if (input.Cancel)
            {
                GameAudio.Play(GameSfx.MenuBack);
                flow.ShowTitleMenu();
                return;
            }
        }

        if (presentationOwnsFrame)
            return;

        playerOneInput.Poll(match.PlayerOne, deltaTime);
        if (match.Mode == TetrisGameMode.LocalVersus)
            playerTwoInput.Poll(match.PlayerTwo, deltaTime);

        match.Tick(deltaTime);
    }

    public void Draw()
    {
        // The battle HUD is labels and textures only — no control IDs — so
        // Layout and input passes would run the whole draw for nothing.
        // The exception is the post-match modal: its buttons need mouse
        // events, so the gate lifts while it is up. Menu screens use
        // GUI.Button everywhere and must never get this gate.
        bool modalActive = ResultModalActive;
        if (!modalActive && Event.current.type != EventType.Repaint)
            return;

        int clicked = BattleHudView.Draw(
            match, theme, art, hudMotion, modalActive ? resultSelection : -1);
        if (clicked != BattleHudView.NoClick)
            ActivateResultOption(clicked);
    }

    private void ActivateResultOption(int option)
    {
        if (option == 0)
            flow.RestartMatch();
        else
            flow.ShowTitleMenu();
    }

    /// <summary>
    /// One-shot sting per beat. The win/lose fanfare plays on Result, where the
    /// banner appears, rather than on Finished when the modal takes over.
    /// </summary>
    private void PlayPhaseStinger(MatchPhase phase)
    {
        switch (phase)
        {
            case MatchPhase.Ready:
                GameAudio.Play(GameSfx.Ready);
                break;
            case MatchPhase.Start:
                GameAudio.Play(GameSfx.Start);
                break;
            // Solo used to be treated as a win outright; with a sprint there is
            // now a real difference between finishing the run and topping out,
            // and the director sets PlayerOneWon for exactly that.
            case MatchPhase.Result when match.HasOutcome:
                GameAudio.Play(match.PlayerOneWon ? GameSfx.Win : GameSfx.Lose);
                break;
        }
    }
}
