using UnityEngine;
using Wave = ChiptuneSynth.Wave;
using Envelope = ChiptuneSynth.Envelope;

public enum GameSfx
{
    MenuMove,
    MenuConfirm,
    MenuBack,
    PieceMove,
    PieceRotate,
    PieceLock,
    HardDrop,
    Hold,
    LineClear,
    Tetris,
    GarbageLand,
    GarbageBlocked,
    SpellCast,
    SpellHit,
    Heal,
    LevelUp,
    Ready,
    Start,
    Win,
    Lose
}

/// <summary>
/// Builds every sound effect procedurally. Each entry is a tiny composition —
/// a couple of gliding tones and a noise burst — tuned to read clearly over the
/// music without stepping on the next sound.
/// </summary>
public static class SfxLibrary
{
    public static AudioClip Build(GameSfx sfx)
    {
        switch (sfx)
        {
            case GameSfx.MenuMove:
                return Simple(0.06f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.05f, 880f, 1170f, Wave.Pulse, 0.35f, Envelope.Blip));

            case GameSfx.MenuConfirm:
                return Simple(0.24f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.07f, ChiptuneSynth.Note(76), Wave.Square, 0.4f, Envelope.Blip);
                    ChiptuneSynth.AddTone(b, 0.06f, 0.16f, ChiptuneSynth.Note(83), Wave.Square, 0.4f, Envelope.Short);
                });

            case GameSfx.MenuBack:
                return Simple(0.16f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.14f, 600f, 300f, Wave.Pulse, 0.35f, Envelope.Short));

            case GameSfx.PieceMove:
                // Fires on every tap, so it must be nearly subliminal.
                return Simple(0.035f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.03f, 320f, 300f, Wave.Pulse, 0.16f, Envelope.Blip));

            case GameSfx.PieceRotate:
                return Simple(0.06f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.05f, 520f, 720f, Wave.Pulse, 0.2f, Envelope.Blip));

            case GameSfx.PieceLock:
                return Simple(0.1f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.06f, 190f, 120f, Wave.Square, 0.3f, Envelope.Percussive);
                    ChiptuneSynth.AddTone(b, 0f, 0.04f, 0f, 0f, Wave.Noise, 0.12f, Envelope.Percussive);
                });

            case GameSfx.HardDrop:
                return Simple(0.18f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.09f, 900f, 130f, Wave.Saw, 0.3f, Envelope.Percussive);
                    ChiptuneSynth.AddTone(b, 0.07f, 0.09f, 0f, 0f, Wave.Noise, 0.22f, Envelope.Percussive);
                });

            case GameSfx.Hold:
                return Simple(0.14f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.12f, 440f, 660f, Wave.Triangle, 0.3f, Envelope.Short));

            case GameSfx.LineClear:
                return Simple(0.4f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.1f, 0f, 0f, Wave.Noise, 0.2f, Envelope.Percussive);
                    for (int i = 0; i < 3; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, i * 0.06f, 0.14f,
                            ChiptuneSynth.Note(72 + i * 4), Wave.Square, 0.3f, Envelope.Short);
                    }
                });

            case GameSfx.Tetris:
                // The reward sound: a rising arpeggio with a bright fifth on top.
                return Simple(0.85f, b =>
                {
                    int[] notes = { 72, 76, 79, 84, 88 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, i * 0.07f, 0.3f,
                            ChiptuneSynth.Note(notes[i]), Wave.Square, 0.32f, Envelope.Pad);
                        ChiptuneSynth.AddTone(
                            b, i * 0.07f, 0.3f,
                            ChiptuneSynth.Note(notes[i] + 7), Wave.Pulse, 0.14f, Envelope.Pad);
                    }

                    ChiptuneSynth.AddTone(b, 0.35f, 0.45f, ChiptuneSynth.Note(91), Wave.Square, 0.3f, Envelope.Pad);
                    ChiptuneSynth.AddTone(b, 0f, 0.14f, 0f, 0f, Wave.Noise, 0.16f, Envelope.Percussive);
                });

            case GameSfx.GarbageLand:
                return Simple(0.4f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.28f, 150f, 60f, Wave.Square, 0.34f, Envelope.Short);
                    ChiptuneSynth.AddTone(b, 0f, 0.3f, 0f, 0f, Wave.Noise, 0.26f, Envelope.Short);
                });

            case GameSfx.GarbageBlocked:
                return Simple(0.32f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.16f, 500f, 900f, Wave.Triangle, 0.32f, Envelope.Short);
                    ChiptuneSynth.AddTone(b, 0.1f, 0.2f, 900f, 1200f, Wave.Pulse, 0.2f, Envelope.Short);
                });

            case GameSfx.SpellCast:
                return Simple(0.75f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.5f, 220f, 1400f, Wave.Saw, 0.26f, Envelope.Pad, 18f, 25f);
                    for (int i = 0; i < 4; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, 0.28f + i * 0.06f, 0.28f,
                            ChiptuneSynth.Note(79 + i * 5), Wave.Square, 0.24f, Envelope.Short);
                    }
                });

            case GameSfx.SpellHit:
                return Simple(0.6f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.4f, 0f, 0f, Wave.Noise, 0.34f, Envelope.Short);
                    ChiptuneSynth.AddTone(b, 0f, 0.3f, 420f, 70f, Wave.Square, 0.3f, Envelope.Short);
                    ChiptuneSynth.AddTone(b, 0.05f, 0.2f, 180f, 50f, Wave.Triangle, 0.26f, Envelope.Short);
                });

            case GameSfx.Heal:
                return Simple(0.7f, b =>
                {
                    int[] notes = { 72, 77, 81, 84 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, i * 0.09f, 0.35f,
                            ChiptuneSynth.Note(notes[i]), Wave.Triangle, 0.3f, Envelope.Pad, 6f, 4f);
                    }
                });

            case GameSfx.LevelUp:
                return Simple(0.5f, b =>
                {
                    int[] notes = { 74, 79, 86 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, i * 0.08f, 0.25f,
                            ChiptuneSynth.Note(notes[i]), Wave.Square, 0.3f, Envelope.Short);
                    }
                });

            case GameSfx.Ready:
                return Simple(0.45f, b =>
                    ChiptuneSynth.AddTone(b, 0f, 0.4f, ChiptuneSynth.Note(69), Wave.Square, 0.34f, Envelope.Pad, 7f, 6f));

            case GameSfx.Start:
                return Simple(0.6f, b =>
                {
                    ChiptuneSynth.AddTone(b, 0f, 0.5f, ChiptuneSynth.Note(81), Wave.Square, 0.36f, Envelope.Pad);
                    ChiptuneSynth.AddTone(b, 0f, 0.5f, ChiptuneSynth.Note(88), Wave.Pulse, 0.2f, Envelope.Pad);
                    ChiptuneSynth.AddTone(b, 0f, 0.12f, 0f, 0f, Wave.Noise, 0.2f, Envelope.Percussive);
                });

            case GameSfx.Win:
                return Simple(1.5f, b =>
                {
                    int[] notes = { 72, 76, 79, 84, 79, 84, 88 };
                    float[] times = { 0f, 0.12f, 0.24f, 0.36f, 0.56f, 0.68f, 0.82f };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, times[i], i == notes.Length - 1 ? 0.6f : 0.2f,
                            ChiptuneSynth.Note(notes[i]), Wave.Square, 0.32f, Envelope.Pad);
                        ChiptuneSynth.AddTone(
                            b, times[i], i == notes.Length - 1 ? 0.6f : 0.2f,
                            ChiptuneSynth.Note(notes[i] - 12), Wave.Triangle, 0.22f, Envelope.Pad);
                    }
                });

            default:
                return Simple(1.4f, b =>
                {
                    int[] notes = { 72, 68, 65, 60 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        ChiptuneSynth.AddTone(
                            b, i * 0.18f, i == notes.Length - 1 ? 0.7f : 0.24f,
                            ChiptuneSynth.Note(notes[i]), Wave.Square, 0.3f, Envelope.Pad, 5f, 5f);
                    }
                });
        }
    }

    private static AudioClip Simple(float seconds, System.Action<float[]> compose)
    {
        float[] buffer = ChiptuneSynth.Buffer(seconds);
        compose(buffer);
        ChiptuneSynth.Normalize(buffer, 0.85f);
        return ChiptuneSynth.CreateClip("sfx", buffer);
    }
}
