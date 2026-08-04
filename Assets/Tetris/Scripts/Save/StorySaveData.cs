using System;

/// <summary>
/// The serialised contents of one story slot. Public fields because
/// <c>JsonUtility</c> only sees fields; the rest of the game reads slots
/// through <see cref="SaveSlotInfo"/> and <see cref="StoryProgress"/>.
/// </summary>
[Serializable]
public sealed class StorySaveData
{
    public const int CurrentVersion = 1;

    /// <summary>How much of a line the slot list is willing to show.</summary>
    private const int PreviewLength = 72;

    public int version = CurrentVersion;
    public string chapterId = string.Empty;
    public string chapterTitle = string.Empty;
    public string speaker = string.Empty;
    public string previewText = string.Empty;
    public int beat;
    public int lineIndex;
    public int response;
    public int selection;
    public bool battleWon;
    public float playtimeSeconds;
    public long savedAtUtcTicks;

    /// <summary>Snapshots a running chapter, dialogue preview included.</summary>
    public static StorySaveData Capture(StoryDirector story, DateTime savedAtUtc)
    {
        if (story == null)
            throw new ArgumentNullException(nameof(story));

        StoryProgress progress = story.Capture();
        StoryLine line = story.CurrentLine;

        return new StorySaveData
        {
            version = CurrentVersion,
            chapterId = progress.ChapterId,
            chapterTitle = story.Script.LocationTitle,
            speaker = line.Speaker ?? string.Empty,
            previewText = Truncate(line.Text, PreviewLength),
            beat = (int)progress.Beat,
            lineIndex = progress.LineIndex,
            response = progress.Response,
            selection = progress.Selection,
            battleWon = progress.BattleWon,
            playtimeSeconds = progress.PlaytimeSeconds,
            savedAtUtcTicks = savedAtUtc.Ticks
        };
    }

    public StoryProgress ToProgress()
    {
        return new StoryProgress(
            chapterId,
            ToBeat(beat),
            lineIndex,
            response,
            selection,
            battleWon,
            playtimeSeconds);
    }

    public DateTime SavedAtUtc =>
        savedAtUtcTicks > 0 && savedAtUtcTicks <= DateTime.MaxValue.Ticks
            ? new DateTime(savedAtUtcTicks, DateTimeKind.Utc)
            : DateTime.MinValue;

    /// <summary>
    /// A slot written by a newer build, or corrupted on disk, must not take the
    /// story screen down — such slots are listed but refuse to load.
    /// </summary>
    public bool IsUsable =>
        version > 0 &&
        version <= CurrentVersion &&
        !string.IsNullOrEmpty(chapterId);

    private static StoryBeat ToBeat(int value)
    {
        return Enum.IsDefined(typeof(StoryBeat), value) ? (StoryBeat)value : StoryBeat.Opening;
    }

    private static string Truncate(string text, int length)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= length ? text : text.Substring(0, length - 1).TrimEnd() + "…";
    }
}
