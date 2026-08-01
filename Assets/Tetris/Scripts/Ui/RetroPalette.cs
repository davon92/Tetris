using UnityEngine;

/// <summary>
/// Single source of truth for the retro-arcade colour scheme. Views name
/// colours instead of inlining literals, so a palette change is one edit.
/// </summary>
public static class RetroPalette
{
    public static readonly Color Backdrop = new Color(0.018f, 0.025f, 0.07f);
    public static readonly Color BackdropFrame = new Color(0.05f, 0.12f, 0.22f);
    public static readonly Color CameraClear = new Color(0.025f, 0.03f, 0.055f, 1f);

    public static readonly Color PanelFill = new Color(0.025f, 0.045f, 0.11f, 0.94f);
    public static readonly Color PanelFillSoft = new Color(0.025f, 0.045f, 0.11f, 0.9f);
    public static readonly Color PanelFillDeep = new Color(0.018f, 0.026f, 0.075f, 0.96f);
    public static readonly Color PanelFillGlass = new Color(0.018f, 0.026f, 0.075f, 0.93f);
    public static readonly Color PanelFillInfo = new Color(0.02f, 0.04f, 0.1f, 0.96f);
    public static readonly Color OverlayPanel = new Color(0.018f, 0.035f, 0.1f, 0.98f);
    public static readonly Color OverlayScrim = new Color(0.005f, 0.01f, 0.035f, 0.58f);
    public static readonly Color StoryScrim = new Color(0.01f, 0.015f, 0.05f, 0.12f);
    public static readonly Color ResultFill = new Color(0.02f, 0.035f, 0.09f, 0.97f);
    public static readonly Color LocationFill = new Color(0.025f, 0.045f, 0.12f, 0.9f);

    public static readonly Color BorderBlue = new Color(0.33f, 0.75f, 0.94f, 0.85f);
    public static readonly Color BorderBlueSoft = new Color(0.24f, 0.52f, 0.72f, 0.8f);
    public static readonly Color BorderCyan = new Color(0.42f, 0.83f, 1f);

    public static readonly Color Gold = new Color(1f, 0.78f, 0.3f);
    public static readonly Color GoldBright = new Color(1f, 0.8f, 0.31f);
    public static readonly Color GoldFrame = new Color(0.95f, 0.72f, 0.24f);
    public static readonly Color GoldText = new Color(1f, 0.84f, 0.38f);
    public static readonly Color Rose = new Color(0.92f, 0.3f, 0.56f);

    public static readonly Color ReadyAccent = new Color(0.3f, 0.88f, 1f);
    public static readonly Color StartAccent = new Color(1f, 0.78f, 0.26f);
    public static readonly Color WinnerFill = new Color(0.11f, 0.095f, 0.025f, 0.97f);
    public static readonly Color LoserFill = new Color(0.09f, 0.035f, 0.08f, 0.97f);

    public static readonly Color CardSelected = new Color(0.1f, 0.2f, 0.32f, 1f);
    public static readonly Color CardIdle = new Color(0.025f, 0.045f, 0.11f, 0.98f);
    public static readonly Color CardNameplate = new Color(0.015f, 0.025f, 0.075f, 0.98f);
    public static readonly Color LockedBorder = new Color(0.32f, 0.29f, 0.44f);
    public static readonly Color LockedAccent = new Color(0.48f, 0.42f, 0.64f);
    public static readonly Color PlayerOneBadge = new Color(0.45f, 0.08f, 0.42f, 0.95f);

    public static readonly Color PortraitBackdrop = new Color(0.025f, 0.035f, 0.08f, 0.95f);
    public static readonly Color PortraitFallback = new Color(0.015f, 0.018f, 0.04f);
    public static readonly Color PortraitNameLeft = new Color(0.08f, 0.03f, 0.13f, 0.96f);
    public static readonly Color PortraitNameRight = new Color(0.02f, 0.1f, 0.16f, 0.96f);

    public static readonly Color NameplateLyra = new Color(0.25f, 0.055f, 0.28f, 0.98f);
    public static readonly Color NameplateBram = new Color(0.025f, 0.18f, 0.25f, 0.98f);
    public static readonly Color NameplateNeutral = new Color(0.08f, 0.09f, 0.16f, 0.98f);
}
