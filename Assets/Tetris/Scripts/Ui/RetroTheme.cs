using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Owns every <see cref="GUIStyle"/> and generated texture the HUD uses.
/// Must be constructed from inside <c>OnGUI</c> because it reads
/// <see cref="GUI.skin"/>. Disposing releases the generated textures.
/// </summary>
public sealed class RetroTheme : IDisposable
{
    private readonly Texture2D buttonTexture;
    private readonly Texture2D buttonHoverTexture;
    private readonly Texture2D buttonSelectedTexture;

    public RetroTheme()
    {
        PanelBackground = RetroGui.CreateSolidTexture(new Color(0.035f, 0.055f, 0.13f, 0.97f));
        buttonTexture = RetroGui.CreateSolidTexture(new Color(0.055f, 0.11f, 0.22f, 1f));
        buttonHoverTexture = RetroGui.CreateSolidTexture(new Color(0.1f, 0.21f, 0.34f, 1f));
        buttonSelectedTexture = RetroGui.CreateSolidTexture(new Color(0.12f, 0.32f, 0.43f, 1f));

        Title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.82f, 0.93f, 1f) }
        };

        Hud = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        Help = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 8,
            normal = { textColor = new Color(0.7f, 0.76f, 0.86f) }
        };

        MenuTitle = new GUIStyle(Title)
        {
            fontSize = 30,
            normal = { textColor = RetroPalette.GoldBright }
        };

        MenuSubtitle = new GUIStyle(Help)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.44f, 0.92f, 1f) }
        };

        MenuHeading = new GUIStyle(Title)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.82f, 0.93f, 1f) }
        };

        MenuButton = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                background = buttonTexture,
                textColor = new Color(0.88f, 0.95f, 1f)
            },
            hover =
            {
                background = buttonHoverTexture,
                textColor = Color.white
            },
            active =
            {
                background = buttonSelectedTexture,
                textColor = Color.white
            }
        };

        SelectedMenuButton = new GUIStyle(MenuButton);
        SelectedMenuButton.normal.background = buttonSelectedTexture;
        SelectedMenuButton.normal.textColor = RetroPalette.GoldText;

        MenuDetail = new GUIStyle(Help)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.62f, 0.72f, 0.84f) }
        };

        MenuFooter = new GUIStyle(Help)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.52f, 0.64f, 0.76f) }
        };

        MatchCallout = new GUIStyle(MenuTitle)
        {
            fontSize = 34
        };

        MatchRole = new GUIStyle(MenuFooter)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.72f, 0.87f, 1f) }
        };

        MatchWinner = new GUIStyle(MenuHeading)
        {
            fontSize = 11,
            normal = { textColor = RetroPalette.GoldText }
        };

        MatchLoser = new GUIStyle(MenuHeading)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.5f, 0.7f) }
        };

        StoryLocation = new GUIStyle(Title)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.88f, 0.56f) }
        };

        StoryName = new GUIStyle(Title)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        StoryDialogue = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.93f, 0.96f, 1f) }
        };

        StoryPrompt = new GUIStyle(Help)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.5f, 0.9f, 1f) }
        };

        CharacterName = new GUIStyle(MenuHeading)
        {
            fontSize = 18,
            normal = { textColor = RetroPalette.GoldText }
        };

        CharacterTitle = new GUIStyle(MenuFooter)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.76f, 0.87f, 1f) }
        };
    }

    public Texture2D PanelBackground { get; }

    public GUIStyle Title { get; }
    public GUIStyle Hud { get; }
    public GUIStyle Help { get; }
    public GUIStyle MenuTitle { get; }
    public GUIStyle MenuSubtitle { get; }
    public GUIStyle MenuHeading { get; }
    public GUIStyle MenuButton { get; }
    public GUIStyle SelectedMenuButton { get; }
    public GUIStyle MenuDetail { get; }
    public GUIStyle MenuFooter { get; }
    public GUIStyle MatchCallout { get; }
    public GUIStyle MatchRole { get; }
    public GUIStyle MatchWinner { get; }
    public GUIStyle MatchLoser { get; }
    public GUIStyle StoryLocation { get; }
    public GUIStyle StoryName { get; }
    public GUIStyle StoryDialogue { get; }
    public GUIStyle StoryPrompt { get; }
    public GUIStyle CharacterName { get; }
    public GUIStyle CharacterTitle { get; }

    /// <summary>
    /// Draws a menu button, optionally at a one-off font size. Callers no
    /// longer have to save and restore <see cref="GUIStyle.fontSize"/>.
    /// </summary>
    public bool Button(Rect rect, string label, bool selected, int fontSize = 0)
    {
        GUIStyle style = selected ? SelectedMenuButton : MenuButton;
        if (fontSize <= 0)
            return GUI.Button(rect, label, style);

        int previousFontSize = style.fontSize;
        style.fontSize = fontSize;
        bool clicked = GUI.Button(rect, label, style);
        style.fontSize = previousFontSize;
        return clicked;
    }

    public void Dispose()
    {
        DestroyTexture(PanelBackground);
        DestroyTexture(buttonTexture);
        DestroyTexture(buttonHoverTexture);
        DestroyTexture(buttonSelectedTexture);
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
            Object.Destroy(texture);
    }
}
