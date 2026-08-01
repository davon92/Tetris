using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Owns the lifetime of a single battle: the player sessions, the CPU, the
/// shared piece queue, the garbage wiring, and the ready/start/result beats.
/// Knows nothing about menus, story flow, input devices, or how any of it is
/// drawn — it only reports state and raises <see cref="MatchEnded"/>.
/// </summary>
public sealed class MatchDirector
{
    private const int PlayerOneSeed = 101;
    private const int PlayerTwoSeed = 202;
    private const int SharedQueueSeed = 303;
    private const int CpuSeed = 2026;

    private static readonly Vector3Int SoloOrigin = new Vector3Int(-5, -10, 0);
    private static readonly Vector3Int VersusLeftOrigin = new Vector3Int(-12, -10, 0);
    private static readonly Vector3Int VersusRightOrigin = new Vector3Int(2, -10, 0);

    private readonly Transform sessionParent;
    private readonly Grid battleGrid;
    private readonly TetriminoPiece[] piecePrefabs;
    private readonly BattleEffectsController effects;
    private readonly MatchSettings settings;

    private TetrisGameSession playerOne;
    private TetrisGameSession playerTwo;
    private SimpleTetrisCpu cpu;
    private float phaseTimer;

    public MatchDirector(
        Transform sessionParent,
        Grid battleGrid,
        TetriminoPiece[] piecePrefabs,
        BattleEffectsController effects,
        MatchSettings settings)
    {
        this.sessionParent = sessionParent;
        this.battleGrid = battleGrid;
        this.piecePrefabs = piecePrefabs ?? Array.Empty<TetriminoPiece>();
        this.effects = effects;
        this.settings = settings;
    }

    /// <summary>Raised once the result beat finishes. True when player one won.</summary>
    public event Action<bool> MatchEnded;

    public TetrisGameSession PlayerOne => playerOne;
    public TetrisGameSession PlayerTwo => playerTwo;
    public SharedPieceQueue SharedQueue { get; private set; }

    public MatchPhase Phase { get; private set; } = MatchPhase.Idle;
    public TetrisGameMode Mode { get; private set; }
    public CpuDifficulty Difficulty { get; private set; }
    public int PlayerOneCharacter { get; private set; }
    public int PlayerTwoCharacter { get; private set; }
    public bool IsStoryBattle { get; private set; }
    public string EncounterTitle { get; private set; } = string.Empty;

    public bool HasOutcome { get; private set; }
    public bool PlayerOneWon { get; private set; }
    public string WinnerName { get; private set; } = string.Empty;
    public string LoserName { get; private set; } = string.Empty;

    public bool IsAcceptingInput => Phase == MatchPhase.Playing;

    public void Begin(in MatchSetup setup)
    {
        Clear();

        Mode = setup.Mode;
        Difficulty = setup.Difficulty;
        IsStoryBattle = setup.IsStoryBattle;
        PlayerOneCharacter = setup.PlayerOneCharacter;
        PlayerTwoCharacter = setup.PlayerTwoCharacter;
        EncounterTitle = setup.EncounterTitle;

        SharedQueue = setup.Mode == TetrisGameMode.Solo
            ? null
            : new SharedPieceQueue(SharedQueueSeed, settings.SharedPieceCloseClaimWindow);

        if (setup.Mode == TetrisGameMode.Solo)
        {
            playerOne = CreateSession("PLAYER", SoloOrigin, PlayerOneSeed);
        }
        else
        {
            playerOne = CreateSession(
                BattleCharacterRoster.Get(setup.PlayerOneCharacter).DisplayName,
                VersusLeftOrigin,
                PlayerOneSeed);
            playerTwo = CreateSession(
                BattleCharacterRoster.Get(setup.PlayerTwoCharacter).DisplayName,
                VersusRightOrigin,
                PlayerTwoSeed);

            playerOne.AttackGenerated += (_, lines) => playerTwo?.QueueGarbage(lines);
            playerTwo.AttackGenerated += (_, lines) => playerOne?.QueueGarbage(lines);

            if (setup.Mode == TetrisGameMode.VersusCpu)
                cpu = new SimpleTetrisCpu(playerTwo, setup.Difficulty, CpuSeed);
        }

        playerOne.GameOver += OnSessionGameOver;
        if (playerTwo != null)
            playerTwo.GameOver += OnSessionGameOver;

        if (effects != null)
            effects.Initialize(playerOne, playerTwo, piecePrefabs);

        Phase = MatchPhase.Ready;
        phaseTimer = settings.ReadyDuration;
    }

    public void Clear()
    {
        if (effects != null)
            effects.ClearBattle();

        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        SharedQueue = null;

        Phase = MatchPhase.Idle;
        phaseTimer = 0f;
        EncounterTitle = string.Empty;
        HasOutcome = false;
        PlayerOneWon = false;
        WinnerName = string.Empty;
        LoserName = string.Empty;
    }

    /// <summary>
    /// Advances the ready/start/result countdown.
    /// </summary>
    /// <returns>True when the presentation owns the frame and gameplay must not run.</returns>
    public bool AdvancePhase(float unscaledDeltaTime)
    {
        if (Phase == MatchPhase.Playing)
            return false;

        if (Phase == MatchPhase.Idle || Phase == MatchPhase.Finished)
            return true;

        phaseTimer -= unscaledDeltaTime;
        if (phaseTimer > 0f)
            return true;

        phaseTimer = 0f;
        switch (Phase)
        {
            case MatchPhase.Ready:
                Phase = MatchPhase.Start;
                phaseTimer = settings.StartDuration;
                break;
            case MatchPhase.Start:
                Phase = MatchPhase.Playing;
                break;
            default:
                // Phase is flipped before the event so a listener that starts a
                // new match from the callback is not overwritten afterwards.
                Phase = MatchPhase.Finished;
                MatchEnded?.Invoke(PlayerOneWon);
                break;
        }

        return true;
    }

    /// <summary>Runs the CPU and both boards for one gameplay frame.</summary>
    public void Tick(float deltaTime)
    {
        if (Phase != MatchPhase.Playing)
            return;

        if (Mode != TetrisGameMode.LocalVersus)
            cpu?.Tick(deltaTime);

        // A hard drop can top a board out and tear the match down before the
        // rest of this frame runs.
        if (playerOne == null)
            return;

        playerOne.Tick(deltaTime);
        if (playerTwo != null)
            playerTwo.Tick(deltaTime);
    }

    private TetrisGameSession CreateSession(string displayName, Vector3Int gridOrigin, int seed)
    {
        GameObject sessionObject = new GameObject($"{displayName} Board");
        sessionObject.transform.SetParent(
            battleGrid != null ? battleGrid.transform : sessionParent,
            false);

        TetrisGameSession session = sessionObject.AddComponent<TetrisGameSession>();
        session.Initialize(displayName, battleGrid, gridOrigin, seed, piecePrefabs, SharedQueue);
        return session;
    }

    private void OnSessionGameOver(TetrisGameSession loser)
    {
        if (Phase == MatchPhase.Result || Phase == MatchPhase.Finished)
            return;

        TetrisGameSession winner = loser == playerOne ? playerTwo : playerOne;

        HasOutcome = true;
        PlayerOneWon = playerTwo != null && loser == playerTwo;
        WinnerName = winner != null ? winner.DisplayName : "OPPONENT";
        LoserName = loser != null ? loser.DisplayName : "PLAYER";

        playerOne?.Stop();
        playerTwo?.Stop();

        Phase = MatchPhase.Result;
        phaseTimer = settings.ResultDuration;
    }

    private static void DestroySession(ref TetrisGameSession session)
    {
        if (session != null)
            Object.Destroy(session.gameObject);

        session = null;
    }
}
