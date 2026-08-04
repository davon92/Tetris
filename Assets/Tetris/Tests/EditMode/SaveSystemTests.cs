using System;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SaveSystemTests
{
    private static StoryDirector RunningStory(float playtime = 0f)
    {
        StoryDirector story = new StoryDirector(new PrologueStoryScript());
        story.Begin();
        story.AddPlaytime(playtime);
        return story;
    }

    private static SaveSlotCatalog NewCatalog(out MemoryJsonStore store)
    {
        store = new MemoryJsonStore();
        return new SaveSlotCatalog(store);
    }

    [Test]
    public void EverySlot_StartsEmpty()
    {
        SaveSlotCatalog catalog = NewCatalog(out _);

        Assert.That(catalog.Slots.Count, Is.EqualTo(SaveSlotCatalog.SlotCount));
        Assert.That(catalog.HasAnySave, Is.False);
        for (int i = 0; i < SaveSlotCatalog.SlotCount; i++)
            Assert.That(catalog.GetSlot(i).IsEmpty, Is.True);
    }

    [Test]
    public void SavedChapter_RoundTripsThroughTheStore()
    {
        SaveSlotCatalog catalog = NewCatalog(out MemoryJsonStore store);
        StoryDirector story = RunningStory(125f);
        story.Confirm();
        story.Confirm();

        Assert.That(catalog.Save(3, StorySaveData.Capture(story, DateTime.UtcNow)), Is.True);

        // A second catalog over the same store proves the data survived the
        // in-memory summary cache.
        SaveSlotCatalog reopened = new SaveSlotCatalog(store);
        Assert.That(reopened.TryLoad(3, out StorySaveData data), Is.True);

        StoryDirector restored = new StoryDirector(new PrologueStoryScript());
        Assert.That(restored.Restore(data.ToProgress()), Is.True);
        Assert.That(restored.IsRunning, Is.True);
        Assert.That(restored.Beat, Is.EqualTo(story.Beat));
        Assert.That(restored.CurrentLine.Text, Is.EqualTo(story.CurrentLine.Text));
        Assert.That(restored.PlaytimeSeconds, Is.EqualTo(125f).Within(0.01f));
    }

    [Test]
    public void SlotSummary_CarriesChapterPlaytimeAndPreview()
    {
        SaveSlotCatalog catalog = NewCatalog(out _);
        StoryDirector story = RunningStory(3725f);

        catalog.Save(0, StorySaveData.Capture(story, new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc)));

        SaveSlotInfo info = catalog.GetSlot(0);
        Assert.That(info.IsEmpty, Is.False);
        Assert.That(info.IsUsable, Is.True);
        Assert.That(info.ChapterTitle, Is.EqualTo(story.Script.LocationTitle));
        Assert.That(info.Speaker, Is.EqualTo(story.CurrentLine.Speaker));
        Assert.That(info.PlaytimeText, Is.EqualTo("01:02:05"));
        Assert.That(catalog.HasAnySave, Is.True);
    }

    [Test]
    public void Saving_OverAnOccupiedSlot_ReplacesIt()
    {
        SaveSlotCatalog catalog = NewCatalog(out _);
        StoryDirector first = RunningStory(10f);
        catalog.Save(7, StorySaveData.Capture(first, DateTime.UtcNow));

        StoryDirector second = RunningStory(90f);
        second.Confirm();
        catalog.Save(7, StorySaveData.Capture(second, DateTime.UtcNow));

        Assert.That(catalog.GetSlot(7).PlaytimeSeconds, Is.EqualTo(90f).Within(0.01f));
        Assert.That(catalog.TryLoad(7, out StorySaveData data), Is.True);
        Assert.That(data.lineIndex, Is.EqualTo(1));
    }

    [Test]
    public void Delete_EmptiesTheSlot()
    {
        SaveSlotCatalog catalog = NewCatalog(out _);
        catalog.Save(2, StorySaveData.Capture(RunningStory(), DateTime.UtcNow));

        Assert.That(catalog.Delete(2), Is.True);
        Assert.That(catalog.GetSlot(2).IsEmpty, Is.True);
        Assert.That(catalog.HasAnySave, Is.False);
        Assert.That(catalog.TryLoad(2, out _), Is.False);
    }

    [Test]
    public void OutOfRangeSlots_AreRejected()
    {
        SaveSlotCatalog catalog = NewCatalog(out _);
        StorySaveData data = StorySaveData.Capture(RunningStory(), DateTime.UtcNow);

        Assert.That(catalog.Save(-1, data), Is.False);
        Assert.That(catalog.Save(SaveSlotCatalog.SlotCount, data), Is.False);
        Assert.That(catalog.TryLoad(SaveSlotCatalog.SlotCount, out _), Is.False);
        Assert.That(catalog.HasAnySave, Is.False);
    }

    [Test]
    public void UnreadableSlot_IsListedButRefusesToLoad()
    {
        // Parsing a damaged slot reports the failure before recovering.
        LogAssert.ignoreFailingMessages = true;

        MemoryJsonStore store = new MemoryJsonStore();
        store.Write(SaveSlotCatalog.KeyFor(1), "{ this is not json");

        SaveSlotCatalog catalog = new SaveSlotCatalog(store);
        SaveSlotInfo info = catalog.GetSlot(1);

        Assert.That(info.IsEmpty, Is.False);
        Assert.That(info.IsUsable, Is.False);
        Assert.That(catalog.TryLoad(1, out _), Is.False);
    }

    [Test]
    public void SlotFromANewerBuild_IsNotLoaded()
    {
        MemoryJsonStore store = new MemoryJsonStore();
        store.Write(
            SaveSlotCatalog.KeyFor(4),
            "{\"version\":999,\"chapterId\":\"prologue-moon-gate\"}");

        SaveSlotCatalog catalog = new SaveSlotCatalog(store);

        Assert.That(catalog.GetSlot(4).IsUsable, Is.False);
        Assert.That(catalog.TryLoad(4, out _), Is.False);
    }

    [Test]
    public void SaveFromAnotherChapter_IsRefused()
    {
        StoryDirector story = new StoryDirector(new PrologueStoryScript());
        StoryProgress foreign = new StoryProgress(
            "chapter-nine", StoryBeat.Opening, 0, 0, 0, false, 0f);

        Assert.That(story.CanRestore(foreign), Is.False);
        Assert.That(story.Restore(foreign), Is.False);
        Assert.That(story.IsRunning, Is.False);
    }

    [Test]
    public void SaveTakenDuringABattle_ReturnsToTheChallengeLine()
    {
        StoryDirector story = RunningStory();
        story.Confirm();
        story.Confirm();
        story.Confirm();
        story.Confirm();
        Assert.That(story.Beat, Is.EqualTo(StoryBeat.Challenge));

        story.EnterBattle();
        Assert.That(story.Beat, Is.EqualTo(StoryBeat.None));

        Assert.That(story.Capture().Beat, Is.EqualTo(StoryBeat.Challenge));
    }

    [Test]
    public void RestoringAnOverlongLineIndex_ClampsToAuthoredContent()
    {
        StoryDirector story = new StoryDirector(new PrologueStoryScript());
        StoryProgress progress = new StoryProgress(
            "prologue-moon-gate", StoryBeat.Opening, 999, 5, 9, false, -20f);

        Assert.That(story.Restore(progress), Is.True);
        Assert.That(story.CurrentLine.Text, Is.Not.Empty);
        Assert.That(story.Response, Is.EqualTo(1));
        Assert.That(story.Selection, Is.EqualTo(1));
        Assert.That(story.PlaytimeSeconds, Is.EqualTo(0f));
    }

    [Test]
    public void ChapterPlaytime_OnlyAccruesWhileRunning()
    {
        StoryDirector story = new StoryDirector(new PrologueStoryScript());

        story.AddPlaytime(30f);
        Assert.That(story.PlaytimeSeconds, Is.EqualTo(0f));

        story.Begin();
        story.AddPlaytime(30f);
        Assert.That(story.PlaytimeSeconds, Is.EqualTo(30f).Within(0.01f));

        story.Cancel();
        Assert.That(story.PlaytimeSeconds, Is.EqualTo(0f));
    }

    [Test]
    public void FormatPlaytime_PadsHoursMinutesAndSeconds()
    {
        Assert.That(SaveSlotCatalog.FormatPlaytime(0f), Is.EqualTo("00:00:00"));
        Assert.That(SaveSlotCatalog.FormatPlaytime(59.9f), Is.EqualTo("00:00:59"));
        Assert.That(SaveSlotCatalog.FormatPlaytime(-5f), Is.EqualTo("00:00:00"));
        Assert.That(SaveSlotCatalog.FormatPlaytime(36061f), Is.EqualTo("10:01:01"));
    }
}
