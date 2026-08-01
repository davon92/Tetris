using UnityEngine.InputSystem;

/// <summary>
/// One frame of device-independent menu navigation. Sampling once per frame
/// keeps every screen reading the same edges and removes the four near-identical
/// keyboard/gamepad polling blocks the old monolithic game manager carried.
/// </summary>
public readonly struct UiInput
{
    public UiInput(bool up, bool down, bool left, bool right, bool confirm, bool cancel, bool pause)
    {
        Up = up;
        Down = down;
        Left = left;
        Right = right;
        Confirm = confirm;
        Cancel = cancel;
        Pause = pause;
    }

    public bool Up { get; }
    public bool Down { get; }
    public bool Left { get; }
    public bool Right { get; }
    public bool Confirm { get; }
    public bool Cancel { get; }

    /// <summary>Escape or the gamepad Start button: leave whatever is running.</summary>
    public bool Pause { get; }

    public static UiInput Sample()
    {
        Keyboard keyboard = Keyboard.current;

        bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        bool up = keyboard != null &&
            (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
        bool down = keyboard != null &&
            (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
        bool left = keyboard != null &&
            (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
        bool right = keyboard != null &&
            (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
        bool confirm = keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame);
        bool cancel = escape;
        bool pause = escape;

        // Every connected pad can drive menus — in local versus either player
        // should be able to navigate — and the left stick counts as a d-pad.
        var gamepads = Gamepad.all;
        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad gamepad = gamepads[i];
            up |= gamepad.dpad.up.wasPressedThisFrame ||
                gamepad.leftStick.up.wasPressedThisFrame;
            down |= gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.leftStick.down.wasPressedThisFrame;
            left |= gamepad.dpad.left.wasPressedThisFrame ||
                gamepad.leftStick.left.wasPressedThisFrame;
            right |= gamepad.dpad.right.wasPressedThisFrame ||
                gamepad.leftStick.right.wasPressedThisFrame;
            confirm |= gamepad.buttonSouth.wasPressedThisFrame;
            cancel |= gamepad.buttonEast.wasPressedThisFrame;
            pause |= gamepad.startButton.wasPressedThisFrame;
        }

        return new UiInput(up, down, left, right, confirm, cancel, pause);
    }
}
