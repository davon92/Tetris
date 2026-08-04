using UnityEngine;

public enum MainMenuPage
{
    Root,

    /// <summary>New game / load game, shown before the prologue starts.</summary>
    Story,
    SoloMode,
    VersusMode,
    CpuDifficulty
}

public enum MainMenuIntent
{
    None,
    StartStory,

    /// <summary>Open the save-slot browser to continue a stored adventure.</summary>
    OpenLoadGame,
    StartSolo,
    OpenCharacterSelect,
    OpenOptions
}

/// <summary>What activating the current menu item asked the game to do.</summary>
public readonly struct MainMenuCommand
{
    private MainMenuCommand(MainMenuIntent intent, TetrisGameMode mode)
    {
        Intent = intent;
        Mode = mode;
    }

    public MainMenuIntent Intent { get; }

    /// <summary>
    /// The mode the intent applies to — the solo variant to start, or the
    /// versus variant the roster picker is being opened for. Meaningless for
    /// the intents that carry no mode.
    /// </summary>
    public TetrisGameMode Mode { get; }

    public static MainMenuCommand None => new MainMenuCommand(MainMenuIntent.None, TetrisGameMode.Marathon);

    public static MainMenuCommand StartStory =>
        new MainMenuCommand(MainMenuIntent.StartStory, TetrisGameMode.Marathon);

    public static MainMenuCommand OpenLoadGame =>
        new MainMenuCommand(MainMenuIntent.OpenLoadGame, TetrisGameMode.Marathon);

    public static MainMenuCommand OpenOptions =>
        new MainMenuCommand(MainMenuIntent.OpenOptions, TetrisGameMode.Marathon);

    public static MainMenuCommand StartSolo(TetrisGameMode soloMode)
    {
        return new MainMenuCommand(MainMenuIntent.StartSolo, soloMode);
    }

    public static MainMenuCommand OpenCharacterSelect(TetrisGameMode versusMode)
    {
        return new MainMenuCommand(MainMenuIntent.OpenCharacterSelect, versusMode);
    }
}

/// <summary>
/// Navigation state for the title menu. Pure C# with no Unity input or
/// rendering dependencies, so the whole flow is covered by EditMode tests.
/// </summary>
public sealed class MainMenuModel
{
    public const int RootItemCount = 4;
    public const int StoryItemCount = 3;
    public const int SoloItemCount = 3;
    public const int VersusItemCount = 3;
    public const int DifficultyItemCount = 4;

    // Rows are named because callers outside this class have to return the
    // cursor to the row a sub-page was opened from, and a bare literal there
    // silently rots the moment the root page gains an item.
    public const int StoryRow = 0;
    public const int SoloRow = 1;
    public const int VersusRow = 2;
    public const int OptionsRow = 3;

    /// <summary>Rows on the story sub-page, which the flow returns the cursor to.</summary>
    public const int NewGameRow = 0;

    public const int LoadGameRow = 1;

    /// <summary>Rows on the versus sub-page, which the flow returns the cursor to.</summary>
    public const int VersusCpuRow = 0;

    public const int VersusPlayerRow = 1;

    public const string NoSavesMessage = "NO SAVED ADVENTURES YET";

    private const int SprintRow = 0;
    private const int MarathonRow = 1;
    private const int SoloBackRow = 2;
    private const int DifficultyBackRow = 3;

    public MainMenuPage Page { get; private set; } = MainMenuPage.Root;
    public int Selection { get; private set; }
    public CpuDifficulty Difficulty { get; set; } = CpuDifficulty.Easy;

    /// <summary>
    /// The solo variant last chosen. Remembered so reopening the page lands on
    /// it, the same way the difficulty page opens on the current difficulty.
    /// </summary>
    public TetrisGameMode SoloMode { get; private set; } = TetrisGameMode.Sprint;

    /// <summary>
    /// Refreshed by the flow controller whenever the menu is shown. Load game
    /// stays visible without any saves, but says so instead of opening an
    /// empty list.
    /// </summary>
    public bool HasAnySave { get; set; }

    /// <summary>One line of feedback for a request the menu declined.</summary>
    public string Message { get; private set; } = string.Empty;

    public int ItemCount => Page switch
    {
        MainMenuPage.Story => StoryItemCount,
        MainMenuPage.SoloMode => SoloItemCount,
        MainMenuPage.VersusMode => VersusItemCount,
        MainMenuPage.CpuDifficulty => DifficultyItemCount,
        _ => RootItemCount
    };

    public void ShowRoot(int selection = 0)
    {
        Page = MainMenuPage.Root;
        Selection = Mathf.Clamp(selection, 0, RootItemCount - 1);
        Message = string.Empty;
    }

    /// <summary>Opens the story page, optionally landing on a specific row.</summary>
    public void ShowStory(int selection = NewGameRow)
    {
        Page = MainMenuPage.Story;
        Selection = Mathf.Clamp(selection, 0, StoryItemCount - 1);
        Message = string.Empty;
    }

    public void ShowSoloMode()
    {
        Page = MainMenuPage.SoloMode;
        Selection = SoloMode == TetrisGameMode.Sprint ? SprintRow : MarathonRow;
    }

    /// <summary>Opens the versus page, optionally landing on a specific row.</summary>
    public void ShowVersusMode(int selection = VersusCpuRow)
    {
        Page = MainMenuPage.VersusMode;
        Selection = Mathf.Clamp(selection, 0, VersusItemCount - 1);
    }

    public void ShowCpuDifficulty()
    {
        Page = MainMenuPage.CpuDifficulty;
        Selection = (int)Difficulty;
    }

    public void Move(int delta)
    {
        int count = ItemCount;
        Selection = (Selection + delta % count + count) % count;
        Message = string.Empty;
    }

    public void Select(int index)
    {
        Selection = Mathf.Clamp(index, 0, ItemCount - 1);
    }

    /// <summary>Escape/B. Returns true when the menu consumed the press.</summary>
    public bool Back()
    {
        switch (Page)
        {
            case MainMenuPage.Story:
                ShowRoot(StoryRow);
                return true;
            case MainMenuPage.SoloMode:
                ShowRoot(SoloRow);
                return true;
            case MainMenuPage.VersusMode:
                ShowRoot(VersusRow);
                return true;
            case MainMenuPage.CpuDifficulty:
                // Difficulty hangs off the versus page now, so backing out of it
                // lands one step up rather than all the way at the root.
                ShowVersusMode(VersusCpuRow);
                return true;
            default:
                return false;
        }
    }

    public MainMenuCommand Activate()
    {
        return Activate(Selection);
    }

    public MainMenuCommand Activate(int index)
    {
        Select(index);
        Message = string.Empty;

        return Page switch
        {
            MainMenuPage.Story => ActivateStory(),
            MainMenuPage.SoloMode => ActivateSoloMode(),
            MainMenuPage.VersusMode => ActivateVersusMode(),
            MainMenuPage.CpuDifficulty => ActivateDifficulty(),
            _ => ActivateRoot()
        };
    }

    private MainMenuCommand ActivateRoot()
    {
        switch (Selection)
        {
            case StoryRow:
                ShowStory();
                return MainMenuCommand.None;
            case SoloRow:
                ShowSoloMode();
                return MainMenuCommand.None;
            case VersusRow:
                ShowVersusMode();
                return MainMenuCommand.None;
            default:
                return MainMenuCommand.OpenOptions;
        }
    }

    /// <summary>
    /// The chapter can be started fresh or resumed from a slot. Load stays
    /// selectable with nothing stored so the option never vanishes; it just
    /// says why it did nothing.
    /// </summary>
    private MainMenuCommand ActivateStory()
    {
        switch (Selection)
        {
            case NewGameRow:
                return MainMenuCommand.StartStory;

            case LoadGameRow:
                if (HasAnySave)
                    return MainMenuCommand.OpenLoadGame;

                Message = NoSavesMessage;
                return MainMenuCommand.None;

            default:
                ShowRoot(StoryRow);
                return MainMenuCommand.None;
        }
    }

    /// <summary>
    /// Versus against a CPU still needs a difficulty picked first; against a
    /// second player it goes straight to the roster.
    /// </summary>
    private MainMenuCommand ActivateVersusMode()
    {
        switch (Selection)
        {
            case VersusCpuRow:
                ShowCpuDifficulty();
                return MainMenuCommand.None;
            case VersusPlayerRow:
                return MainMenuCommand.OpenCharacterSelect(TetrisGameMode.LocalVersus);
            default:
                ShowRoot(VersusRow);
                return MainMenuCommand.None;
        }
    }

    /// <summary>
    /// Solo skips the roster picker: with nobody to fight, the character only
    /// supplies the defensive spell, so there is nothing to choose between yet.
    /// </summary>
    private MainMenuCommand ActivateSoloMode()
    {
        if (Selection == SoloBackRow)
        {
            ShowRoot(SoloRow);
            return MainMenuCommand.None;
        }

        SoloMode = Selection == SprintRow ? TetrisGameMode.Sprint : TetrisGameMode.Marathon;
        return MainMenuCommand.StartSolo(SoloMode);
    }

    private MainMenuCommand ActivateDifficulty()
    {
        if (Selection == DifficultyBackRow)
        {
            ShowVersusMode(VersusCpuRow);
            return MainMenuCommand.None;
        }

        Difficulty = (CpuDifficulty)Selection;
        return MainMenuCommand.OpenCharacterSelect(TetrisGameMode.VersusCpu);
    }
}
