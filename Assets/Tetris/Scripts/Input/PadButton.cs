using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// The gamepad controls a player can bind to, named by position rather than by
/// an Xbox/PlayStation face label so the enum survives whichever pad is
/// plugged in. Analog stick directions are deliberately absent: the left stick
/// always steers, and that is not something worth letting a player unbind.
/// </summary>
public enum PadButton
{
    None,
    South,
    East,
    West,
    North,
    LeftShoulder,
    RightShoulder,
    LeftTrigger,
    RightTrigger,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,
    LeftStickPress,
    RightStickPress,
    Start,
    Select
}

public static class PadButtonInfo
{
    /// <summary>Every bindable button, for the rebind capture sweep.</summary>
    public static readonly PadButton[] All =
    {
        PadButton.South, PadButton.East, PadButton.West, PadButton.North,
        PadButton.LeftShoulder, PadButton.RightShoulder,
        PadButton.LeftTrigger, PadButton.RightTrigger,
        PadButton.DpadUp, PadButton.DpadDown, PadButton.DpadLeft, PadButton.DpadRight,
        PadButton.LeftStickPress, PadButton.RightStickPress,
        PadButton.Start, PadButton.Select
    };

    /// <summary>Xbox lettering, since that is what the control diagrams use.</summary>
    public static string Label(PadButton button)
    {
        return button switch
        {
            PadButton.South => "A",
            PadButton.East => "B",
            PadButton.West => "X",
            PadButton.North => "Y",
            PadButton.LeftShoulder => "LB",
            PadButton.RightShoulder => "RB",
            PadButton.LeftTrigger => "LT",
            PadButton.RightTrigger => "RT",
            PadButton.DpadUp => "D-UP",
            PadButton.DpadDown => "D-DN",
            PadButton.DpadLeft => "D-LF",
            PadButton.DpadRight => "D-RT",
            PadButton.LeftStickPress => "LS",
            PadButton.RightStickPress => "RS",
            PadButton.Start => "START",
            PadButton.Select => "SELECT",
            _ => "—"
        };
    }

    public static ButtonControl Resolve(Gamepad gamepad, PadButton button)
    {
        if (gamepad == null)
            return null;

        return button switch
        {
            PadButton.South => gamepad.buttonSouth,
            PadButton.East => gamepad.buttonEast,
            PadButton.West => gamepad.buttonWest,
            PadButton.North => gamepad.buttonNorth,
            PadButton.LeftShoulder => gamepad.leftShoulder,
            PadButton.RightShoulder => gamepad.rightShoulder,
            PadButton.LeftTrigger => gamepad.leftTrigger,
            PadButton.RightTrigger => gamepad.rightTrigger,
            PadButton.DpadUp => gamepad.dpad.up,
            PadButton.DpadDown => gamepad.dpad.down,
            PadButton.DpadLeft => gamepad.dpad.left,
            PadButton.DpadRight => gamepad.dpad.right,
            PadButton.LeftStickPress => gamepad.leftStickButton,
            PadButton.RightStickPress => gamepad.rightStickButton,
            PadButton.Start => gamepad.startButton,
            PadButton.Select => gamepad.selectButton,
            _ => null
        };
    }
}
