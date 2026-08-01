using UnityEngine;

public enum MainMenuPage
{
    Root,
    CpuDifficulty
}

public enum MainMenuIntent
{
    None,
    StartStory,
    OpenCharacterSelect
}

/// <summary>What activating the current menu item asked the game to do.</summary>
public readonly struct MainMenuCommand
{
    private MainMenuCommand(MainMenuIntent intent, TetrisGameMode versusMode)
    {
        Intent = intent;
        VersusMode = versusMode;
    }

    public MainMenuIntent Intent { get; }
    public TetrisGameMode VersusMode { get; }

    public static MainMenuCommand None => new MainMenuCommand(MainMenuIntent.None, TetrisGameMode.Solo);

    public static MainMenuCommand StartStory =>
        new MainMenuCommand(MainMenuIntent.StartStory, TetrisGameMode.Solo);

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
    public const int RootItemCount = 3;
    public const int DifficultyItemCount = 4;
    private const int BackItemIndex = 3;

    public MainMenuPage Page { get; private set; } = MainMenuPage.Root;
    public int Selection { get; private set; }
    public CpuDifficulty Difficulty { get; set; } = CpuDifficulty.Easy;

    public int ItemCount => Page == MainMenuPage.Root ? RootItemCount : DifficultyItemCount;

    public void ShowRoot(int selection = 0)
    {
        Page = MainMenuPage.Root;
        Selection = Mathf.Clamp(selection, 0, RootItemCount - 1);
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
    }

    public void Select(int index)
    {
        Selection = Mathf.Clamp(index, 0, ItemCount - 1);
    }

    /// <summary>Escape/B. Returns true when the menu consumed the press.</summary>
    public bool Back()
    {
        if (Page != MainMenuPage.CpuDifficulty)
            return false;

        ShowRoot(1);
        return true;
    }

    public MainMenuCommand Activate()
    {
        return Activate(Selection);
    }

    public MainMenuCommand Activate(int index)
    {
        Select(index);

        if (Page == MainMenuPage.Root)
        {
            switch (Selection)
            {
                case 0:
                    return MainMenuCommand.StartStory;
                case 1:
                    ShowCpuDifficulty();
                    return MainMenuCommand.None;
                default:
                    return MainMenuCommand.OpenCharacterSelect(TetrisGameMode.LocalVersus);
            }
        }

        if (Selection == BackItemIndex)
        {
            ShowRoot(1);
            return MainMenuCommand.None;
        }

        Difficulty = (CpuDifficulty)Selection;
        return MainMenuCommand.OpenCharacterSelect(TetrisGameMode.VersusCpu);
    }
}
