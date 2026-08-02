using UnityEngine;

public enum OptionsPage
{
    Root,
    Audio,
    Graphics,
    Controls
}

/// <summary>
/// Navigation and edit state for the options screen. Pure C# with no Unity
/// input or rendering dependencies, matching <see cref="MainMenuModel"/>, so
/// the page flow and the rebind handshake are both testable.
/// </summary>
public sealed class OptionsModel
{
    public const int RootItemCount = 4;
    public const int AudioItemCount = 4;
    public const int GraphicsItemCount = 5;

    /// <summary>Seat row, one row per action, then reset and back.</summary>
    public const int ControlsItemCount = 1 + GameActionInfo.Count + 2;

    private const int ControlsSeatRow = 0;
    private const int ControlsFirstActionRow = 1;

    public OptionsPage Page { get; private set; } = OptionsPage.Root;
    public int Selection { get; private set; }

    /// <summary>Which seat the controls page is editing.</summary>
    public bool EditingPlayerOne { get; private set; } = true;

    /// <summary>The action waiting for a key or button, or null.</summary>
    public GameAction? Listening { get; private set; }

    /// <summary>
    /// The confirm press that opened the capture is still down on the frame it
    /// opens, so the first frame is swallowed rather than binding Enter to
    /// whatever row the player was on.
    /// </summary>
    public bool ListeningArmed { get; private set; }

    public int ItemCount => Page switch
    {
        OptionsPage.Root => RootItemCount,
        OptionsPage.Audio => AudioItemCount,
        OptionsPage.Graphics => GraphicsItemCount,
        _ => ControlsItemCount
    };

    /// <summary>The bindings the controls page is currently editing.</summary>
    public PlayerInputBindings EditingBindings => PlayerInputProfiles.For(EditingPlayerOne);

    public void ShowRoot(int selection = 0)
    {
        Page = OptionsPage.Root;
        Selection = Mathf.Clamp(selection, 0, RootItemCount - 1);
        Listening = null;
    }

    public void Show(OptionsPage page)
    {
        Page = page;
        Selection = 0;
        Listening = null;
    }

    public void Move(int delta)
    {
        if (Listening.HasValue)
            return;

        int count = ItemCount;
        Selection = (Selection + delta % count + count) % count;
    }

    public void Select(int index)
    {
        if (Listening.HasValue)
            return;

        Selection = Mathf.Clamp(index, 0, ItemCount - 1);
    }

    /// <summary>The action on the current controls row, or null on a non-action row.</summary>
    public GameAction? ActionAt(int row)
    {
        if (Page != OptionsPage.Controls)
            return null;

        int index = row - ControlsFirstActionRow;
        return index >= 0 && index < GameActionInfo.Count ? (GameAction)index : null;
    }

    public bool IsResetRow(int row) =>
        Page == OptionsPage.Controls && row == ControlsFirstActionRow + GameActionInfo.Count;

    public bool IsBackRow(int row)
    {
        return Page switch
        {
            OptionsPage.Root => row == RootItemCount - 1,
            OptionsPage.Audio => row == AudioItemCount - 1,
            OptionsPage.Graphics => row == GraphicsItemCount - 1,
            _ => row == ControlsItemCount - 1
        };
    }

    public bool IsSeatRow(int row) => Page == OptionsPage.Controls && row == ControlsSeatRow;

    /// <summary>Escape/B. Returns true when the options screen consumed the press.</summary>
    public bool Back()
    {
        if (Listening.HasValue)
        {
            Listening = null;
            return true;
        }

        if (Page == OptionsPage.Root)
            return false;

        ShowRoot(PageRow(Page));
        return true;
    }

    /// <summary>Left/right on a row that holds a value. Returns true if it changed one.</summary>
    public bool Adjust(int delta)
    {
        if (Listening.HasValue)
            return false;

        switch (Page)
        {
            case OptionsPage.Audio:
                return AdjustAudio(delta);
            case OptionsPage.Graphics:
                return AdjustGraphics(delta);
            case OptionsPage.Controls when IsSeatRow(Selection):
                EditingPlayerOne = !EditingPlayerOne;
                return true;
            default:
                return false;
        }
    }

    private bool AdjustAudio(int delta)
    {
        switch (Selection)
        {
            case 0:
                GameSettings.SetMusicVolume(GameSettings.MusicVolume + delta * 0.05f);
                return true;
            case 1:
                GameSettings.SetSfxVolume(GameSettings.SfxVolume + delta * 0.05f);
                return true;
            case 2:
                GameSettings.SetMuted(!GameSettings.Muted);
                return true;
            default:
                return false;
        }
    }

    private bool AdjustGraphics(int delta)
    {
        switch (Selection)
        {
            case 0:
                GameSettings.SetFullscreen(!GameSettings.Fullscreen);
                return true;
            case 1:
                GameSettings.StepResolution(delta);
                return true;
            case 2:
                GameSettings.SetVSync(!GameSettings.VSync);
                return true;
            case 3:
                GameSettings.StepQuality(delta);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Enter/A on the current row.</summary>
    public OptionsCommand Activate()
    {
        return Activate(Selection);
    }

    public OptionsCommand Activate(int index)
    {
        if (Listening.HasValue)
            return OptionsCommand.None;

        Select(index);

        if (Page == OptionsPage.Root)
        {
            switch (Selection)
            {
                case 0:
                    Show(OptionsPage.Audio);
                    return OptionsCommand.None;
                case 1:
                    Show(OptionsPage.Graphics);
                    return OptionsCommand.None;
                case 2:
                    Show(OptionsPage.Controls);
                    return OptionsCommand.None;
                default:
                    return OptionsCommand.Close;
            }
        }

        if (IsBackRow(Selection))
        {
            ShowRoot(PageRow(Page));
            return OptionsCommand.None;
        }

        if (Page == OptionsPage.Controls)
        {
            if (IsSeatRow(Selection))
            {
                EditingPlayerOne = !EditingPlayerOne;
                return OptionsCommand.Changed;
            }

            if (IsResetRow(Selection))
            {
                PlayerInputProfiles.ResetAll();
                return OptionsCommand.Changed;
            }

            GameAction? action = ActionAt(Selection);
            if (action.HasValue)
            {
                Listening = action;
                ListeningArmed = false;
                return OptionsCommand.Changed;
            }

            return OptionsCommand.None;
        }

        // Audio and graphics rows are toggles or sliders; confirm nudges them
        // the same way right does, so the mouse can drive them too.
        return Adjust(1) ? OptionsCommand.Changed : OptionsCommand.None;
    }

    /// <summary>Called once per frame while a capture is open, before polling.</summary>
    public void ArmListening()
    {
        if (Listening.HasValue)
            ListeningArmed = true;
    }

    /// <summary>Binds the captured key and closes the capture.</summary>
    public void CompleteRebind(UnityEngine.InputSystem.Key key)
    {
        if (!Listening.HasValue)
            return;

        EditingBindings.SetKey(Listening.Value, key);
        PlayerInputProfiles.SaveAll();
        Listening = null;
    }

    /// <summary>Binds the captured pad button and closes the capture.</summary>
    public void CompleteRebind(PadButton button)
    {
        if (!Listening.HasValue)
            return;

        EditingBindings.SetPad(Listening.Value, button);
        PlayerInputProfiles.SaveAll();
        Listening = null;
    }

    /// <summary>Where a sub-page sits on the root list, so backing out lands on it.</summary>
    private static int PageRow(OptionsPage page)
    {
        return page switch
        {
            OptionsPage.Audio => 0,
            OptionsPage.Graphics => 1,
            _ => 2
        };
    }
}

public enum OptionsCommand
{
    None,

    /// <summary>A value changed — the screen plays a blip.</summary>
    Changed,

    /// <summary>Leave the options screen.</summary>
    Close
}
