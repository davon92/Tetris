using System;
using NUnit.Framework;

public class SaveSlotMenuModelTests
{
    [Test]
    public void Columns_HoldFiveConsecutiveSlots()
    {
        SaveSlotMenuModel model = new SaveSlotMenuModel();
        model.Begin(SaveSlotMenuMode.Load);

        model.Move(0, 1);
        Assert.That(model.Cursor, Is.EqualTo(1));

        model.Move(1, 0);
        Assert.That(model.Cursor, Is.EqualTo(SaveSlotMenuModel.Rows + 1));

        model.Move(-1, 0);
        Assert.That(model.Cursor, Is.EqualTo(1));
    }

    [Test]
    public void WalkingPastEitherEndOfAColumn_LandsOnBack()
    {
        SaveSlotMenuModel model = new SaveSlotMenuModel();
        model.Begin(SaveSlotMenuMode.Load);

        model.Move(0, -1);
        Assert.That(model.IsBackSelected, Is.True);

        model.Move(0, 1);
        Assert.That(model.Cursor, Is.EqualTo(0));

        for (int i = 0; i < SaveSlotMenuModel.Rows; i++)
            model.Move(0, 1);

        Assert.That(model.IsBackSelected, Is.True);
    }

    [Test]
    public void Back_RemembersTheColumnItWasReachedFrom()
    {
        SaveSlotMenuModel model = new SaveSlotMenuModel();
        model.Begin(SaveSlotMenuMode.Load, SaveSlotMenuModel.Rows * 2 - 1);

        model.Move(0, 1);
        Assert.That(model.IsBackSelected, Is.True);

        model.Move(0, -1);
        Assert.That(model.Cursor, Is.EqualTo(SaveSlotMenuModel.Rows * 2 - 1));
    }

    [Test]
    public void Back_ConfirmsAsLeave()
    {
        SaveSlotMenuModel model = new SaveSlotMenuModel();
        model.Begin(SaveSlotMenuMode.Save);

        Assert.That(model.Confirm(), Is.EqualTo(SaveSlotIntent.Use));

        model.MoveTo(SaveSlotMenuModel.BackIndex);
        Assert.That(model.Confirm(), Is.EqualTo(SaveSlotIntent.Back));
    }
}

public class StoryPauseModelTests
{
    private static SaveSlotCatalog CatalogWith(params int[] occupiedSlots)
    {
        SaveSlotCatalog catalog = new SaveSlotCatalog(new MemoryJsonStore());
        StoryDirector story = new StoryDirector(new PrologueStoryScript());
        story.Begin();

        foreach (int slot in occupiedSlots)
            catalog.Save(slot, StorySaveData.Capture(story, DateTime.UtcNow));

        return catalog;
    }

    private static StoryPauseModel OpenPause(SaveSlotCatalog catalog)
    {
        StoryPauseModel pause = new StoryPauseModel(catalog);
        pause.Open();
        return pause;
    }

    [Test]
    public void Menu_StartsClosedAndOpensOnTheResumeItem()
    {
        StoryPauseModel pause = new StoryPauseModel(CatalogWith());

        Assert.That(pause.IsOpen, Is.False);

        pause.Open();
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Root));
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.ResumeItem));
    }

    [Test]
    public void ConfirmingResume_AsksTheScreenToClose()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());

        Assert.That(pause.Confirm().Intent, Is.EqualTo(StoryPauseIntent.Resume));
    }

    [Test]
    public void BackFromTheRootPage_AlsoResumes()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());
        pause.Move(0, 1);

        Assert.That(pause.Back().Intent, Is.EqualTo(StoryPauseIntent.Resume));
    }

    [Test]
    public void RootSelection_WrapsThroughFourItems()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());

        pause.Move(0, -1);
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.ExitItem));

        pause.Move(0, 1);
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.ResumeItem));
    }

    [Test]
    public void SavingToAnEmptySlot_NeedsNoConfirmation()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());
        pause.Move(0, 1);

        Assert.That(pause.Confirm().Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Slots));
        Assert.That(pause.Slots.Mode, Is.EqualTo(SaveSlotMenuMode.Save));

        StoryPauseCommand command = pause.ConfirmAt(4);
        Assert.That(command.Intent, Is.EqualTo(StoryPauseIntent.Save));
        Assert.That(command.Slot, Is.EqualTo(4));
    }

    [Test]
    public void SavingOverAnOccupiedSlot_AsksFirstAndDefaultsToNo()
    {
        StoryPauseModel pause = OpenPause(CatalogWith(4));
        pause.ConfirmAt(StoryPauseModel.SaveItem);

        Assert.That(pause.ConfirmAt(4).Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.ConfirmOverwrite));
        Assert.That(pause.PendingSlot, Is.EqualTo(4));
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.ConfirmNo));

        // Declining returns to the list without writing anything.
        Assert.That(pause.Confirm().Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Slots));

        pause.ConfirmAt(4);
        pause.Move(-1, 0);
        StoryPauseCommand command = pause.Confirm();
        Assert.That(command.Intent, Is.EqualTo(StoryPauseIntent.Save));
        Assert.That(command.Slot, Is.EqualTo(4));
    }

    [Test]
    public void ReportSaved_KeepsTheListUpAndSaysWhereItWent()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());
        pause.ConfirmAt(StoryPauseModel.SaveItem);
        pause.ConfirmAt(2);

        pause.ReportSaved(2);

        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Slots));
        Assert.That(pause.Message, Is.EqualTo("SAVED TO SLOT 03"));
    }

    [Test]
    public void LoadingAnEmptySlot_SaysSoInsteadOfLoading()
    {
        StoryPauseModel pause = OpenPause(CatalogWith(1));
        pause.ConfirmAt(StoryPauseModel.LoadItem);

        Assert.That(pause.Slots.Mode, Is.EqualTo(SaveSlotMenuMode.Load));
        Assert.That(pause.ConfirmAt(8).Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Message, Is.EqualTo(StoryPauseModel.EmptySlotMessage));

        StoryPauseCommand command = pause.ConfirmAt(1);
        Assert.That(command.Intent, Is.EqualTo(StoryPauseIntent.Load));
        Assert.That(command.Slot, Is.EqualTo(1));
    }

    [Test]
    public void LeavingTheSlotList_ReturnsToTheItemItWasOpenedFrom()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());
        pause.ConfirmAt(StoryPauseModel.LoadItem);

        Assert.That(pause.Back().Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Root));
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.LoadItem));
    }

    [Test]
    public void SlotListBackButton_AlsoReturnsToTheRoot()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());
        pause.ConfirmAt(StoryPauseModel.SaveItem);

        Assert.That(pause.ConfirmAt(SaveSlotMenuModel.BackIndex).Intent,
            Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Root));
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.SaveItem));
    }

    [Test]
    public void ExitingToTheTitle_NeedsAConfirmationThatDefaultsToNo()
    {
        StoryPauseModel pause = OpenPause(CatalogWith());

        Assert.That(pause.ConfirmAt(StoryPauseModel.ExitItem).Intent,
            Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.ConfirmExit));
        Assert.That(pause.Selection, Is.EqualTo(StoryPauseModel.ConfirmNo));

        Assert.That(pause.Confirm().Intent, Is.EqualTo(StoryPauseIntent.None));
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Root));

        pause.ConfirmAt(StoryPauseModel.ExitItem);
        pause.Move(1, 0);
        Assert.That(pause.Confirm().Intent, Is.EqualTo(StoryPauseIntent.ReturnToTitle));
    }

    [Test]
    public void BackFromAModal_ReturnsToThePageBehindIt()
    {
        StoryPauseModel pause = OpenPause(CatalogWith(0));

        pause.ConfirmAt(StoryPauseModel.ExitItem);
        pause.Back();
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Root));

        pause.ConfirmAt(StoryPauseModel.SaveItem);
        pause.ConfirmAt(0);
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.ConfirmOverwrite));

        pause.Back();
        Assert.That(pause.Page, Is.EqualTo(StoryPausePage.Slots));
        Assert.That(pause.PendingSlot, Is.EqualTo(-1));
    }

    [Test]
    public void Closing_ForgetsEverything()
    {
        StoryPauseModel pause = OpenPause(CatalogWith(0));
        pause.ConfirmAt(StoryPauseModel.SaveItem);
        pause.ConfirmAt(0);

        pause.Close();

        Assert.That(pause.IsOpen, Is.False);
        Assert.That(pause.PendingSlot, Is.EqualTo(-1));
        Assert.That(pause.Message, Is.Empty);
    }
}
