using UnityEngine;

/// <summary>
/// One definition of what mana looks like, shared by everything that draws it:
/// the block on the board, the motes a clear sends flying, and the charged mana
/// bar. The connection the player has to make — <em>that block feeds this
/// bar</em> — only lands if all three run the same rainbow.
/// </summary>
public static class ManaVisuals
{
    /// <summary>Hue cycles per second. Slow enough to read as a colour, fast enough to move.</summary>
    private const float HueScroll = 0.45f;

    /// <summary>Held short of full so the rainbow stays a light source, not a poster print.</summary>
    private const float Saturation = 0.72f;

    /// <summary>Radians per second of the white twinkle laid over the hue.</summary>
    private const float ShimmerSpeed = 9f;

    /// <summary>The scrolling rainbow at a phase offset, 0..1 for one full turn.</summary>
    public static Color Hue(float phase)
    {
        return Color.HSVToRGB(
            Mathf.Repeat(Time.unscaledTime * HueScroll + phase, 1f), Saturation, 1f);
    }

    /// <summary>
    /// A mana block or mote: the rainbow plus a twinkle toward white. Tetromino
    /// colours never move, so the twinkle is what keeps a mana cell unmistakable
    /// in the frames where its hue happens to land on a normal piece's colour.
    /// </summary>
    public static Color Cell(float phase, float alpha)
    {
        float shimmer = 0.5f + 0.5f * Mathf.Sin(
            Time.unscaledTime * ShimmerSpeed + phase * Mathf.PI * 2f);
        Color color = Color.Lerp(Hue(phase), Color.white, 0.3f * shimmer);
        color.a = alpha;
        return color;
    }
}
