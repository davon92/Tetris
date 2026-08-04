using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The motes a clear throws off — one per block it removed — that fly into that
/// player's mana bar. The whole point is causal legibility: the charge does not
/// simply appear, it arrives, and the player can see which blocks paid for it.
///
/// Positions are held in normalised board space (0..1 across the board, 0..1 up
/// from its floor) so nothing here knows HUD geometry; <see cref="BattleHudView"/>
/// maps them onto whatever rects the board and the bar occupy this frame.
/// </summary>
public sealed class ManaMoteField
{
    /// <summary>A tetris throws 40 motes. The cap is headroom over that, not a budget.</summary>
    private const int MaxMotes = 48;

    private const float FlightDuration = 0.44f;
    private const float ColumnStagger = 0.012f;
    private const float RowStagger = 0.02f;
    private const float AbsorbDuration = 0.22f;

    public struct Mote
    {
        /// <summary>Spawn point, normalised across the board and up from its floor.</summary>
        public float U;
        public float V;

        /// <summary>Colour of the block this came from. Ignored while <see cref="IsMana"/>.</summary>
        public Color Color;

        /// <summary>A mana cell's mote, which runs the rainbow instead of a flat colour.</summary>
        public bool IsMana;

        /// <summary>Rainbow offset, and the seed of this mote's arc and size.</summary>
        public float Phase;

        /// <summary>Bezier control offset from the midpoint, in canvas pixels.</summary>
        public float ArcX;
        public float ArcY;

        public float Size;
        public float Age;
        public float Delay;
        public float Duration;

        /// <summary>0..1 along the flight, or negative while still waiting to leave.</summary>
        public float Progress => Age < Delay ? -1f : Mathf.Clamp01((Age - Delay) / Duration);
    }

    private readonly List<Mote> motes = new();
    private readonly System.Random random = new(90210);

    private float absorbTimer;

    public int Count => motes.Count;

    public Mote this[int index] => motes[index];

    /// <summary>1 the instant a mote struck the bar, decaying to 0. Flashes the fill edge.</summary>
    public float AbsorbStrength => absorbTimer <= 0f ? 0f : absorbTimer / AbsorbDuration;

    public void Reset()
    {
        motes.Clear();
        absorbTimer = 0f;
    }

    /// <summary>
    /// Launches one mote per block the clear just removed. Reads the board's
    /// record of that clear, so it has to run from the clear event, before the
    /// next placement overwrites it.
    /// </summary>
    public void Spawn(TetrisBoardModel model)
    {
        if (model == null)
            return;

        IReadOnlyList<int> rows = model.LastClearedRows;
        IReadOnlyList<int> values = model.LastClearedCells;

        for (int row = 0; row < rows.Count; row++)
        {
            // A line completed above the visible board has no on-screen block to
            // fly from, so it pays charge without a mote.
            if (rows[row] >= model.VisibleHeight)
                continue;

            float v = (rows[row] + 0.5f) / model.VisibleHeight;
            for (int x = 0; x < model.Width; x++)
            {
                if (motes.Count >= MaxMotes)
                    return;

                int index = row * model.Width + x;
                if (index >= values.Count || values[index] == 0)
                    continue;

                motes.Add(BuildMote(values[index], (x + 0.5f) / model.Width, v, x, row));
            }
        }
    }

    public void Tick(float deltaTime)
    {
        absorbTimer = Mathf.Max(0f, absorbTimer - deltaTime);

        for (int i = motes.Count - 1; i >= 0; i--)
        {
            Mote mote = motes[i];
            mote.Age += deltaTime;
            if (mote.Age >= mote.Delay + mote.Duration)
            {
                motes.RemoveAt(i);
                absorbTimer = AbsorbDuration;
                continue;
            }

            motes[i] = mote;
        }
    }

    private Mote BuildMote(int cellValue, float u, float v, int column, int row)
    {
        bool isMana = cellValue == TetrisBoardModel.ManaCell;
        return new Mote
        {
            U = u,
            V = v,
            Color = Brighten(TetrominoDefinitions.GetColor(cellValue), 0.3f),
            IsMana = isMana,
            Phase = Range(0f, 1f),

            // The arc bulges up and away from the board before the pull takes
            // over, which is what sells the mote as being drawn to the bar
            // rather than merely sliding toward it.
            ArcX = Range(-12f, 12f),
            ArcY = Range(-46f, -18f),

            // Mana motes fly bigger: they are worth more, and they are the ones
            // the whole effect exists to make legible.
            Size = isMana ? Range(5f, 6.5f) : Range(3f, 4.5f),
            Delay = column * ColumnStagger + row * RowStagger,
            Duration = FlightDuration + Range(0f, 0.12f)
        };
    }

    private float Range(float minimum, float maximum)
    {
        return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }

    private static Color Brighten(Color color, float amount)
    {
        return new Color(
            Mathf.Lerp(color.r, 1f, amount),
            Mathf.Lerp(color.g, 1f, amount),
            Mathf.Lerp(color.b, 1f, amount),
            1f);
    }
}
