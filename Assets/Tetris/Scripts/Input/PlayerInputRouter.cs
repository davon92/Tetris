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

        bool leftPressed = WasPressed(keyboard, bindings.Left) || WasPressed(gamepad?.dpad.left);
        bool rightPressed = WasPressed(keyboard, bindings.Right) || WasPressed(gamepad?.dpad.right);
        bool leftHeld = IsHeld(keyboard, bindings.Left) || IsHeld(gamepad?.dpad.left);
        bool rightHeld = IsHeld(keyboard, bindings.Right) || IsHeld(gamepad?.dpad.right);
        UpdateHorizontal(session, leftPressed, rightPressed, leftHeld, rightHeld, deltaTime);

        bool dropPressed = WasPressed(keyboard, bindings.SoftDrop) || WasPressed(gamepad?.dpad.down);
        bool dropHeld = IsHeld(keyboard, bindings.SoftDrop) || IsHeld(gamepad?.dpad.down);
        UpdateSoftDrop(session, dropPressed, dropHeld, deltaTime);

        if (WasPressed(keyboard, bindings.RotateClockwise) || WasPressed(gamepad?.buttonSouth))
            Apply(session, TetrisCommand.RotateClockwise);

        if (WasPressed(keyboard, bindings.RotateCounterClockwise) || WasPressed(gamepad?.buttonWest))
            Apply(session, TetrisCommand.RotateCounterClockwise);

        if (WasPressed(keyboard, bindings.HardDrop) || WasPressed(gamepad?.buttonEast))
            Apply(session, TetrisCommand.HardDrop);

        if (WasPressed(keyboard, bindings.Hold) ||
            WasPressed(gamepad?.leftShoulder) ||
            WasPressed(gamepad?.rightShoulder))
            Apply(session, TetrisCommand.Hold);
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

    private static bool WasPressed(Keyboard keyboard, Key[] keys)
    {
        if (keyboard == null)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keyboard[keys[i]].wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private static bool IsHeld(Keyboard keyboard, Key[] keys)
    {
        if (keyboard == null)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keyboard[keys[i]].isPressed)
                return true;
        }

        return false;
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
