using System;

/// <summary>The horizontal roster picker shared by both versus routes.</summary>
public sealed class CharacterSelectScreen : IGameScreen
{
    private readonly CharacterSelectModel model;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;
    private readonly BattleArtLibrary art;
    private readonly Func<CpuDifficulty> difficulty;

    public CharacterSelectScreen(
        CharacterSelectModel model,
        IGameFlow flow,
        RetroTheme theme,
        BattleArtLibrary art,
        Func<CpuDifficulty> difficulty)
    {
        this.model = model;
        this.flow = flow;
        this.theme = theme;
        this.art = art;
        this.difficulty = difficulty;
    }

    public void Enter()
    {
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        if (input.Cancel)
        {
            HandleIntent(model.Back());
            return;
        }

        if (input.Left)
            model.Move(-1);
        else if (input.Right)
            model.Move(1);

        if (input.Confirm)
            HandleIntent(model.Confirm());
    }

    public void Draw()
    {
        CharacterSelectClick click = CharacterSelectView.Draw(model, difficulty(), theme, art);
        if (click.CardIndex >= 0)
            model.MoveTo(click.CardIndex);

        if (click.Back)
            HandleIntent(model.Back());

        if (click.Confirm)
            HandleIntent(model.Confirm());
    }

    private void HandleIntent(CharacterSelectIntent intent)
    {
        switch (intent)
        {
            case CharacterSelectIntent.Leave:
                flow.CloseCharacterSelect();
                break;
            case CharacterSelectIntent.Ready:
                flow.BeginMatch(model.VersusMode);
                break;
        }
    }
}
