/// <summary>
/// The navigation requests a screen can make. Screens depend on this narrow
/// interface rather than on <see cref="GameManager"/>, so they can be driven
/// by a test double.
/// </summary>
public interface IGameFlow
{
    /// <summary>Tear everything down and return to the title menu root page.</summary>
    void ShowTitleMenu();

    void ShowCharacterSelect(TetrisGameMode versusMode);

    /// <summary>Back out of the roster picker to the page it was opened from.</summary>
    void CloseCharacterSelect();

    void BeginStory();

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
