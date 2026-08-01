using System;
using UnityEngine;

/// <summary>
/// Renders chiptune waveforms into <see cref="AudioClip"/>s at runtime. The game
/// ships no audio files: every sound is synthesized on first use and cached, so
/// the retro palette stays consistent and nothing depends on imported assets.
/// </summary>
public static class ChiptuneSynth
{
    public const int SampleRate = 44100;

    public enum Wave
    {
        /// <summary>50% square — the fat lead voice.</summary>
        Square,

        /// <summary>25% pulse — thinner, good for blips.</summary>
        Pulse,

        /// <summary>Soft triangle — bass and mellow tones.</summary>
        Triangle,

        Saw,

        /// <summary>White noise — percussion, impacts, explosions.</summary>
        Noise
    }

    /// <summary>Envelope shape in seconds; sustain is a 0..1 level.</summary>
    public readonly struct Envelope
    {
        public Envelope(float attack, float decay, float sustain, float release)
        {
            Attack = attack;
            Decay = decay;
            Sustain = sustain;
            Release = release;
        }

        public float Attack { get; }
        public float Decay { get; }
        public float Sustain { get; }
        public float Release { get; }

        public static Envelope Blip => new Envelope(0.001f, 0.03f, 0.0f, 0.02f);
        public static Envelope Short => new Envelope(0.002f, 0.06f, 0.35f, 0.06f);
        public static Envelope Pad => new Envelope(0.01f, 0.08f, 0.7f, 0.12f);
        public static Envelope Percussive => new Envelope(0.0005f, 0.05f, 0.0f, 0.03f);
    }

    /// <summary>Midi note number to frequency. 69 = A4 = 440Hz.</summary>
    public static float Note(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    public static AudioClip CreateClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static float[] Buffer(float seconds)
    {
        return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];
    }

    /// <summary>
    /// Adds a tone to <paramref name="buffer"/>. Frequency glides from
    /// <paramref name="startHz"/> to <paramref name="endHz"/>, which is what
    /// gives chiptune its characteristic zaps and drops.
    /// </summary>
    public static void AddTone(
        float[] buffer,
        float startSeconds,
        float duration,
        float startHz,
        float endHz,
        Wave wave,
        float volume,
        Envelope envelope,
        float vibratoHz = 0f,
        float vibratoDepth = 0f)
    {
        int start = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, buffer.Length);
        int count = Mathf.RoundToInt(duration * SampleRate);
        if (count <= 0)
            return;

        // A single running phase keeps a gliding tone continuous — stepping the
        // phase per-sample instead of recomputing sin(t) avoids clicks.
        double phase = 0d;
        var noise = new System.Random(unchecked(start * 397 + count));

        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            if (index >= buffer.Length)
                break;

            float t = (float)i / count;
            float hz = Mathf.Lerp(startHz, endHz, t);
            if (vibratoHz > 0f)
                hz += Mathf.Sin(i / (float)SampleRate * vibratoHz * Mathf.PI * 2f) * vibratoDepth;

            phase += hz / SampleRate;
            if (phase > 1d)
                phase -= 1d;

            float sample = Sample(wave, (float)phase, noise);
            float amplitude = EnvelopeAt(envelope, i / (float)SampleRate, duration);
            buffer[index] += sample * amplitude * volume;
        }
    }

    /// <summary>Convenience overload for a steady (non-gliding) tone.</summary>
    public static void AddTone(
        float[] buffer,
        float startSeconds,
        float duration,
        float hz,
        Wave wave,
        float volume,
        Envelope envelope)
    {
        AddTone(buffer, startSeconds, duration, hz, hz, wave, volume, envelope);
    }

    /// <summary>Steady tone with vibrato — a held note that shimmers.</summary>
    public static void AddTone(
        float[] buffer,
        float startSeconds,
        float duration,
        float hz,
        Wave wave,
        float volume,
        Envelope envelope,
        float vibratoHz,
        float vibratoDepth)
    {
        AddTone(
            buffer, startSeconds, duration, hz, hz, wave, volume, envelope,
            vibratoHz, vibratoDepth);
    }

    private static float Sample(Wave wave, float phase, System.Random noise)
    {
        switch (wave)
        {
            case Wave.Square:
                return phase < 0.5f ? 1f : -1f;
            case Wave.Pulse:
                return phase < 0.25f ? 1f : -1f;
            case Wave.Triangle:
                return 4f * Mathf.Abs(phase - 0.5f) - 1f;
            case Wave.Saw:
                return 1f - 2f * phase;
            default:
                return (float)(noise.NextDouble() * 2d - 1d);
        }
    }

    private static float EnvelopeAt(Envelope envelope, float time, float duration)
    {
        float releaseStart = Mathf.Max(0f, duration - envelope.Release);
        if (time >= releaseStart && envelope.Release > 0f)
        {
            float sustainLevel = envelope.Sustain;
            if (releaseStart <= envelope.Attack + envelope.Decay)
                sustainLevel = Mathf.Max(sustainLevel, 0.35f);

            float t = Mathf.Clamp01((time - releaseStart) / envelope.Release);
            return Mathf.Lerp(sustainLevel, 0f, t);
        }

        if (time < envelope.Attack && envelope.Attack > 0f)
            return time / envelope.Attack;

        float decayTime = time - envelope.Attack;
        if (decayTime < envelope.Decay && envelope.Decay > 0f)
            return Mathf.Lerp(1f, envelope.Sustain, decayTime / envelope.Decay);

        return envelope.Sustain;
    }

    /// <summary>
    /// Plucked-string voice: two slightly detuned triangle oscillators with a
    /// sharp attack and exponential decay. The detune beats against itself and
    /// the exponential tail reads as an acoustic guitar or harp rather than a
    /// square-wave blip — this is the workhorse of the melancholy JRPG palette.
    /// </summary>
    public static void AddPluck(
        float[] buffer,
        float startSeconds,
        float duration,
        float hz,
        float volume,
        float detuneCents = 7f,
        float decayRate = 3.2f)
    {
        int start = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, buffer.Length);
        int count = Mathf.RoundToInt(duration * SampleRate);
        if (count <= 0 || hz <= 0f)
            return;

        double phaseA = 0d;
        double phaseB = 0d;
        float hzB = hz * Mathf.Pow(2f, detuneCents / 1200f);
        const float attack = 0.004f;

        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            if (index >= buffer.Length)
                break;

            float t = i / (float)SampleRate;
            phaseA += hz / SampleRate;
            phaseB += hzB / SampleRate;
            if (phaseA > 1d) phaseA -= 1d;
            if (phaseB > 1d) phaseB -= 1d;

            float a = 4f * Mathf.Abs((float)phaseA - 0.5f) - 1f;
            float b = 4f * Mathf.Abs((float)phaseB - 0.5f) - 1f;

            // A touch of odd harmonic gives the pluck its "string" bite.
            float body = (a + b) * 0.5f + Mathf.Sin((float)phaseA * Mathf.PI * 2f) * 0.25f;

            float envelope = Mathf.Exp(-t * decayRate);
            if (t < attack)
                envelope *= t / attack;

            buffer[index] += body * envelope * volume;
        }
    }

    /// <summary>
    /// Sustained pad: detuned triangle pair with a slow swell and gentle
    /// vibrato. Sits under the melody as strings or breathy choir.
    /// </summary>
    public static void AddPad(
        float[] buffer,
        float startSeconds,
        float duration,
        float hz,
        float volume,
        float detuneCents = 11f)
    {
        int start = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, buffer.Length);
        int count = Mathf.RoundToInt(duration * SampleRate);
        if (count <= 0 || hz <= 0f)
            return;

        double phaseA = 0d;
        double phaseB = 0d;
        float hzB = hz * Mathf.Pow(2f, detuneCents / 1200f);
        float attack = Mathf.Min(0.35f, duration * 0.4f);
        float release = Mathf.Min(0.5f, duration * 0.45f);

        for (int i = 0; i < count; i++)
        {
            int index = start + i;
            if (index >= buffer.Length)
                break;

            float t = i / (float)SampleRate;
            float vibrato = 1f + Mathf.Sin(t * 4.5f * Mathf.PI * 2f) * 0.0025f;
            phaseA += hz * vibrato / SampleRate;
            phaseB += hzB / SampleRate;
            if (phaseA > 1d) phaseA -= 1d;
            if (phaseB > 1d) phaseB -= 1d;

            float a = 4f * Mathf.Abs((float)phaseA - 0.5f) - 1f;
            float b = 4f * Mathf.Abs((float)phaseB - 0.5f) - 1f;

            float envelope = 1f;
            if (t < attack)
                envelope = t / attack;
            float remaining = duration - t;
            if (remaining < release)
                envelope *= Mathf.Max(0f, remaining / release);

            buffer[index] += (a + b) * 0.5f * envelope * volume;
        }
    }

    /// <summary>
    /// Delay taps that wrap around the buffer end, so a looping track keeps its
    /// echo tail across the loop point instead of cutting dead at the seam.
    /// </summary>
    public static void AddCircularEcho(
        float[] buffer,
        float delaySeconds,
        float feedback = 0.42f,
        int taps = 3)
    {
        int delay = Mathf.RoundToInt(delaySeconds * SampleRate);
        if (delay <= 0 || buffer.Length == 0)
            return;

        float[] source = (float[])buffer.Clone();
        float gain = feedback;
        for (int tap = 1; tap <= taps; tap++)
        {
            int offset = delay * tap;
            for (int i = 0; i < buffer.Length; i++)
                buffer[(i + offset) % buffer.Length] += source[i] * gain;

            gain *= feedback;
        }
    }

    /// <summary>Clamps the buffer into range, keeping the loudest peak at ~0.9.</summary>
    public static void Normalize(float[] buffer, float peak = 0.9f)
    {
        float max = 0f;
        for (int i = 0; i < buffer.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(buffer[i]));

        if (max <= Mathf.Epsilon)
            return;

        float scale = peak / max;
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = Mathf.Clamp(buffer[i] * scale, -1f, 1f);
    }
}
