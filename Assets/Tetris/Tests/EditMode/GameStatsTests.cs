using NUnit.Framework;
using UnityEngine.TestTools;

public class GameStatsTests
{
    private const int Lyra = 0;
    private const int Bram = 1;

    [Test]
    public void VersusResults_LandOnPlayerOnesCharacterAndTheMode()
    {
        GameStats stats = new GameStats(new MemoryJsonStore());

        stats.RecordMatch(TetrisGameMode.VersusCpu, Lyra, true, false);
        stats.RecordMatch(TetrisGameMode.VersusCpu, Lyra, false, false);
        stats.RecordMatch(TetrisGameMode.LocalVersus, Bram, true, false);

        Assert.That(stats.Data.matchesPlayed, Is.EqualTo(3));

        CharacterStatsEntry lyra = stats.GetCharacter(Lyra);
        Assert.That(lyra.wins, Is.EqualTo(1));
        Assert.That(lyra.losses, Is.EqualTo(1));
        Assert.That(lyra.Played, Is.EqualTo(2));
        Assert.That(lyra.WinRate, Is.EqualTo(0.5f).Within(0.001f));

        CharacterStatsEntry bram = stats.GetCharacter(Bram);
        Assert.That(bram.wins, Is.EqualTo(1));
        Assert.That(bram.losses, Is.EqualTo(0));

        ModeStatsEntry versusCpu = stats.GetMode(TetrisGameMode.VersusCpu);
        Assert.That(versusCpu.played, Is.EqualTo(2));
        Assert.That(versusCpu.wins, Is.EqualTo(1));
        Assert.That(versusCpu.losses, Is.EqualTo(1));
    }

    [Test]
    public void SoloRuns_CountAsPlayedWithoutAWinOrALoss()
    {
        GameStats stats = new GameStats(new MemoryJsonStore());

        stats.RecordMatch(TetrisGameMode.Marathon, Lyra, false, false);
        stats.RecordMatch(TetrisGameMode.Sprint, Lyra, false, false);

        foreach (TetrisGameMode mode in new[] { TetrisGameMode.Marathon, TetrisGameMode.Sprint })
        {
            ModeStatsEntry solo = stats.GetMode(mode);
            Assert.That(solo.played, Is.EqualTo(1));
            Assert.That(solo.wins, Is.EqualTo(0));
            Assert.That(solo.losses, Is.EqualTo(0));
        }

        Assert.That(stats.Data.matchesPlayed, Is.EqualTo(2));
        Assert.That(stats.GetCharacter(Lyra).Played, Is.EqualTo(0));
    }

    [Test]
    public void StoryBattles_AreCountedSeparately()
    {
        GameStats stats = new GameStats(new MemoryJsonStore());

        stats.RecordMatch(TetrisGameMode.VersusCpu, Lyra, false, true);
        stats.RecordMatch(TetrisGameMode.VersusCpu, Lyra, true, true);
        stats.RecordMatch(TetrisGameMode.VersusCpu, Lyra, true, false);

        Assert.That(stats.Data.storyBattlesWon, Is.EqualTo(1));
        Assert.That(stats.Data.storyBattlesLost, Is.EqualTo(1));
        Assert.That(stats.GetCharacter(Lyra).Played, Is.EqualTo(3));
    }

    [Test]
    public void TheStoryClock_StopsWhileTheTotalKeepsRunning()
    {
        GameStats stats = new GameStats(new MemoryJsonStore());

        stats.AddPlaytime(10f, true);
        stats.AddPlaytime(5f, false);

        Assert.That(stats.Data.totalPlaytimeSeconds, Is.EqualTo(15f).Within(0.01f));
        Assert.That(stats.Data.storyPlaytimeSeconds, Is.EqualTo(10f).Within(0.01f));
    }

    [Test]
    public void Counters_SurviveIntoTheNextSession()
    {
        MemoryJsonStore store = new MemoryJsonStore();

        GameStats first = new GameStats(store);
        first.BeginSession();
        first.AddPlaytime(42f, true);
        first.RecordMatch(TetrisGameMode.VersusCpu, Bram, true, false);
        first.RecordStorySave();
        first.Flush();

        GameStats second = new GameStats(store);
        second.BeginSession();

        Assert.That(second.Data.sessionsStarted, Is.EqualTo(2));
        Assert.That(second.Data.matchesPlayed, Is.EqualTo(1));
        Assert.That(second.Data.storySavesWritten, Is.EqualTo(1));
        Assert.That(second.Data.totalPlaytimeSeconds, Is.EqualTo(42f).Within(0.01f));
        Assert.That(second.GetCharacter(Bram).wins, Is.EqualTo(1));
    }

    [Test]
    public void AnUnreadableStatsDocument_StartsFromZeroInsteadOfThrowing()
    {
        // Parsing a damaged document reports the failure before recovering.
        LogAssert.ignoreFailingMessages = true;

        MemoryJsonStore store = new MemoryJsonStore();
        store.Write(GameStats.StoreKey, "{ not json at all");

        GameStats stats = new GameStats(store);

        Assert.That(stats.Data.matchesPlayed, Is.EqualTo(0));
        Assert.That(stats.Data.totalPlaytimeSeconds, Is.EqualTo(0f));
    }
}
