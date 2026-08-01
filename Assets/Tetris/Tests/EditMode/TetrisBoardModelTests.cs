using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class TetrisBoardModelTests
{
    [Test]
    public void I_Piece_RespectsLeftBoundary()
    {
        TetrisBoardModel board = new TetrisBoardModel();

        Assert.That(board.IsValid(TetriminoType.I, new Vector2Int(0, 5), 0), Is.False);
        Assert.That(board.IsValid(TetriminoType.I, new Vector2Int(1, 5), 0), Is.True);
    }

    [Test]
    public void Two_O_Pieces_ClearTwoRows_OnFourWideBoard()
    {
        TetrisBoardModel board = new TetrisBoardModel(4, 8, 4);

        Assert.That(board.Place(TetriminoType.O, new Vector2Int(0, 0), 0), Is.EqualTo(0));
        Assert.That(board.Place(TetriminoType.O, new Vector2Int(2, 0), 0), Is.EqualTo(2));
        CollectionAssert.AreEqual(new[] { 0, 1 }, board.LastClearedRows);

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
                Assert.That(board.GetCell(x, y), Is.EqualTo(0));
        }
    }

    [Test]
    public void Garbage_HasOneHolePerRow()
    {
        TetrisBoardModel board = new TetrisBoardModel();

        bool overflowed = board.AddGarbage(3, _ => 4);

        Assert.That(overflowed, Is.False);
        for (int y = 0; y < 3; y++)
        {
            Assert.That(board.GetCell(4, y), Is.EqualTo(0));
            Assert.That(
                Enumerable.Range(0, board.Width).Count(x => board.GetCell(x, y) != 0),
                Is.EqualTo(board.Width - 1));
        }
    }

    [Test]
    public void SevenBag_ContainsEveryPieceBeforeRepeating()
    {
        SevenBagRandomizer randomizer = new SevenBagRandomizer(1234);
        TetriminoType[] firstBag = Enumerable.Range(0, 7).Select(_ => randomizer.Next()).ToArray();

        Assert.That(firstBag.Distinct().Count(), Is.EqualTo(7));
    }

    [Test]
    public void SharedQueue_CloseClaimsCanReceiveTheSamePiece()
    {
        SharedPieceQueue queue = new SharedPieceQueue(1234, 0.1f);
        object playerOne = new object();
        object playerTwo = new object();
        TetriminoType contestedPiece = queue.NextType;

        TetriminoType firstClaim = queue.Claim(playerOne, 1f);
        TetriminoType followingPiece = queue.NextType;
        TetriminoType closeClaim = queue.Claim(playerTwo, 1.08f);

        Assert.That(firstClaim, Is.EqualTo(contestedPiece));
        Assert.That(closeClaim, Is.EqualTo(contestedPiece));
        Assert.That(queue.NextType, Is.EqualTo(followingPiece));
    }

    [Test]
    public void SharedQueue_LateClaimTakesTheFollowingPiece()
    {
        SharedPieceQueue queue = new SharedPieceQueue(1234, 0.1f);
        object playerOne = new object();
        object playerTwo = new object();

        queue.Claim(playerOne, 1f);
        TetriminoType followingPiece = queue.NextType;
        TetriminoType lateClaim = queue.Claim(playerTwo, 1.11f);

        Assert.That(lateClaim, Is.EqualTo(followingPiece));
        Assert.That(queue.NextType, Is.Not.EqualTo(followingPiece));
    }

    [Test]
    public void SharedQueue_MirroredPlayerCannotClaimTheSamePieceTwice()
    {
        SharedPieceQueue queue = new SharedPieceQueue(1234, 0.1f);
        object playerOne = new object();
        object playerTwo = new object();

        TetriminoType contestedPiece = queue.Claim(playerOne, 1f);
        queue.Claim(playerTwo, 1.05f);
        TetriminoType secondClaim = queue.Claim(playerTwo, 1.06f);

        Assert.That(secondClaim, Is.Not.EqualTo(contestedPiece));
    }

    [Test]
    public void I_Piece_UsesItsFourDistinctSrsStates()
    {
        CollectionAssert.AreEquivalent(
            new[]
            {
                new Vector2Int(1, 1),
                new Vector2Int(1, 0),
                new Vector2Int(1, -1),
                new Vector2Int(1, -2)
            },
            TetrominoDefinitions.GetCells(TetriminoType.I, 1));

        CollectionAssert.AreEquivalent(
            new[]
            {
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
                new Vector2Int(2, -1)
            },
            TetrominoDefinitions.GetCells(TetriminoType.I, 2));
    }

    [Test]
    public void I_Piece_UsesTheSrsZeroToRightKickOrder()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(-2, 0),
                new Vector2Int(1, 0),
                new Vector2Int(-2, -1),
                new Vector2Int(1, 2)
            },
            SrsRotationSystem.GetKickTests(TetriminoType.I, 0, 1));
    }

    [Test]
    public void T_Piece_UsesTheSrsLeftToZeroKickOrder()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, -1),
                new Vector2Int(0, 2),
                new Vector2Int(-1, 2)
            },
            SrsRotationSystem.GetKickTests(TetriminoType.T, 3, 0));
    }

    [Test]
    public void CpuDifficulty_TradesSpeedAndAccuracyForApproachability()
    {
        CpuDifficultySettings easy = SimpleTetrisCpu.GetSettings(CpuDifficulty.Easy);
        CpuDifficultySettings normal = SimpleTetrisCpu.GetSettings(CpuDifficulty.Normal);
        CpuDifficultySettings hard = SimpleTetrisCpu.GetSettings(CpuDifficulty.Hard);

        Assert.That(easy.ActionInterval, Is.GreaterThan(normal.ActionInterval));
        Assert.That(normal.ActionInterval, Is.GreaterThan(hard.ActionInterval));
        Assert.That(easy.ThinkDelay, Is.GreaterThan(normal.ThinkDelay));
        Assert.That(easy.DropDelay, Is.GreaterThan(normal.DropDelay));
        Assert.That(easy.PlacementChoices, Is.GreaterThan(normal.PlacementChoices));
        Assert.That(normal.PlacementChoices, Is.GreaterThan(hard.PlacementChoices));
    }

    [Test]
    public void CharacterRoster_HasTwoStartersAndLockedExpansionSlots()
    {
        Assert.That(BattleCharacterRoster.Count, Is.EqualTo(6));
        Assert.That(BattleCharacterRoster.Get(0).DisplayName, Is.EqualTo("LYRA"));
        Assert.That(BattleCharacterRoster.Get(1).DisplayName, Is.EqualTo("BRAM"));
        Assert.That(BattleCharacterRoster.Get(0).UnlockedByDefault, Is.True);
        Assert.That(BattleCharacterRoster.Get(1).UnlockedByDefault, Is.True);
        Assert.That(BattleCharacterRoster.Get(2).UnlockedByDefault, Is.False);
        Assert.That(BattleCharacterRoster.FindIndex("bram"), Is.EqualTo(1));
    }
}
