using System;
using UnityEngine;

/// <summary>Visual-novel presentation driven by a <see cref="StoryDirector"/>.</summary>
public sealed class StoryScreen : IGameScreen
{
    private readonly StoryDirector story;
    private readonly IGameFlow flow;
    private readonly RetroTheme theme;
    private readonly BattleArtLibrary art;
    private readonly Func<CpuDifficulty> difficulty;
    private readonly SaveSlotCatalog saves;
    private readonly StoryPauseModel pause;

    public StoryScreen(
        StoryDirector story,
        IGameFlow flow,
        RetroTheme theme,
        BattleArtLibrary art,
        Func<CpuDifficulty> difficulty,
        SaveSlotCatalog saves)
    {
        this.story = story;
        this.flow = flow;
        this.theme = theme;
        this.art = art;
        this.difficulty = difficulty;
        this.saves = saves;
        pause = new StoryPauseModel(saves);
    }

    /// <summary>The chapter clock and dialogue input both stop while this is true.</summary>
    public bool IsPaused => pause.IsOpen;

    public void Enter()
    {
    }

    public void Exit()
    {
        pause.Close();
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        if (pause.IsOpen)
        {
            TickPause(input);
            return;
        }

        // Escape raises Pause and Cancel together, so opening lives in this
        // branch only: the same press can never open and then close the menu.
        if (input.Pause)
        {
            saves.Refresh();
            pause.Open();
            GameAudio.Play(GameSfx.MenuConfirm);
            return;
        }

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
        // While the menu is up the chapter behind it is scenery: disabled IMGUI
        // controls neither respond to nor swallow the clicks meant for the
        // overlay, which a discarded return value alone would not prevent.
        bool paused = pause.IsOpen;
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !paused;
        int clicked = StoryView.Draw(story, difficulty(), theme, art);
        GUI.enabled = wasEnabled;

        if (paused)
        {
            int pauseClick = StoryPauseView.Draw(pause, saves, theme, story.PlaytimeSeconds);
            if (pauseClick != StoryPauseView.NoClick)
            {
                GameAudio.Play(GameSfx.MenuConfirm);
                HandlePauseCommand(pause.ConfirmAt(pauseClick));
            }

            return;
        }

        if (clicked == StoryView.NoClick)
            return;

        story.SetSelection(clicked);
        HandleIntent(story.Confirm());
    }

    private void TickPause(in UiInput input)
    {
        // Escape backs out one page at a time and closes from the root; a
        // gamepad Start button (Pause without Cancel) closes from anywhere.
        if (input.Cancel)
        {
            GameAudio.Play(GameSfx.MenuBack);
            HandlePauseCommand(pause.Back());
            return;
        }

        if (input.Pause)
        {
            GameAudio.Play(GameSfx.MenuBack);
            ClosePause();
            return;
        }

        int deltaX = (input.Right ? 1 : 0) - (input.Left ? 1 : 0);
        int deltaY = (input.Down ? 1 : 0) - (input.Up ? 1 : 0);
        if (deltaX != 0 || deltaY != 0)
        {
            pause.Move(deltaX, deltaY);
            GameAudio.Play(GameSfx.MenuMove);
        }

        if (input.Confirm)
        {
            GameAudio.Play(GameSfx.MenuConfirm);
            HandlePauseCommand(pause.Confirm());
        }
    }

    private void HandlePauseCommand(StoryPauseCommand command)
    {
        switch (command.Intent)
        {
            case StoryPauseIntent.Resume:
                ClosePause();
                break;

            case StoryPauseIntent.Save:
                if (flow.SaveStory(command.Slot))
                    pause.ReportSaved(command.Slot);
                else
                    pause.ReportSaveFailed();
                break;

            case StoryPauseIntent.Load:
                // A successful load re-routes to this screen, and Exit closes
                // the menu; only the failure path has to be handled here.
                if (!flow.LoadStory(command.Slot))
                    pause.ReportLoadFailed();
                break;

            case StoryPauseIntent.ReturnToTitle:
                ClosePause();
                flow.ShowTitleMenu();
                break;
        }
    }

    private void ClosePause()
    {
        pause.Close();
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
