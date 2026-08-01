using UnityEngine;

/// <summary>
/// Draws a single roster portrait. Shared by the battle HUD and the
/// character-select cards so both stay visually consistent.
///
/// When a character has expression art for the requested mood it is drawn
/// directly; until that art exists the base portrait is treated — tinted,
/// shaken, and punched — so damage states still read on screen.
/// </summary>
public static class CharacterPortraitView
{
    private static readonly Rect LyraCoords = new Rect(0f, 0f, 0.41f, 1f);
    private static readonly Rect BramCoords = new Rect(0.59f, 0f, 0.41f, 1f);

    public static void Draw(int characterIndex, Rect rect, BattleArtLibrary art)
    {
        Draw(characterIndex, rect, art, PortraitMood.Ready, 0f);
    }

    public static void Draw(
        int characterIndex,
        Rect rect,
        BattleArtLibrary art,
        PortraitMood mood,
        float reactionStrength)
    {
        BattleCharacterDefinition character = BattleCharacterRoster.Get(characterIndex);

        // A hit knocks the portrait around; a cast lifts and swells it.
        Rect drawRect = rect;
        if (reactionStrength > 0f)
        {
            if (mood == PortraitMood.Hurt)
            {
                float shake = reactionStrength * 3f;
                drawRect.x += Mathf.Sin(Time.unscaledTime * 60f) * shake;
                drawRect.y += Mathf.Cos(Time.unscaledTime * 47f) * shake * 0.6f;
            }
            else if (mood == PortraitMood.Casting)
            {
                float swell = reactionStrength * 4f;
                drawRect = new Rect(
                    rect.x - swell * 0.5f,
                    rect.y - swell,
                    rect.width + swell,
                    rect.height + swell);
            }
        }

        Color previous = GUI.color;
        GUI.color = MoodTint(mood, reactionStrength);
        DrawArt(character, characterIndex, drawRect, art, mood);
        GUI.color = previous;

        DrawMoodOverlay(rect, mood, reactionStrength);
    }

    private static void DrawArt(
        BattleCharacterDefinition character,
        int characterIndex,
        Rect rect,
        BattleArtLibrary art,
        PortraitMood mood)
    {
        Texture2D expression = ResolveExpression(character.Id, art, mood);
        if (expression != null)
        {
            GUI.DrawTexture(rect, expression, ScaleMode.ScaleAndCrop);
            return;
        }

        switch (character.Portrait)
        {
            case BattleCharacterPortrait.Lyra when art.StoryBackdrop != null:
                GUI.DrawTextureWithTexCoords(rect, art.StoryBackdrop, LyraCoords, true);
                break;
            case BattleCharacterPortrait.Bram when art.StoryBackdrop != null:
                GUI.DrawTextureWithTexCoords(rect, art.StoryBackdrop, BramCoords, true);
                break;
            case BattleCharacterPortrait.Locked when art.LockedPortrait != null:
                GUI.DrawTexture(rect, art.LockedPortrait, ScaleMode.ScaleAndCrop);
                break;
            default:
                DrawSilhouette(rect);
                break;
        }
    }

    /// <summary>Walks the fallback chain so a partial art set still resolves.</summary>
    private static Texture2D ResolveExpression(
        string characterId,
        BattleArtLibrary art,
        PortraitMood mood)
    {
        PortraitMood current = mood;
        for (int step = 0; step < 4; step++)
        {
            Texture2D texture = art.Expression(characterId, current);
            if (texture != null)
                return texture;

            PortraitMood next = BattleArtLibrary.Fallback(current);
            if (next == current)
                break;

            current = next;
        }

        return null;
    }

    private static Color MoodTint(PortraitMood mood, float reactionStrength)
    {
        switch (mood)
        {
            case PortraitMood.Hurt:
                return Color.Lerp(Color.white, new Color(1f, 0.42f, 0.42f), reactionStrength);
            case PortraitMood.Casting:
                return Color.Lerp(Color.white, new Color(1f, 0.96f, 0.72f), reactionStrength);
            case PortraitMood.Strained:
                return new Color(0.94f, 0.88f, 0.86f);
            case PortraitMood.Critical:
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                return Color.Lerp(new Color(0.88f, 0.72f, 0.72f), new Color(1f, 0.6f, 0.6f), pulse);
            }

            case PortraitMood.Defeat:
                return new Color(0.42f, 0.42f, 0.5f);
            case PortraitMood.Victory:
                return new Color(1f, 0.98f, 0.85f);
            default:
                return Color.white;
        }
    }

    /// <summary>Scrims and rim flashes that sell the mood over the flat art.</summary>
    private static void DrawMoodOverlay(Rect rect, PortraitMood mood, float reactionStrength)
    {
        switch (mood)
        {
            case PortraitMood.Hurt when reactionStrength > 0f:
                RetroGui.Fill(rect, new Color(1f, 0.15f, 0.25f, 0.34f * reactionStrength));
                break;
            case PortraitMood.Casting when reactionStrength > 0f:
                RetroGui.Fill(rect, new Color(1f, 0.9f, 0.45f, 0.3f * reactionStrength));
                break;
            case PortraitMood.Critical:
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                RetroGui.Border(rect, new Color(1f, 0.2f, 0.3f, 0.35f + 0.35f * pulse), 3f);
                break;
            }

            case PortraitMood.Defeat:
                RetroGui.Fill(rect, new Color(0.02f, 0.02f, 0.06f, 0.45f));
                break;
        }
    }

    private static void DrawSilhouette(Rect rect)
    {
        RetroGui.Fill(rect, RetroPalette.PortraitFallback);
        RetroGui.Fill(
            new Rect(
                rect.x + rect.width * 0.33f,
                rect.y + rect.height * 0.2f,
                rect.width * 0.34f,
                rect.width * 0.38f),
            Color.black);
        RetroGui.Fill(
            new Rect(
                rect.x + rect.width * 0.18f,
                rect.y + rect.height * 0.52f,
                rect.width * 0.64f,
                rect.height * 0.42f),
            Color.black);
    }
}
