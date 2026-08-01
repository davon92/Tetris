using System;

/// <summary>Visual-novel presentation driven by a <see cref="StoryDirector"/>.</summary>
public sealed class StoryScreen : IGameScreen
{
    private readonly StoryDirector story;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;
    private readonly BattleArtLibrary art;
    private readonly Func<CpuDifficulty> difficulty;

    public StoryScreen(
        StoryDirector story,
        IGameFlow flow,
        RetroTheme theme,
        BattleArtLibrary art,
        Func<CpuDifficulty> difficulty)
    {
        this.story = story;
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
        if (story.HasChoices || story.Beat == StoryBeat.Result)
        {
            if (input.Up || input.Down)
                story.MoveSelection();
        }

        if (input.Confirm)
            HandleIntent(story.Confirm());
    }

    public void Draw()
    {
        int clicked = StoryView.Draw(story, difficulty(), theme, art);
        if (clicked == StoryView.NoClick)
            return;

        story.SetSelection(clicked);
        HandleIntent(story.Confirm());
    }

    private void HandleIntent(StoryIntent intent)
    {
        switch (intent)
        {
            case StoryIntent.StartBattle:
                flow.RequestStoryBattle();
                break;
            case StoryIntent.ReturnToMenu:
                flow.ShowTitleMenu();
                break;
        }
    }
}
