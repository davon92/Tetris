using UnityEngine;

/// <summary>
/// The title screen's load route: the same ten-slot browser story mode's pause
/// menu uses, in load mode, shown before the prologue starts.
/// </summary>
public sealed class SaveSlotScreen : IGameScreen
{
    private readonly SaveSlotMenuModel model;
    private readonly SaveSlotCatalog catalog;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;

    public SaveSlotScreen(
        SaveSlotMenuModel model,
        SaveSlotCatalog catalog,
        IGameFlow flow,
        RetroTheme theme)
    {
        this.model = model;
        this.catalog = catalog;
        this.flow = flow;
        this.theme = theme;
    }

    public void Enter()
    {
        catalog.Refresh();
        model.Begin(SaveSlotMenuMode.Load);
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        if (input.Cancel)
        {
            GameAudio.Play(GameSfx.MenuBack);
            flow.CloseLoadGame();
            return;
        }

        int deltaX = (input.Right ? 1 : 0) - (input.Left ? 1 : 0);
        int deltaY = (input.Down ? 1 : 0) - (input.Up ? 1 : 0);
        if (deltaX != 0 || deltaY != 0)
        {
            model.Move(deltaX, deltaY);
            GameAudio.Play(GameSfx.MenuMove);
        }

        if (input.Confirm)
            Activate();
    }

    public void Draw()
    {
        MenuChromeView.DrawFrame(theme, wide: true);
        GUI.DrawTexture(SaveSlotView.Panel, theme.PanelBackground);
        RetroGui.Border(SaveSlotView.Panel, RetroPalette.GoldFrame, 2f);

        int clicked = SaveSlotView.Draw(model, catalog, theme, "LOAD ADVENTURE");
        MenuChromeView.DrawFooter(theme, MenuChromeView.RosterHint);

        if (clicked == SaveSlotView.NoClick)
            return;

        model.MoveTo(clicked);
        Activate();
    }

    private void Activate()
    {
        if (model.Confirm() == SaveSlotIntent.Back)
        {
            GameAudio.Play(GameSfx.MenuBack);
            flow.CloseLoadGame();
            return;
        }

        SaveSlotInfo info = catalog.GetSlot(model.Cursor);
        if (info.IsEmpty)
        {
            model.SetMessage(StoryPauseModel.EmptySlotMessage);
            GameAudio.Play(GameSfx.MenuBack);
            return;
        }

        GameAudio.Play(GameSfx.MenuConfirm);
        if (!flow.LoadStory(model.Cursor))
            model.SetMessage(StoryPauseModel.DamagedSlotMessage);
    }
}
