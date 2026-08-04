/// <summary>
/// Everything needed to put a <see cref="StoryDirector"/> back where it was.
/// Deliberately a plain value: the save layer serialises it and the director
/// restores it, and neither one learns about the other.
/// </summary>
public readonly struct StoryProgress
{
    public StoryProgress(
        string chapterId,
        StoryBeat beat,
        int lineIndex,
        int response,
        int selection,
        bool battleWon,
        float playtimeSeconds)
    {
        ChapterId = chapterId ?? string.Empty;
        Beat = beat;
        LineIndex = lineIndex;
        Response = response;
        Selection = selection;
        BattleWon = battleWon;
        PlaytimeSeconds = playtimeSeconds;
    }

    public string ChapterId { get; }
    public StoryBeat Beat { get; }
    public int LineIndex { get; }
    public int Response { get; }
    public int Selection { get; }
    public bool BattleWon { get; }
    public float PlaytimeSeconds { get; }
}
