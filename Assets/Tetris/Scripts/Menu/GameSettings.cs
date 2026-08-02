using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio and graphics preferences, persisted to <see cref="PlayerPrefs"/> and
/// pushed at the engine on <see cref="Apply"/>. Kept separate from the options
/// screen so the settings survive without a menu on screen, and so anything
/// else that wants the volume can read it without touching UI code.
/// </summary>
public static class GameSettings
{
    private const string MusicKey = "settings-music-volume";
    private const string SfxKey = "settings-sfx-volume";
    private const string MutedKey = "settings-muted";
    private const string FullscreenKey = "settings-fullscreen";
    private const string ResolutionKey = "settings-resolution";
    private const string VSyncKey = "settings-vsync";
    private const string QualityKey = "settings-quality";

    private static Vector2Int[] resolutions;
    private static bool loaded;

    public static float MusicVolume { get; private set; } = 0.4f;
    public static float SfxVolume { get; private set; } = 0.75f;
    public static bool Muted { get; private set; }
    public static bool Fullscreen { get; private set; } = true;
    public static bool VSync { get; private set; } = true;
    public static int QualityLevel { get; private set; }
    public static int ResolutionIndex { get; private set; }

    public static string[] QualityNames => QualitySettings.names;

    /// <summary>
    /// Distinct width x height pairs the display supports, ascending. Refresh
    /// rates are collapsed out — a resolution list that shows the same size
    /// four times is a worse menu, and the engine picks the rate anyway.
    /// </summary>
    public static Vector2Int[] Resolutions
    {
        get
        {
            if (resolutions != null)
                return resolutions;

            List<Vector2Int> distinct = new();
            foreach (Resolution mode in Screen.resolutions)
            {
                Vector2Int size = new Vector2Int(mode.width, mode.height);
                if (!distinct.Contains(size))
                    distinct.Add(size);
            }

            // A headless or virtual display can report nothing at all.
            if (distinct.Count == 0)
                distinct.Add(new Vector2Int(Screen.width, Screen.height));

            distinct.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
            resolutions = distinct.ToArray();
            return resolutions;
        }
    }

    public static string ResolutionLabel
    {
        get
        {
            Vector2Int size = Resolutions[Mathf.Clamp(ResolutionIndex, 0, Resolutions.Length - 1)];
            return $"{size.x} x {size.y}";
        }
    }

    public static string QualityLabel
    {
        get
        {
            string[] names = QualityNames;
            return names.Length == 0
                ? "DEFAULT"
                : names[Mathf.Clamp(QualityLevel, 0, names.Length - 1)].ToUpperInvariant();
        }
    }

    public static void Load()
    {
        if (loaded)
            return;

        loaded = true;
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, MusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, SfxVolume);
        Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        VSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        QualityLevel = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        ResolutionIndex = PlayerPrefs.GetInt(ResolutionKey, CurrentResolutionIndex());
        Apply();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
        PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(VSyncKey, VSync ? 1 : 0);
        PlayerPrefs.SetInt(QualityKey, QualityLevel);
        PlayerPrefs.SetInt(ResolutionKey, ResolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>Pushes every setting at the engine and the audio system.</summary>
    public static void Apply()
    {
        GameAudio.MusicVolume = MusicVolume;
        GameAudio.SfxVolume = SfxVolume;
        GameAudio.Muted = Muted;

        QualitySettings.vSyncCount = VSync ? 1 : 0;

        string[] names = QualityNames;
        if (names.Length > 0)
        {
            QualityLevel = Mathf.Clamp(QualityLevel, 0, names.Length - 1);
            if (QualitySettings.GetQualityLevel() != QualityLevel)
                QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        ApplyDisplayMode();
    }

    /// <summary>
    /// Resolution changes are a no-op in the editor's Game view, so this is
    /// only worth the call in a player.
    /// </summary>
    private static void ApplyDisplayMode()
    {
        if (Application.isEditor)
            return;

        Vector2Int size = Resolutions[Mathf.Clamp(ResolutionIndex, 0, Resolutions.Length - 1)];
        if (Screen.width == size.x && Screen.height == size.y && Screen.fullScreen == Fullscreen)
            return;

        Screen.SetResolution(size.x, size.y, Fullscreen);
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f);
        GameAudio.MusicVolume = MusicVolume;
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f);
        GameAudio.SfxVolume = SfxVolume;
    }

    public static void SetMuted(bool value)
    {
        Muted = value;
        GameAudio.Muted = value;
    }

    public static void SetFullscreen(bool value)
    {
        Fullscreen = value;
        ApplyDisplayMode();
    }

    public static void SetVSync(bool value)
    {
        VSync = value;
        QualitySettings.vSyncCount = value ? 1 : 0;
    }

    public static void StepResolution(int delta)
    {
        ResolutionIndex = Mathf.Clamp(ResolutionIndex + delta, 0, Resolutions.Length - 1);
        ApplyDisplayMode();
    }

    public static void StepQuality(int delta)
    {
        string[] names = QualityNames;
        if (names.Length == 0)
            return;

        QualityLevel = Mathf.Clamp(QualityLevel + delta, 0, names.Length - 1);
        QualitySettings.SetQualityLevel(QualityLevel, true);
    }

    private static int CurrentResolutionIndex()
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        Vector2Int[] all = Resolutions;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == current)
                return i;
        }

        return all.Length - 1;
    }
}
