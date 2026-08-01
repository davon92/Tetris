using System;
using UnityEngine;

/// <summary>
/// Low-level IMGUI drawing primitives shared by every view. Views compose
/// these calls and never touch <see cref="GUI.matrix"/> or raw textures.
/// </summary>
public static class RetroGui
{
    public const float CanvasWidth = 640f;
    public const float CanvasHeight = 480f;

    public static Rect CanvasRect => new Rect(0f, 0f, CanvasWidth, CanvasHeight);

    /// <summary>
    /// Restores the GUI transform when the enclosing <c>using</c> block ends,
    /// so a view can never leak canvas state into the next view.
    /// </summary>
    public readonly struct CanvasScope : IDisposable
    {
        private readonly Matrix4x4 previousMatrix;
        private readonly Color previousColor;

        internal CanvasScope(Matrix4x4 previousMatrix, Color previousColor)
        {
            this.previousMatrix = previousMatrix;
            this.previousColor = previousColor;
        }

        public void Dispose()
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }

    /// <summary>
    /// Maps the 640x480 reference canvas onto the backbuffer using an integer
    /// scale wherever possible, keeping the pixel-art HUD crisp.
    /// </summary>
    public static CanvasScope ReferenceCanvas()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;

        float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
        if (scale >= 1f)
            scale = Mathf.Floor(scale);

        scale = Mathf.Max(0.1f, scale);
        float offsetX = Mathf.Round((Screen.width - CanvasWidth * scale) * 0.5f);
        float offsetY = Mathf.Round((Screen.height - CanvasHeight * scale) * 0.5f);
        GUI.matrix = Matrix4x4.TRS(
            new Vector3(offsetX, offsetY, 0f),
            Quaternion.identity,
            new Vector3(scale, scale, 1f));

        return new CanvasScope(previousMatrix, previousColor);
    }

    public static void Fill(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    public static void Border(Rect rect, Color color, float thickness)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    public static void Panel(Rect rect, Color fill, Color border, float thickness = 1f)
    {
        Fill(rect, fill);
        Border(rect, border, thickness);
    }

    /// <summary>Draws a decorative tetromino with a drop shadow and highlight.</summary>
    public static void Tetromino(
        TetriminoType type,
        Vector2 center,
        float cellSize,
        int rotation,
        float alpha)
    {
        Vector2Int[] cells = TetrominoDefinitions.GetCells(type, rotation);
        Color pieceColor = TetrominoDefinitions.GetColor(type);
        pieceColor.a = alpha;

        for (int i = 0; i < cells.Length; i++)
        {
            float x = Mathf.Round(center.x + cells[i].x * cellSize - cellSize * 0.5f);
            float y = Mathf.Round(center.y - cells[i].y * cellSize - cellSize * 0.5f);
            Rect cellRect = new Rect(x, y, cellSize - 1f, cellSize - 1f);
            Fill(
                new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width, cellRect.height),
                new Color(0f, 0f, 0f, alpha * 0.45f));
            Fill(cellRect, pieceColor);
            Border(cellRect, new Color(1f, 1f, 1f, alpha * 0.45f), 1f);
        }
    }

    public static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
