using UnityEngine;

/// <summary>
/// The face a fighter is pulling right now. Sustained moods come from how full
/// the board is; reaction moods are transient and outrank them for a moment.
/// </summary>
public enum PortraitMood
{
    /// <summary>Healthy and ready to battle.</summary>
    Ready,

    /// <summary>Board is filling up — visibly working.</summary>
    Strained,

    /// <summary>Near the top. Panic.</summary>
    Critical,

    /// <summary>Casting a spell.</summary>
    Casting,

    /// <summary>Just took garbage or ate a spell.</summary>
    Hurt,

    Victory,
    Defeat
}

/// <summary>
/// Reads a board as a fighting-game health bar. Board fill is damage; garbage
/// rows are the recoverable grey portion, so clearing garbage wins health back
/// exactly like chip damage in Tekken or Street Fighter.
/// </summary>
public sealed class BattleVitals
{
    private const float BarCatchUp = 6f;
    private const float ReactionDuration = 0.75f;
    private const float StrainedThreshold = 0.62f;
    private const float CriticalThreshold = 0.34f;

    /// <summary>Health the player currently holds, 0..1. Empty board is full health.</summary>
    public float Health { get; private set; } = 1f;

    /// <summary>Smoothed <see cref="Health"/> for drawing the bar.</summary>
    public float DisplayedHealth { get; private set; } = 1f;

    /// <summary>Recoverable damage sitting on the board as garbage, 0..1.</summary>
    public float Recoverable { get; private set; }

    /// <summary>Smoothed <see cref="Recoverable"/> for drawing the bar.</summary>
    public float DisplayedRecoverable { get; private set; }

    public PortraitMood Mood { get; private set; } = PortraitMood.Ready;

    /// <summary>1 at the instant a reaction fired, decaying to 0. Drives shake and flash.</summary>
    public float ReactionStrength =>
        reactionTimer <= 0f ? 0f : reactionTimer / ReactionDuration;

    private PortraitMood reactionMood = PortraitMood.Ready;
    private float reactionTimer;
    private bool outcomeLatched;

    public void Reset()
    {
        Health = 1f;
        DisplayedHealth = 1f;
        Recoverable = 0f;
        DisplayedRecoverable = 0f;
        Mood = PortraitMood.Ready;
        reactionMood = PortraitMood.Ready;
        reactionTimer = 0f;
        outcomeLatched = false;
    }

    /// <summary>Plays a one-shot reaction face that outranks the health mood.</summary>
    public void React(PortraitMood mood)
    {
        reactionMood = mood;
        reactionTimer = ReactionDuration;
    }

    /// <summary>Latches the end-of-match face, which outranks everything.</summary>
    public void SetOutcome(bool won)
    {
        Mood = won ? PortraitMood.Victory : PortraitMood.Defeat;
        outcomeLatched = true;
        reactionTimer = 0f;
    }

    public void Tick(TetrisGameSession session, float deltaTime)
    {
        if (session != null)
        {
            float height = Mathf.Max(1, session.Model.VisibleHeight);
            float stack = Mathf.Clamp(session.Model.GetStackHeight(), 0f, height);
            float garbage = Mathf.Clamp(session.Model.CountGarbageRows(), 0f, height);

            Health = 1f - stack / height;
            Recoverable = Mathf.Min(garbage / height, 1f - Health);
        }

        float blend = 1f - Mathf.Exp(-BarCatchUp * deltaTime);
        DisplayedHealth = Mathf.Lerp(DisplayedHealth, Health, blend);
        DisplayedRecoverable = Mathf.Lerp(DisplayedRecoverable, Recoverable, blend);

        if (reactionTimer > 0f)
            reactionTimer = Mathf.Max(0f, reactionTimer - deltaTime);

        if (outcomeLatched)
            return;

        if (reactionTimer > 0f)
        {
            Mood = reactionMood;
            return;
        }

        Mood = Health >= StrainedThreshold
            ? PortraitMood.Ready
            : Health >= CriticalThreshold
                ? PortraitMood.Strained
                : PortraitMood.Critical;
    }
}
