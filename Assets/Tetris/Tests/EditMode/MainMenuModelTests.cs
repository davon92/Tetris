using NUnit.Framework;

public class MainMenuModelTests
{
    private static MainMenuModel StoryPage(bool hasAnySave)
    {
        MainMenuModel model = new MainMenuModel { HasAnySave = hasAnySave };
        model.Activate(MainMenuModel.StoryRow);
        return model;
    }

    [Test]
    public void StoryMode_OpensTheNewGameLoadGamePage()
    {
        MainMenuModel model = new MainMenuModel();

        Assert.That(
            model.Activate(MainMenuModel.StoryRow).Intent,
            Is.EqualTo(MainMenuIntent.None));
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.Story));
        Assert.That(model.ItemCount, Is.EqualTo(MainMenuModel.StoryItemCount));
        Assert.That(model.Selection, Is.EqualTo(MainMenuModel.NewGameRow));
    }

    [Test]
    public void NewGame_StartsTheChapter()
    {
        MainMenuModel model = StoryPage(hasAnySave: true);

        Assert.That(
            model.Activate(MainMenuModel.NewGameRow).Intent,
            Is.EqualTo(MainMenuIntent.StartStory));
    }

    [Test]
    public void LoadGame_OpensTheSlotBrowserOnlyWhenSavesExist()
    {
        MainMenuModel empty = StoryPage(hasAnySave: false);

        Assert.That(
            empty.Activate(MainMenuModel.LoadGameRow).Intent,
            Is.EqualTo(MainMenuIntent.None));
        Assert.That(empty.Message, Is.EqualTo(MainMenuModel.NoSavesMessage));
        Assert.That(empty.Page, Is.EqualTo(MainMenuPage.Story));

        MainMenuModel saved = StoryPage(hasAnySave: true);
        Assert.That(
            saved.Activate(MainMenuModel.LoadGameRow).Intent,
            Is.EqualTo(MainMenuIntent.OpenLoadGame));
    }

    [Test]
    public void MovingOffARefusedItem_ClearsTheMessage()
    {
        MainMenuModel model = StoryPage(hasAnySave: false);
        model.Activate(MainMenuModel.LoadGameRow);

        model.Move(1);

        Assert.That(model.Message, Is.Empty);
    }

    [Test]
    public void BackFromTheStoryPage_ReturnsTheCursorToStoryMode()
    {
        MainMenuModel model = StoryPage(hasAnySave: false);

        Assert.That(model.Back(), Is.True);
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.Root));
        Assert.That(model.Selection, Is.EqualTo(MainMenuModel.StoryRow));

        // The root page has nothing to back out to.
        Assert.That(model.Back(), Is.False);
    }

    [Test]
    public void StoryPageBackRow_AlsoReturnsToTheRoot()
    {
        MainMenuModel model = StoryPage(hasAnySave: true);

        Assert.That(
            model.Activate(MainMenuModel.StoryItemCount - 1).Intent,
            Is.EqualTo(MainMenuIntent.None));
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.Root));
        Assert.That(model.Selection, Is.EqualTo(MainMenuModel.StoryRow));
    }

    [Test]
    public void TheOtherRootRows_StillReachTheirOwnPages()
    {
        MainMenuModel model = new MainMenuModel();

        Assert.That(model.Activate(MainMenuModel.SoloRow).Intent, Is.EqualTo(MainMenuIntent.None));
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.SoloMode));
        model.Back();

        Assert.That(model.Activate(MainMenuModel.VersusRow).Intent, Is.EqualTo(MainMenuIntent.None));
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.VersusMode));
        model.Back();

        Assert.That(
            model.Activate(MainMenuModel.OptionsRow).Intent,
            Is.EqualTo(MainMenuIntent.OpenOptions));
    }

    [Test]
    public void DifficultyPage_StillLeadsToCharacterSelect()
    {
        MainMenuModel model = new MainMenuModel();
        model.Activate(MainMenuModel.VersusRow);

        Assert.That(
            model.Activate(MainMenuModel.VersusCpuRow).Intent,
            Is.EqualTo(MainMenuIntent.None));
        Assert.That(model.Page, Is.EqualTo(MainMenuPage.CpuDifficulty));

        MainMenuCommand command = model.Activate((int)CpuDifficulty.Hard);
        Assert.That(command.Intent, Is.EqualTo(MainMenuIntent.OpenCharacterSelect));
        Assert.That(command.Mode, Is.EqualTo(TetrisGameMode.VersusCpu));
        Assert.That(model.Difficulty, Is.EqualTo(CpuDifficulty.Hard));
    }

    [Test]
    public void VersusPlayer_GoesStraightToCharacterSelect()
    {
        MainMenuModel model = new MainMenuModel();
        model.Activate(MainMenuModel.VersusRow);

        MainMenuCommand command = model.Activate(MainMenuModel.VersusPlayerRow);

        Assert.That(command.Intent, Is.EqualTo(MainMenuIntent.OpenCharacterSelect));
        Assert.That(command.Mode, Is.EqualTo(TetrisGameMode.LocalVersus));
    }
}
