using System.Collections.Generic;
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

    private readonly Dictionary<string, Texture2D> expressions = new();

    private Texture2D storyBackdrop;
    private Texture2D lockedPortrait;

    public Texture2D StoryBackdrop => Resolve(ref storyBackdrop, StoryBackdropPath);

    public Texture2D LockedPortrait => Resolve(ref lockedPortrait, LockedPortraitPath);

    /// <summary>
    /// Expression art for a character, or null when that mood has no sprite yet
    /// and the caller should fall back to treating the base portrait.
    /// Looked for at <c>Resources/Characters/{id}_{mood}</c>, e.g.
    /// <c>Characters/lyra_hurt</c>. Missing files are cached as null so a
    /// lookup miss costs one Resources.Load for the lifetime of the battle.
    /// </summary>
    public Texture2D Expression(string characterId, PortraitMood mood)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        string key = $"Characters/{characterId}_{MoodSuffix(mood)}";
        if (expressions.TryGetValue(key, out Texture2D cached))
            return cached;

        Texture2D loaded = Resources.Load<Texture2D>(key);
        expressions[key] = loaded;
        return loaded;
    }

    /// <summary>
    /// Reaction moods collapse onto the sustained art when a dedicated sprite
    /// is missing, so a partial art set still reads correctly.
    /// </summary>
    public static PortraitMood Fallback(PortraitMood mood)
    {
        return mood switch
        {
            PortraitMood.Hurt => PortraitMood.Strained,
            PortraitMood.Casting => PortraitMood.Ready,
            PortraitMood.Victory => PortraitMood.Ready,
            PortraitMood.Defeat => PortraitMood.Critical,
            PortraitMood.Critical => PortraitMood.Strained,
            PortraitMood.Strained => PortraitMood.Ready,
            _ => PortraitMood.Ready
        };
    }

    private static string MoodSuffix(PortraitMood mood)
    {
        return mood switch
        {
            PortraitMood.Ready => "ready",
            PortraitMood.Strained => "strained",
            PortraitMood.Critical => "critical",
            PortraitMood.Casting => "casting",
            PortraitMood.Hurt => "hurt",
            PortraitMood.Victory => "victory",
            _ => "defeat"
        };
    }

    private static Texture2D Resolve(ref Texture2D cached, string resourcePath)
    {
        if (cached == null)
            cached = Resources.Load<Texture2D>(resourcePath);

        return cached;
    }
}
