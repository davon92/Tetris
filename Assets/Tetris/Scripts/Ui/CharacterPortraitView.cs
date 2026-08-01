using UnityEngine;

/// <summary>
/// Draws a single roster portrait. Shared by the battle HUD and the
/// character-select cards so both stay visually consistent.
/// </summary>
public static class CharacterPortraitView
{
    private static readonly Rect LyraCoords = new Rect(0f, 0f, 0.41f, 1f);
    private static readonly Rect BramCoords = new Rect(0.59f, 0f, 0.41f, 1f);

    public static void Draw(int characterIndex, Rect rect, BattleArtLibrary art)
    {
        BattleCharacterDefinition character = BattleCharacterRoster.Get(characterIndex);
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
