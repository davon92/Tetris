using UnityEngine;

/// <summary>
/// The objective and the clock for one solo run. Marathon has no target and
/// only ends when the board tops out; Sprint is a race, so the run ends the
/// instant the line target is met and the elapsed time is the result.
///
/// Lives beside the match rather than inside <see cref="TetrisGameSession"/>
/// because the board plays identically in both modes — only the reason the
/// match stops differs. Pure state with no Unity dependencies beyond the maths,
/// so the rule is exercisable without a live board.
/// </summary>
public sealed class SoloRun
{
    /// <summary>
    /// The genre-standard sprint distance: long enough that one buried well
    /// costs the run, short enough to want an immediate retry.
    /// </summary>
    public const int DefaultSprintTarget = 40;

    private SoloRun(int lineTarget)
    {
        LineTarget = Mathf.Max(0, lineTarget);
    }

    /// <summary>Endless. Nothing ends this but a top-out.</summary>
    public static SoloRun Marathon()
    {
        return new SoloRun(0);
    }

    /// <summary>A race to <paramref name="lineTarget"/> cleared lines.</summary>
    public static SoloRun Sprint(int lineTarget)
    {
        return new SoloRun(Mathf.Max(1, lineTarget));
    }

    /// <summary>Lines this run owes, or 0 when it is endless.</summary>
    public int LineTarget { get; }

    /// <summary>True when the run is finished by a line target instead of a top-out.</summary>
    public bool IsRace => LineTarget > 0;

    /// <summary>Seconds of gameplay. Stops the moment the run is decided.</summary>
    public float Elapsed { get; private set; }

    /// <summary>Lines cleared so far, mirrored off the board.</summary>
    public int Lines { get; private set; }

    /// <summary>Lines still to clear. Always 0 in marathon.</summary>
    public int LinesRemaining => IsRace ? Mathf.Max(0, LineTarget - Lines) : 0;

    /// <summary>The target was met. Never true in marathon.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// Advances the clock and re-reads the board's line count.
    /// </summary>
    /// <returns>
    /// True on the single frame the target is met, so the caller ends the match
    /// there — the clock must not run on into the result beat.
    /// </returns>
    public bool Tick(float deltaTime, int lines)
    {
        if (IsComplete)
            return false;

        Elapsed += deltaTime;
        Lines = lines;

        if (!IsRace || Lines < LineTarget)
            return false;

        IsComplete = true;
        return true;
    }

    /// <summary>
    /// Race-clock digits, "M:SS.hh". Formatting lives with the value rather
    /// than in a view because both the HUD readout and the result copy have to
    /// print the same time the same way.
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int hundredths = Mathf.FloorToInt(Mathf.Max(0f, seconds) * 100f);
        return $"{hundredths / 6000}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
    }
}
