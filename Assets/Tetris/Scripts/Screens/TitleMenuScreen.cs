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
        {
            GameAudio.Play(GameSfx.MenuBack);
            return;
        }

        // The quit modal lays its two answers out side by side, so it takes the
        // horizontal axis as well; every other page is a vertical list.
        bool previous = input.Up || (model.QuitConfirmActive && input.Left);
        bool next = input.Down || (model.QuitConfirmActive && input.Right);

        if (previous)
        {
            model.Move(-1);
            GameAudio.Play(GameSfx.MenuMove);
        }
        else if (next)
        {
            model.Move(1);
            GameAudio.Play(GameSfx.MenuMove);
        }

        if (input.Confirm)
        {
            GameAudio.Play(GameSfx.MenuConfirm);
            Activate(model.Activate());
        }
    }

    public void Draw()
    {
        int clicked = MainMenuView.Draw(model, theme);
        if (clicked == MainMenuView.NoClick)
            return;

        GameAudio.Play(GameSfx.MenuConfirm);
        Activate(model.Activate(clicked));
    }

    private void Activate(MainMenuCommand command)
    {
        switch (command.Intent)
        {
            case MainMenuIntent.StartStory:
                flow.BeginStory();
                break;
            case MainMenuIntent.OpenLoadGame:
                flow.ShowLoadGame();
                break;
            case MainMenuIntent.StartSolo:
                flow.BeginMatch(command.Mode);
                break;
            case MainMenuIntent.OpenCharacterSelect:
                flow.ShowCharacterSelect(command.Mode);
                break;
            case MainMenuIntent.OpenOptions:
                flow.ShowOptions();
                break;
            case MainMenuIntent.QuitGame:
                flow.QuitGame();
                break;
        }
    }
}
