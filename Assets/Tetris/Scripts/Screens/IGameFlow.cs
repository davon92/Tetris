/// <summary>
/// The navigation requests a screen can make. Screens depend on this narrow
/// interface rather than on a concrete flow controller, so they can be driven
/// by a test double.
/// </summary>
public interface IGameFlow
{
    /// <summary>Tear everything down and return to the title menu root page.</summary>
    void ShowTitleMenu();

    void ShowCharacterSelect(TetrisGameMode versusMode);

    /// <summary>Back out of the roster picker to the page it was opened from.</summary>
    void CloseCharacterSelect();

    void ShowOptions();

    /// <summary>Back out of options to the title menu row it was opened from.</summary>
    void CloseOptions();

    /// <summary>Start the chapter from the beginning, discarding any run in progress.</summary>
    void BeginStory();

    /// <summary>Open the save-slot browser from the title menu, in load mode.</summary>
    void ShowLoadGame();

    /// <summary>Back out of the load browser to the story page of the title menu.</summary>
    void CloseLoadGame();

    /// <summary>Write the running chapter to a slot. False when nothing was stored.</summary>
    bool SaveStory(int slot);

    /// <summary>
    /// Restore a chapter from a slot and show the story screen. False when the
    /// slot is empty, unreadable, or belongs to a different chapter.
    /// </summary>
    bool LoadStory(int slot);

    /// <summary>Ask the story bridge for this chapter's battle.</summary>
    void RequestStoryBattle();

    /// <summary>
    /// Start a match in the given mode using the current character and
    /// difficulty selections, leaving story mode if it was running.
    /// </summary>
    void BeginMatch(TetrisGameMode mode);

    /// <summary>Replay the current match with the same setup.</summary>
    void RestartMatch();
}
