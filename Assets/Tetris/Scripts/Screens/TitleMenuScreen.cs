/// <summary>The title menu: mode root page and CPU difficulty sub-page.</summary>
public sealed class TitleMenuScreen : IGameScreen
{
    private readonly MainMenuModel model;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;

    public TitleMenuScreen(MainMenuModel model, IGameFlow flow, RetroTheme theme)
    {
        this.model = model;
        this.flow = flow;
        this.theme = theme;
    }

    public void Enter()
    {
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        if (input.Cancel && model.Back())
            return;

        if (input.Up)
            model.Move(-1);
        else if (input.Down)
            model.Move(1);

        if (input.Confirm)
            Activate(model.Activate());
    }

    public void Draw()
    {
        int clicked = MainMenuView.Draw(model, theme);
        if (clicked != MainMenuView.NoClick)
            Activate(model.Activate(clicked));
    }

    private void Activate(MainMenuCommand command)
    {
        switch (command.Intent)
        {
            case MainMenuIntent.StartStory:
                flow.BeginStory();
                break;
            case MainMenuIntent.OpenCharacterSelect:
                flow.ShowCharacterSelect(command.VersusMode);
                break;
        }
    }
}
