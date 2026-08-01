using UnityEngine;

/// <summary>
/// Lazily resolves the presentation textures loaded from <c>Resources</c>.
/// Views ask this for art instead of calling <see cref="Resources"/> inline,
/// so swapping to addressables or authored sprites is a one-file change.
/// </summary>
public sealed class BattleArtLibrary
{
    private const string StoryBackdropPath = "Story/moon_gate_duel";
    private const string LockedPortraitPath = "Characters/locked_portrait";

    private Texture2D storyBackdrop;
    private Texture2D lockedPortrait;

    public Texture2D StoryBackdrop => Resolve(ref storyBackdrop, StoryBackdropPath);

    public Texture2D LockedPortrait => Resolve(ref lockedPortrait, LockedPortraitPath);

    private static Texture2D Resolve(ref Texture2D cached, string resourcePath)
    {
        if (cached == null)
            cached = Resources.Load<Texture2D>(resourcePath);

        return cached;
    }
}
