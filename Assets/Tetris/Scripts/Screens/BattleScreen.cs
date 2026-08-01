using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives one battle: player input, the ready/start/result presentation beats,
/// and the dev shortcuts (1/2/3 mode switch, R retry) carried over from the
/// original implementation.
/// </summary>
public sealed class BattleScreen : IGameScreen
{
    private readonly MatchDirector match;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;
    private readonly BattleArtLibrary art;
    private readonly PlayerInputRouter playerOneInput = new PlayerInputRouter(PlayerInputBindings.PlayerOne());
    private readonly PlayerInputRouter playerTwoInput = new PlayerInputRouter(PlayerInputBindings.PlayerTwo());

    public BattleScreen(MatchDirector match, IGameFlow flow, RetroTheme theme, BattleArtLibrary art)
    {
        this.match = match;
        this.flow = flow;
        this.theme = theme;
        this.art = art;
    }

    public void Enter()
    {
        playerOneInput.Reset();
        playerTwoInput.Reset();
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        if (match.AdvancePhase(Time.unscaledDeltaTime))
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                flow.BeginMatch(TetrisGameMode.Solo);
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

            if (keyboard.rKey.wasPressedThisFrame)
            {
                flow.RestartMatch();
                return;
            }
        }

        playerOneInput.Poll(match.PlayerOne, deltaTime);
        if (match.Mode == TetrisGameMode.LocalVersus)
            playerTwoInput.Poll(match.PlayerTwo, deltaTime);

        match.Tick(deltaTime);
    }

    public void Draw()
    {
        BattleHudView.Draw(match, theme, art);
    }
}
