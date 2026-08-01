using UnityEngine.InputSystem;

/// <summary>
/// One frame of device-independent menu navigation. Sampling once per frame
/// keeps every screen reading the same edges and removes the four near-identical
/// keyboard/gamepad polling blocks the old GameManager carried.
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
        Gamepad gamepad = Gamepad.all.Count > 0 ? Gamepad.all[0] : null;

        bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        return new UiInput(
            up:
            (keyboard != null &&
             (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.up.wasPressedThisFrame),
            down:
            (keyboard != null &&
             (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.down.wasPressedThisFrame),
            left:
            (keyboard != null &&
             (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.left.wasPressedThisFrame),
            right:
            (keyboard != null &&
             (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.right.wasPressedThisFrame),
            confirm:
            (keyboard != null &&
             (keyboard.enterKey.wasPressedThisFrame ||
              keyboard.numpadEnterKey.wasPressedThisFrame ||
              keyboard.spaceKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame),
            cancel: escape || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame),
            pause: escape || (gamepad != null && gamepad.startButton.wasPressedThisFrame));
    }
}
