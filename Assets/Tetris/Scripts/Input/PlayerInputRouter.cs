using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Turns one seat's keyboard and gamepad state into <see cref="TetrisCommand"/>s,
/// including delayed auto-shift and soft-drop repeat.
/// </summary>
/// <remarks>
/// Keyboard and gamepad are merged into a single held/pressed query before the
/// repeat state is advanced. The previous implementation ran two independent
/// passes over one shared timer, so an idle connected gamepad reset the
/// keyboard's auto-shift every frame and repeats never fired.
/// </remarks>
public sealed class PlayerInputRouter
{
    private const float HorizontalDelay = 0.16f;
    private const float HorizontalRepeat = 0.055f;
    private const float SoftDropDelay = 0.055f;
    private const float SoftDropRepeat = 0.035f;

    private readonly PlayerInputBindings bindings;

    private int horizontalDirection;
    private float horizontalTimer;
    private float softDropTimer;

    public PlayerInputRouter(PlayerInputBindings bindings)
    {
        this.bindings = bindings;
    }

    public void Reset()
    {
        horizontalDirection = 0;
        horizontalTimer = 0f;
        softDropTimer = 0f;
    }

    public void Poll(TetrisGameSession session, float deltaTime)
    {
        if (session == null)
        {
            Reset();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.all.Count > bindings.GamepadIndex
            ? Gamepad.all[bindings.GamepadIndex]
            : null;

        // The left stick shadows the four directional actions no matter what
        // they are bound to. Steering is not worth making a player rebind, and
        // plenty of them never touch the d-pad at all.
        bool leftPressed = WasPressed(keyboard, gamepad, GameAction.MoveLeft) ||
            WasPressed(gamepad?.leftStick.left);
        bool rightPressed = WasPressed(keyboard, gamepad, GameAction.MoveRight) ||
            WasPressed(gamepad?.leftStick.right);
        bool leftHeld = IsHeld(keyboard, gamepad, GameAction.MoveLeft) ||
            IsHeld(gamepad?.leftStick.left);
        bool rightHeld = IsHeld(keyboard, gamepad, GameAction.MoveRight) ||
            IsHeld(gamepad?.leftStick.right);
        UpdateHorizontal(session, leftPressed, rightPressed, leftHeld, rightHeld, deltaTime);

        bool dropPressed = WasPressed(keyboard, gamepad, GameAction.SoftDrop) ||
            WasPressed(gamepad?.leftStick.down);
        bool dropHeld = IsHeld(keyboard, gamepad, GameAction.SoftDrop) ||
            IsHeld(gamepad?.leftStick.down);
        UpdateSoftDrop(session, dropPressed, dropHeld, deltaTime);

        if (WasPressed(keyboard, gamepad, GameAction.HardDrop) ||
            WasPressed(gamepad?.leftStick.up))
            Apply(session, TetrisCommand.HardDrop);

        Dispatch(session, keyboard, gamepad, GameAction.RotateClockwise);
        Dispatch(session, keyboard, gamepad, GameAction.RotateCounterClockwise);
        Dispatch(session, keyboard, gamepad, GameAction.Hold);
        Dispatch(session, keyboard, gamepad, GameAction.CastOffensive);
        Dispatch(session, keyboard, gamepad, GameAction.CastDefensive);
    }

    private void Dispatch(
        TetrisGameSession session,
        Keyboard keyboard,
        Gamepad gamepad,
        GameAction action)
    {
        if (WasPressed(keyboard, gamepad, action))
            Apply(session, GameActionInfo.ToCommand(action));
    }

    private void UpdateHorizontal(
        TetrisGameSession session,
        bool leftPressed,
        bool rightPressed,
        bool leftHeld,
        bool rightHeld,
        float deltaTime)
    {
        int direction = leftHeld == rightHeld ? 0 : leftHeld ? -1 : 1;

        if (leftPressed || rightPressed)
        {
            Apply(session, leftPressed ? TetrisCommand.MoveLeft : TetrisCommand.MoveRight);
            horizontalDirection = direction;
            horizontalTimer = HorizontalDelay;
            return;
        }

        if (direction == 0)
        {
            horizontalDirection = 0;
            horizontalTimer = 0f;
            return;
        }

        if (direction != horizontalDirection)
        {
            horizontalDirection = direction;
            horizontalTimer = HorizontalDelay;
            return;
        }

        horizontalTimer -= deltaTime;
        if (horizontalTimer > 0f)
            return;

        Apply(session, direction < 0 ? TetrisCommand.MoveLeft : TetrisCommand.MoveRight);
        horizontalTimer = HorizontalRepeat;
    }

    private void UpdateSoftDrop(
        TetrisGameSession session,
        bool pressed,
        bool held,
        float deltaTime)
    {
        if (pressed)
        {
            Apply(session, TetrisCommand.SoftDrop);
            softDropTimer = SoftDropDelay;
            return;
        }

        if (!held)
        {
            softDropTimer = 0f;
            return;
        }

        softDropTimer -= deltaTime;
        if (softDropTimer > 0f)
            return;

        Apply(session, TetrisCommand.SoftDrop);
        softDropTimer = SoftDropRepeat;
    }

    /// <summary>
    /// A command can end the match, so the session is re-checked before every
    /// dispatch rather than once per frame.
    /// </summary>
    private static void Apply(TetrisGameSession session, TetrisCommand command)
    {
        if (session != null)
            session.ApplyCommand(command);
    }

    private bool WasPressed(Keyboard keyboard, Gamepad gamepad, GameAction action)
    {
        Key key = bindings.GetKey(action);
        if (keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame)
            return true;

        return WasPressed(PadButtonInfo.Resolve(gamepad, bindings.GetPad(action)));
    }

    private bool IsHeld(Keyboard keyboard, Gamepad gamepad, GameAction action)
    {
        Key key = bindings.GetKey(action);
        if (keyboard != null && key != Key.None && keyboard[key].isPressed)
            return true;

        return IsHeld(PadButtonInfo.Resolve(gamepad, bindings.GetPad(action)));
    }

    private static bool WasPressed(ButtonControl button)
    {
        return button != null && button.wasPressedThisFrame;
    }

    private static bool IsHeld(ButtonControl button)
    {
        return button != null && button.isPressed;
    }
}
