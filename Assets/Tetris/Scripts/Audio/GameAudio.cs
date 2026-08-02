using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game's single audio entry point. Clips are synthesized on first request
/// and cached for the session, SFX play through a small voice pool so rapid
/// sounds never cut each other off, and music cross-fades between tracks.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameAudio : MonoBehaviour
{
    private const int VoiceCount = 8;
    private const float MusicFadeSpeed = 2.5f;

    private static GameAudio instance;

    private readonly Dictionary<GameSfx, AudioClip> sfxCache = new();
    private readonly Dictionary<GameMusic, AudioClip> musicCache = new();

    private AudioSource[] voices;
    private int nextVoice;

    private AudioSource musicSource;
    private GameMusic? currentMusic;
    private float musicTargetVolume;

    /// <summary>Master switches, surfaced so a settings screen can bind them later.</summary>
    public static float SfxVolume { get; set; } = 0.75f;
    public static float MusicVolume { get; set; } = 0.4f;
    public static bool Muted { get; set; }

    public static GameAudio Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<GameAudio>();
            if (instance == null)
            {
                GameObject host = new GameObject("Game Audio");
                instance = host.AddComponent<GameAudio>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsureSources();
    }

    /// <summary>
    /// Keeps the audio host alive across scene loads, so music carries through
    /// a transition instead of restarting. Only applied to a root object — the
    /// host <see cref="Instance"/> builds for itself always is one, but a
    /// scene-authored component parented under something else must not drag its
    /// whole hierarchy into the next scene.
    /// </summary>
    public void MarkPersistent()
    {
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (musicSource == null)
            return;

        float target = Muted ? 0f : musicTargetVolume * MusicVolume;
        musicSource.volume = Mathf.MoveTowards(
            musicSource.volume, target, MusicFadeSpeed * Time.unscaledDeltaTime);

        if (musicSource.volume <= 0.001f && musicTargetVolume <= 0f && musicSource.isPlaying)
            musicSource.Stop();
    }

    public static void Play(GameSfx sfx, float volumeScale = 1f)
    {
        if (Muted || !Application.isPlaying)
            return;

        Instance.PlayInternal(sfx, volumeScale);
    }

    public static void PlayMusic(GameMusic music)
    {
        if (!Application.isPlaying)
            return;

        Instance.PlayMusicInternal(music);
    }

    public static void StopMusic()
    {
        if (!Application.isPlaying || instance == null)
            return;

        instance.musicTargetVolume = 0f;
        instance.currentMusic = null;
    }

    private void PlayInternal(GameSfx sfx, float volumeScale)
    {
        EnsureSources();

        if (!sfxCache.TryGetValue(sfx, out AudioClip clip))
        {
            clip = SfxLibrary.Build(sfx);
            sfxCache[sfx] = clip;
        }

        if (clip == null)
            return;

        AudioSource voice = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        voice.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * volumeScale));
    }

    private void PlayMusicInternal(GameMusic music)
    {
        EnsureSources();

        if (currentMusic == music && musicSource.isPlaying)
        {
            musicTargetVolume = 1f;
            return;
        }

        if (!musicCache.TryGetValue(music, out AudioClip clip))
        {
            clip = MusicLibrary.Build(music);
            musicCache[music] = clip;
        }

        if (clip == null)
            return;

        currentMusic = music;
        musicTargetVolume = 1f;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.Play();
    }

    private void EnsureSources()
    {
        if (voices != null && musicSource != null)
            return;

        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voices[i] = source;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = 0f;
    }
}
