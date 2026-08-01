using UnityEngine.InputSystem;

/// <summary>
/// The keyboard layout and gamepad slot for one player seat. Declaring the
/// layout as data means <see cref="PlayerInputRouter"/> has a single polling
/// path instead of one hand-written method per player.
/// </summary>
public sealed class PlayerInputBindings
{
    private static readonly Key[] None = new Key[0];

    public PlayerInputBindings(
        int gamepadIndex,
        Key[] left,
        Key[] right,
        Key[] softDrop,
        Key[] rotateClockwise,
        Key[] rotateCounterClockwise,
        Key[] hardDrop,
        Key[] hold)
    {
        GamepadIndex = gamepadIndex;
        Left = left ?? None;
        Right = right ?? None;
        SoftDrop = softDrop ?? None;
        RotateClockwise = rotateClockwise ?? None;
        RotateCounterClockwise = rotateCounterClockwise ?? None;
        HardDrop = hardDrop ?? None;
        Hold = hold ?? None;
    }

    public int GamepadIndex { get; }
    public Key[] Left { get; }
    public Key[] Right { get; }
    public Key[] SoftDrop { get; }
    public Key[] RotateClockwise { get; }
    public Key[] RotateCounterClockwise { get; }
    public Key[] HardDrop { get; }
    public Key[] Hold { get; }

    public static PlayerInputBindings PlayerOne()
    {
        return new PlayerInputBindings(
            gamepadIndex: 0,
            left: new[] { Key.A },
            right: new[] { Key.D },
            softDrop: new[] { Key.S },
            rotateClockwise: new[] { Key.W },
            rotateCounterClockwise: new[] { Key.Q },
            hardDrop: new[] { Key.Space },
            hold: new[] { Key.LeftShift, Key.C });
    }

    public static PlayerInputBindings PlayerTwo()
    {
        return new PlayerInputBindings(
            gamepadIndex: 1,
            left: new[] { Key.LeftArrow },
            right: new[] { Key.RightArrow },
            softDrop: new[] { Key.DownArrow },
            rotateClockwise: new[] { Key.UpArrow },
            rotateCounterClockwise: new[] { Key.RightCtrl },
            hardDrop: new[] { Key.Enter, Key.NumpadEnter },
            hold: new[] { Key.RightShift });
    }
}
