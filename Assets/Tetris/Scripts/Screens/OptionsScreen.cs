using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Drives the options screen: page navigation, value adjustment, and the
/// rebind capture. Settings apply the moment they change and persist on exit,
/// so a player who alt-F4s out of the menu keeps what they heard and saw.
/// </summary>
public sealed class OptionsScreen : IGameScreen
{
    private readonly OptionsModel model;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;

    public OptionsScreen(OptionsModel model, IGameFlow flow, RetroTheme theme)
    {
        this.model = model;
        this.flow = flow;
        this.theme = theme;
    }

    public void Enter()
    {
        model.ShowRoot();
    }

    public void Exit()
    {
        GameSettings.Save();
        PlayerInputProfiles.SaveAll();
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        // A capture swallows the whole frame — otherwise the confirm that
        // opened it, or the arrow the player reaches for, would move the
        // cursor underneath the modal.
        if (model.Listening.HasValue)
        {
            TickCapture();
            return;
        }

        if (input.Cancel)
        {
            GameAudio.Play(GameSfx.MenuBack);
            if (!model.Back())
                flow.CloseOptions();

            return;
        }

        if (input.Up)
        {
            model.Move(-1);
            GameAudio.Play(GameSfx.MenuMove);
        }
        else if (input.Down)
        {
            model.Move(1);
            GameAudio.Play(GameSfx.MenuMove);
        }

        if (input.Left && model.Adjust(-1))
            GameAudio.Play(GameSfx.MenuMove);
        else if (input.Right && model.Adjust(1))
            GameAudio.Play(GameSfx.MenuMove);

        if (input.Confirm)
            Activate(model.Activate());
    }

    /// <summary>
    /// Listens for the next key or pad button and binds it. Both devices are
    /// live at once, so the player just presses what they want on whichever
    /// one they are holding rather than picking a device first.
    /// </summary>
    private void TickCapture()
    {
        if (!model.ListeningArmed)
        {
            model.ArmListening();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                model.Back();
                GameAudio.Play(GameSfx.MenuBack);
                return;
            }

            foreach (KeyControl control in keyboard.allKeys)
            {
                if (!control.wasPressedThisFrame)
                    continue;

                model.CompleteRebind(control.keyCode);
                GameAudio.Play(GameSfx.MenuConfirm);
                return;
            }
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            foreach (PadButton button in PadButtonInfo.All)
            {
                if (PadButtonInfo.Resolve(gamepad, button)?.wasPressedThisFrame != true)
                    continue;

                model.CompleteRebind(button);
                GameAudio.Play(GameSfx.MenuConfirm);
                return;
            }
        }
    }

    public void Draw()
    {
        int clicked = OptionsView.Draw(model, theme);
        if (clicked == OptionsView.NoClick)
            return;

        Activate(model.Activate(clicked));
    }

    private void Activate(OptionsCommand command)
    {
        switch (command)
        {
            case OptionsCommand.Close:
                GameAudio.Play(GameSfx.MenuBack);
                flow.CloseOptions();
                break;
            case OptionsCommand.Changed:
                GameAudio.Play(GameSfx.MenuConfirm);
                break;
            default:
                GameAudio.Play(GameSfx.MenuConfirm);
                break;
        }
    }
}
