using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One seat's control layout: a keyboard key and a pad button per
/// <see cref="GameAction"/>. Holding the layout as data rather than as code at
/// a call site is what lets the options screen edit it, the router poll it, and
/// the HUD print the right key prompt — all from one source.
/// </summary>
/// <remarks>
/// Player one's defaults are the shipped scheme:
/// <code>
/// Q rotate left    W rotate right   A/D move      S fast fall
/// SPACE hard drop  E hold           F offensive   C defensive
/// Pad: A rotate left, B rotate right, Y hold, RB offensive, LB defensive,
///      d-pad up hard drop, d-pad down fast fall, d-pad/stick move.
/// </code>
/// </remarks>
public sealed class PlayerInputBindings
{
    private const string PrefsPrefix = "input-binding-";

    private readonly Key[] keys = new Key[GameActionInfo.Count];
    private readonly PadButton[] pad = new PadButton[GameActionInfo.Count];

    public PlayerInputBindings(string seatId, int gamepadIndex)
    {
        SeatId = seatId;
        GamepadIndex = gamepadIndex;
        ResetToDefaults();
    }

    /// <summary>Stable key for the saved profile. Never change it once it ships.</summary>
    public string SeatId { get; }

    public int GamepadIndex { get; }

    public bool IsPlayerOne => GamepadIndex == 0;

    public Key GetKey(GameAction action) => keys[(int)action];

    public PadButton GetPad(GameAction action) => pad[(int)action];

    public string KeyLabel(GameAction action) => KeyDisplay.Label(keys[(int)action]);

    public string PadLabel(GameAction action) => PadButtonInfo.Label(pad[(int)action]);

    /// <summary>
    /// Binds a key, swapping with whatever action already held it. Swapping
    /// rather than refusing means a player reorganising a layout never gets
    /// stuck needing a spare key to park a binding on.
    /// </summary>
    public void SetKey(GameAction action, Key key)
    {
        if (key == Key.None)
            return;

        int slot = (int)action;
        for (int i = 0; i < keys.Length; i++)
        {
            if (i != slot && keys[i] == key)
                keys[i] = keys[slot];
        }

        keys[slot] = key;
    }

    public void SetPad(GameAction action, PadButton button)
    {
        if (button == PadButton.None)
            return;

        int slot = (int)action;
        for (int i = 0; i < pad.Length; i++)
        {
            if (i != slot && pad[i] == button)
                pad[i] = pad[slot];
        }

        pad[slot] = button;
    }

    public void ResetToDefaults()
    {
        if (IsPlayerOne)
            ApplyPlayerOneDefaults();
        else
            ApplyPlayerTwoDefaults();
    }

    private void ApplyPlayerOneDefaults()
    {
        Set(GameAction.MoveLeft, Key.A, PadButton.DpadLeft);
        Set(GameAction.MoveRight, Key.D, PadButton.DpadRight);
        Set(GameAction.SoftDrop, Key.S, PadButton.DpadDown);
        Set(GameAction.HardDrop, Key.Space, PadButton.DpadUp);
        Set(GameAction.RotateClockwise, Key.W, PadButton.East);
        Set(GameAction.RotateCounterClockwise, Key.Q, PadButton.South);
        Set(GameAction.Hold, Key.E, PadButton.North);
        Set(GameAction.CastOffensive, Key.F, PadButton.RightShoulder);
        Set(GameAction.CastDefensive, Key.C, PadButton.LeftShoulder);
    }

    private void ApplyPlayerTwoDefaults()
    {
        Set(GameAction.MoveLeft, Key.LeftArrow, PadButton.DpadLeft);
        Set(GameAction.MoveRight, Key.RightArrow, PadButton.DpadRight);
        Set(GameAction.SoftDrop, Key.DownArrow, PadButton.DpadDown);
        Set(GameAction.HardDrop, Key.Enter, PadButton.DpadUp);
        Set(GameAction.RotateClockwise, Key.UpArrow, PadButton.East);
        Set(GameAction.RotateCounterClockwise, Key.RightCtrl, PadButton.South);
        Set(GameAction.Hold, Key.RightShift, PadButton.North);
        Set(GameAction.CastOffensive, Key.Slash, PadButton.RightShoulder);
        Set(GameAction.CastDefensive, Key.Period, PadButton.LeftShoulder);
    }

    private void Set(GameAction action, Key key, PadButton button)
    {
        keys[(int)action] = key;
        pad[(int)action] = button;
    }

    public void Save()
    {
        for (int i = 0; i < GameActionInfo.Count; i++)
        {
            PlayerPrefs.SetInt($"{PrefsPrefix}{SeatId}-key-{i}", (int)keys[i]);
            PlayerPrefs.SetInt($"{PrefsPrefix}{SeatId}-pad-{i}", (int)pad[i]);
        }
    }

    /// <summary>
    /// Reads the saved profile over the defaults. A missing or nonsense entry
    /// keeps the default, so a profile saved by an older build with fewer
    /// actions still loads cleanly.
    /// </summary>
    public void Load()
    {
        ResetToDefaults();

        for (int i = 0; i < GameActionInfo.Count; i++)
        {
            int savedKey = PlayerPrefs.GetInt($"{PrefsPrefix}{SeatId}-key-{i}", (int)keys[i]);
            if (savedKey > (int)Key.None && System.Enum.IsDefined(typeof(Key), savedKey))
                keys[i] = (Key)savedKey;

            int savedPad = PlayerPrefs.GetInt($"{PrefsPrefix}{SeatId}-pad-{i}", (int)pad[i]);
            if (savedPad > (int)PadButton.None && savedPad <= (int)PadButton.Select)
                pad[i] = (PadButton)savedPad;
        }
    }
}

/// <summary>
/// The two seats, shared by the router that reads them, the options screen that
/// edits them, and the HUD that prints their prompts.
/// </summary>
public static class PlayerInputProfiles
{
    public static PlayerInputBindings One { get; } = new PlayerInputBindings("p1", 0);
    public static PlayerInputBindings Two { get; } = new PlayerInputBindings("p2", 1);

    public static PlayerInputBindings For(bool playerOne) => playerOne ? One : Two;

    public static void LoadAll()
    {
        One.Load();
        Two.Load();
    }

    public static void SaveAll()
    {
        One.Save();
        Two.Save();
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        One.ResetToDefaults();
        Two.ResetToDefaults();
        SaveAll();
    }
}

/// <summary>Readable names for the keys a prompt or rebind row has to print.</summary>
public static class KeyDisplay
{
    public static string Label(Key key)
    {
        return key switch
        {
            Key.None => "—",
            Key.Space => "SPACE",
            Key.Enter => "ENTER",
            Key.NumpadEnter => "NUM-ENT",
            Key.LeftShift => "L-SHIFT",
            Key.RightShift => "R-SHIFT",
            Key.LeftCtrl => "L-CTRL",
            Key.RightCtrl => "R-CTRL",
            Key.LeftAlt => "L-ALT",
            Key.RightAlt => "R-ALT",
            Key.UpArrow => "UP",
            Key.DownArrow => "DOWN",
            Key.LeftArrow => "LEFT",
            Key.RightArrow => "RIGHT",
            Key.Slash => "/",
            Key.Backslash => "\\",
            Key.Period => ".",
            Key.Comma => ",",
            Key.Semicolon => ";",
            Key.Quote => "'",
            Key.Minus => "-",
            Key.Equals => "=",
            Key.LeftBracket => "[",
            Key.RightBracket => "]",
            Key.Backquote => "`",
            Key.Tab => "TAB",
            Key.Backspace => "BKSP",
            _ => StripPrefix(key.ToString()).ToUpperInvariant()
        };
    }

    /// <summary>"Digit1" and "Numpad4" read badly on a 40px binding chip.</summary>
    private static string StripPrefix(string name)
    {
        if (name.StartsWith("Digit"))
            return name.Substring(5);

        return name.StartsWith("Numpad") ? "NUM" + name.Substring(6) : name;
    }
}
