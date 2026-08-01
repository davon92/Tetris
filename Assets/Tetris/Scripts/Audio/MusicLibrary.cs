using UnityEngine;

public enum GameMusic
{
    Menu,
    Battle,
    Story
}

/// <summary>
/// Sequences the game's looping music. The palette is deliberately not
/// chiptune: plucked detuned strings over slow pads, modal (Aeolian/Dorian)
/// harmony with min7 / add9 / sus voicings, and a wrap-around echo so the
/// reverb tail survives the loop point. The aim is melancholy world-JRPG —
/// Xenogears-adjacent mood rather than arcade bleeps.
///
/// Tracks are rendered to an exact bar count so looping is sample-accurate.
/// </summary>
public static class MusicLibrary
{
    public static AudioClip Build(GameMusic music)
    {
        switch (music)
        {
            case GameMusic.Menu:
                return BuildMenu();
            case GameMusic.Story:
                return BuildStory();
            default:
                return BuildBattle();
        }
    }

    /// <summary>
    /// A chord progression expressed as scale-degree stacks over a root, so
    /// arpeggios and pads always agree on the harmony.
    /// </summary>
    private readonly struct Chord
    {
        public Chord(int root, params int[] tones)
        {
            Root = root;
            Tones = tones;
        }

        public int Root { get; }
        public int[] Tones { get; }
    }

    /// <summary>Title screen: wistful, unhurried, A-minor with a lifted 9th.</summary>
    private static AudioClip BuildMenu()
    {
        // 66 BPM. Four bars of 4 beats.
        const float beat = 60f / 66f;
        Chord[] progression =
        {
            new Chord(57, 0, 3, 7, 14),   // Am add9
            new Chord(53, 0, 4, 7, 11),   // Fmaj7
            new Chord(60, 0, 4, 7, 11),   // Cmaj7
            new Chord(55, 0, 3, 7, 10)    // Gm7 — the modal turn back to Am
        };

        float[] buffer = ChiptuneSynth.Buffer(progression.Length * 4f * beat);

        for (int bar = 0; bar < progression.Length; bar++)
        {
            Chord chord = progression[bar];
            float barStart = bar * 4f * beat;

            // Sustained string bed plus a low root.
            foreach (int tone in chord.Tones)
            {
                ChiptuneSynth.AddPad(
                    buffer, barStart, 4f * beat,
                    ChiptuneSynth.Note(chord.Root + tone), 0.055f);
            }

            ChiptuneSynth.AddPad(
                buffer, barStart, 4f * beat,
                ChiptuneSynth.Note(chord.Root - 12), 0.11f, 5f);

            // Rolling eighth-note arpeggio — the signature texture.
            int[] shape = { 0, 1, 2, 3, 2, 1, 2, 3 };
            for (int step = 0; step < shape.Length; step++)
            {
                int tone = chord.Tones[shape[step] % chord.Tones.Length];
                ChiptuneSynth.AddPluck(
                    buffer,
                    barStart + step * beat * 0.5f,
                    beat * 1.6f,
                    ChiptuneSynth.Note(chord.Root + tone + 12),
                    0.16f);
            }
        }

        // A sparse melody floating on top of the last two bars.
        int[] melody = { 76, 74, 72, 69, 71, 72, 69 };
        float[] melodyTimes = { 0f, 1.5f, 2.5f, 4f, 6f, 7f, 9f };
        for (int i = 0; i < melody.Length; i++)
        {
            ChiptuneSynth.AddPluck(
                buffer, melodyTimes[i] * beat, beat * 2.4f,
                ChiptuneSynth.Note(melody[i]), 0.17f, 5f, 2.1f);
        }

        ChiptuneSynth.AddCircularEcho(buffer, beat * 0.75f, 0.34f);
        ChiptuneSynth.Normalize(buffer, 0.58f);
        return ChiptuneSynth.CreateClip("music_menu", buffer);
    }

    /// <summary>
    /// Battle: same acoustic palette, but urgent. Driving low ostinato in
    /// D Dorian with a pushing arpeggio and a hand-drum pulse.
    /// </summary>
    private static AudioClip BuildBattle()
    {
        const float beat = 60f / 132f;

        // Eight bars rather than four: a four-bar loop turns over every ~7s and
        // a battle can run for minutes, so the second half re-harmonises before
        // resolving to keep the repeat from wearing.
        Chord[] progression =
        {
            new Chord(50, 0, 3, 7, 10),   // Dm7
            new Chord(50, 0, 3, 7, 10),
            new Chord(58, 0, 4, 7, 11),   // Bbmaj7
            new Chord(57, 0, 4, 7, 10),   // A7 — dominant tension back to Dm
            new Chord(50, 0, 3, 7, 10),   // Dm7
            new Chord(55, 0, 3, 7, 10),   // Gm7
            new Chord(48, 0, 4, 7, 10),   // C7
            new Chord(57, 0, 4, 7, 10)    // A7
        };

        float[] buffer = ChiptuneSynth.Buffer(progression.Length * 4f * beat);

        for (int bar = 0; bar < progression.Length; bar++)
        {
            Chord chord = progression[bar];
            float barStart = bar * 4f * beat;

            foreach (int tone in chord.Tones)
            {
                ChiptuneSynth.AddPad(
                    buffer, barStart, 4f * beat,
                    ChiptuneSynth.Note(chord.Root + tone), 0.045f);
            }

            // Insistent low ostinato: root on every beat, octave on the "and".
            for (int b = 0; b < 4; b++)
            {
                ChiptuneSynth.AddPluck(
                    buffer, barStart + b * beat, beat * 0.9f,
                    ChiptuneSynth.Note(chord.Root - 12), 0.2f, 4f, 5.5f);
                ChiptuneSynth.AddPluck(
                    buffer, barStart + b * beat + beat * 0.5f, beat * 0.5f,
                    ChiptuneSynth.Note(chord.Root), 0.1f, 4f, 7f);
            }

            // Sixteenth-ish arpeggio climbing the chord.
            int[] shape = { 0, 2, 3, 2, 1, 2, 3, 2 };
            for (int step = 0; step < shape.Length; step++)
            {
                int tone = chord.Tones[shape[step] % chord.Tones.Length];
                ChiptuneSynth.AddPluck(
                    buffer, barStart + step * beat * 0.5f, beat * 0.7f,
                    ChiptuneSynth.Note(chord.Root + tone + 12), 0.13f, 8f, 4.5f);
            }

            // Frame drum: soft skin on 1 and 3, tighter accent on the offbeats.
            for (int b = 0; b < 4; b++)
            {
                ChiptuneSynth.AddTone(
                    buffer, barStart + b * beat, 0.09f, 160f, 70f,
                    ChiptuneSynth.Wave.Triangle, b % 2 == 0 ? 0.24f : 0.12f,
                    ChiptuneSynth.Envelope.Percussive);
                ChiptuneSynth.AddTone(
                    buffer, barStart + b * beat + beat * 0.5f, 0.05f, 0f, 0f,
                    ChiptuneSynth.Wave.Noise, 0.05f, ChiptuneSynth.Envelope.Percussive);
            }
        }

        // Modal lead over the second half — the raised 6th (B natural over Dm)
        // is the Dorian colour that keeps this from sounding plainly minor.
        int[] melody = { 69, 72, 74, 77, 76, 74, 72, 69, 71, 74, 72, 69 };
        float[] times =
        {
            16f, 16.75f, 17.5f, 18.5f, 19.5f, 20.5f,
            21.25f, 22f, 24f, 25.5f, 27f, 29f
        };

        for (int i = 0; i < melody.Length; i++)
        {
            ChiptuneSynth.AddPluck(
                buffer, times[i] * beat, beat * 1.4f,
                ChiptuneSynth.Note(melody[i]), 0.15f, 6f, 2.6f);
        }

        ChiptuneSynth.AddCircularEcho(buffer, beat * 0.5f, 0.26f);
        ChiptuneSynth.Normalize(buffer, 0.6f);
        return ChiptuneSynth.CreateClip("music_battle", buffer);
    }

    /// <summary>Story beats: very sparse, just pad and a lonely plucked line.</summary>
    private static AudioClip BuildStory()
    {
        const float beat = 60f / 58f;
        Chord[] progression =
        {
            new Chord(53, 0, 4, 7, 11),   // Fmaj7
            new Chord(55, 0, 3, 7, 10),   // Gm7
            new Chord(48, 0, 4, 7, 11),   // Cmaj7
            new Chord(53, 0, 4, 7, 14)    // Fadd9
        };

        float[] buffer = ChiptuneSynth.Buffer(progression.Length * 4f * beat);

        for (int bar = 0; bar < progression.Length; bar++)
        {
            Chord chord = progression[bar];
            float barStart = bar * 4f * beat;

            foreach (int tone in chord.Tones)
            {
                ChiptuneSynth.AddPad(
                    buffer, barStart, 4f * beat,
                    ChiptuneSynth.Note(chord.Root + tone), 0.06f, 13f);
            }

            ChiptuneSynth.AddPad(
                buffer, barStart, 4f * beat,
                ChiptuneSynth.Note(chord.Root - 12), 0.1f, 4f);

            // Two plucks a bar — space is the point here.
            ChiptuneSynth.AddPluck(
                buffer, barStart, beat * 2.6f,
                ChiptuneSynth.Note(chord.Root + chord.Tones[2] + 12), 0.14f, 6f, 1.9f);
            ChiptuneSynth.AddPluck(
                buffer, barStart + beat * 2.5f, beat * 2.2f,
                ChiptuneSynth.Note(chord.Root + chord.Tones[1] + 12), 0.11f, 6f, 2.1f);
        }

        ChiptuneSynth.AddCircularEcho(buffer, beat * 1.5f, 0.4f, 2);
        ChiptuneSynth.Normalize(buffer, 0.5f);
        return ChiptuneSynth.CreateClip("music_story", buffer);
    }
}
