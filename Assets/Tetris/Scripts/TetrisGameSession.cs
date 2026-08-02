using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TetrisGameSession : MonoBehaviour
{
    private readonly struct CellStyle
    {
        public CellStyle(Sprite sprite, Material material, Color color, Vector3 scale)
        {
            Sprite = sprite;
            Material = material;
            Color = color;
            Scale = scale;
        }

        public Sprite Sprite { get; }
        public Material Material { get; }
        public Color Color { get; }
        public Vector3 Scale { get; }
    }

    private static Sprite sharedCellSprite;

    private readonly Dictionary<TetriminoType, TetriminoPiece> piecePrefabLookup = new();
    private readonly Dictionary<TetriminoType, CellStyle> cellStyleLookup = new();

    private const int ManaPieceInterval = 6;
    private const int ManaSpawnPercent = 45;

    /// <summary>Pool size for a fighter who does not override it.</summary>
    private const int DefaultManaCapacity = 100;

    /// <summary>Charge for each gold cell a clear takes with it, on top of the line payout.</summary>
    private const int ManaPerManaCell = 25;

    private static readonly Color ManaColor = new Color(1f, 0.84f, 0.25f);

    private SevenBagRandomizer randomizer;
    private SharedPieceQueue sharedPieceQueue;
    private System.Random garbageRandom;
    private System.Random manaRandom;
    private int piecesSinceMana;
    private Grid battleGrid;
    private Transform lockedRoot;
    private Transform activeRoot;
    private Transform ghostRoot;
    private TetriminoPiece activePieceView;
    private TetriminoPiece ghostPieceView;
    private float fallTimer;
    private float lockTimer;
    private int combo = -1;
    private bool holdUsed;
    private bool initialized;
    private bool running;
    private TetriminoType nextType;

    public event Action<TetrisGameSession, int> AttackGenerated;
    public event Action<TetrisGameSession> GameOver;
    public event Action<TetrisGameSession, TetriminoType> PieceSpawned;
    public event Action<TetrisGameSession, Vector2Int[]> PieceLocked;
    public event Action<TetrisGameSession, int[]> LinesCleared;
    public event Action<TetrisGameSession, int> GarbageApplied;
    public event Action<TetrisGameSession, int> GarbageCancelled;

    /// <summary>This player spent mana on a spell — the match routes it at a target.</summary>
    public event Action<TetrisGameSession, MagicAbilityDefinition> AbilityCast;

    /// <summary>A spell resolved on this board. Carries the cells it destroyed.</summary>
    public event Action<TetrisGameSession, MagicAbilityDefinition, Vector2Int[]> AbilityResolved;

    public string DisplayName { get; private set; }
    public TetrisBoardModel Model { get; private set; }
    public TetriminoType ActiveType { get; private set; }
    public Vector2Int ActivePosition { get; private set; }
    public int ActiveRotation { get; private set; }
    public TetriminoType? HeldType { get; private set; }
    public int PieceSerial { get; private set; }
    public int PendingGarbage { get; private set; }
    public int Score { get; private set; }
    public int Lines { get; private set; }
    public int Level { get; private set; } = 1;
    public TetriminoType NextType => sharedPieceQueue?.NextType ?? nextType;
    public bool IsRunning => running;
    public bool IsGameOver => initialized && !running;

    /// <summary>True while hold has been spent for the current drop.</summary>
    public bool IsHoldLocked => holdUsed;

    /// <summary>The spell aimed at the opponent, from this player's character.</summary>
    public MagicAbilityDefinition OffensiveAbility { get; set; }

    /// <summary>The spell aimed at this player's own board.</summary>
    public MagicAbilityDefinition DefensiveAbility { get; set; }

    /// <summary>Index into the active piece's cells that is a mana cell, or -1.</summary>
    public int ActiveManaCell { get; private set; } = -1;

    /// <summary>Charge banked toward the next cast, 0..<see cref="ManaCapacity"/>.</summary>
    public int Mana { get; private set; }

    /// <summary>
    /// Size of the mana pool, from the character. Costs are absolute, so a
    /// smaller pool is what makes a fighter cast more often.
    /// </summary>
    public int ManaCapacity { get; set; } = DefaultManaCapacity;

    /// <summary>Mana as 0..1, for the bar.</summary>
    public float ManaCharge => Mathf.Clamp01(Mana / (float)Mathf.Max(1, ManaCapacity));

    /// <summary>
    /// False in solo. An offensive spell with no opposing board has nowhere to
    /// land, so the session refuses the cast instead of spending mana on it.
    /// </summary>
    public bool HasOpponent { get; set; } = true;

    /// <summary>Enough banked for at least one spell this board could actually cast.</summary>
    public bool IsSpellReady =>
        CanAfford(OffensiveAbility) || CanAfford(DefensiveAbility);

    /// <summary>Armed and still alive, so a cast key will actually fire.</summary>
    public bool CanCastAbility => running && IsSpellReady;

    /// <summary>Where a spell's cost sits on the bar, 0..1. 0 when unequipped.</summary>
    public float CostFraction(MagicAbilityDefinition ability)
    {
        return ability == null
            ? 0f
            : Mathf.Clamp01(ability.ManaCost / (float)Mathf.Max(1, ManaCapacity));
    }

    /// <summary>Equipped, aimable at something, and paid for.</summary>
    public bool CanAfford(MagicAbilityDefinition ability)
    {
        return IsCastable(ability) && Mana >= ability.ManaCost;
    }

    private bool IsCastable(MagicAbilityDefinition ability)
    {
        return ability != null &&
               (ability.Slot == MagicAbilitySlot.Defensive || HasOpponent);
    }

    /// <summary>Upcoming pieces, nearest first. Reads the shared queue in versus.</summary>
    public TetriminoType[] PeekUpcoming(int count)
    {
        if (sharedPieceQueue != null)
            return sharedPieceQueue.PeekUpcoming(count);

        if (count <= 0)
            return Array.Empty<TetriminoType>();

        TetriminoType[] upcoming = new TetriminoType[count];
        upcoming[0] = nextType;
        if (count > 1)
        {
            TetriminoType[] fromBag = randomizer.Peek(count - 1);
            Array.Copy(fromBag, 0, upcoming, 1, count - 1);
        }

        return upcoming;
    }

    public Vector3 GetCellWorldPosition(Vector2Int cell)
    {
        return transform.TransformPoint(CellPosition(cell.x, cell.y));
    }

    public Vector3 GetBoardWorldPosition(float x, float y)
    {
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }

    public void Initialize(
        string displayName,
        Grid grid,
        Vector3Int gridOrigin,
        int seed,
        TetriminoPiece[] piecePrefabs,
        SharedPieceQueue sharedPieceQueue = null)
    {
        DisplayName = displayName;
        battleGrid = grid;
        this.sharedPieceQueue = sharedPieceQueue;
        transform.localPosition = battleGrid != null
            ? battleGrid.CellToLocal(gridOrigin)
            : (Vector3)gridOrigin;
        Model = new TetrisBoardModel();
        randomizer = new SevenBagRandomizer(seed);
        garbageRandom = new System.Random(seed ^ 0x5f3759df);
        manaRandom = new System.Random(seed ^ 0x2545f491);
        piecesSinceMana = 0;
        nextType = sharedPieceQueue?.NextType ?? randomizer.Next();

        CachePiecePrefabs(piecePrefabs);
        CreateBoardView();
        initialized = true;
        running = true;
        SpawnNextPiece(true);
    }

    public void Tick(float deltaTime)
    {
        if (!running)
            return;

        fallTimer += deltaTime;
        float gravityInterval = Mathf.Max(0.075f, 0.82f - (Level - 1) * 0.055f);
        if (fallTimer >= gravityInterval)
        {
            fallTimer -= gravityInterval;
            TryMove(Vector2Int.down, false);
        }

        if (Model.IsValid(ActiveType, ActivePosition + Vector2Int.down, ActiveRotation))
        {
            lockTimer = 0f;
        }
        else
        {
            lockTimer += deltaTime;
            if (lockTimer >= 0.45f)
                LockActivePiece();
        }
    }

    public bool ApplyCommand(TetrisCommand command)
    {
        if (!running)
            return false;

        return command switch
        {
            TetrisCommand.MoveLeft => TryMove(Vector2Int.left, true),
            TetrisCommand.MoveRight => TryMove(Vector2Int.right, true),
            TetrisCommand.SoftDrop => SoftDrop(),
            TetrisCommand.HardDrop => HardDrop(),
            TetrisCommand.RotateClockwise => TryRotate(1),
            TetrisCommand.RotateCounterClockwise => TryRotate(-1),
            TetrisCommand.Hold => Hold(),
            TetrisCommand.CastOffensive => TryCastAbility(OffensiveAbility),
            TetrisCommand.CastDefensive => TryCastAbility(DefensiveAbility),
            _ => false
        };
    }

    /// <summary>
    /// Spends mana and casts. Deliberately manual: banked mana is a threat the
    /// player chooses when to spend, not something that fires the instant it is
    /// affordable. A press the bar cannot pay for just fizzles.
    /// </summary>
    public bool TryCastAbility(MagicAbilityDefinition ability)
    {
        if (!running)
            return false;

        if (!CanAfford(ability))
        {
            GameAudio.Play(GameSfx.ManaFizzle);
            return false;
        }

        Mana -= ability.ManaCost;
        AbilityCast?.Invoke(this, ability);
        return true;
    }

    /// <summary>
    /// Banks charge from a clear. The chime on the point that first makes a
    /// spell affordable is the only cue the player needs to look down.
    /// </summary>
    private void GainMana(int amount)
    {
        if (amount <= 0 || Mana >= ManaCapacity)
            return;

        bool wasReady = IsSpellReady;
        Mana = Mathf.Min(ManaCapacity, Mana + amount);
        if (IsSpellReady && !wasReady)
            GameAudio.Play(GameSfx.ManaCharged);
    }

    /// <summary>
    /// The opening handicap: buries this board under garbage before the bell.
    /// Health is read off board fill, so this is exactly a starting health
    /// penalty — and a recoverable one, since clearing the rows wins it back.
    /// </summary>
    public void ApplyStartingGarbage(int rows)
    {
        if (rows <= 0 || !running)
            return;

        int hole = garbageRandom.Next(Model.Width);
        Model.AddGarbage(rows, line => line % 3 == 0 ? garbageRandom.Next(Model.Width) : hole);
        RefreshLockedView();
    }

    /// <summary>
    /// Charge for a clear alone. Steep enough that a tetris is worth building
    /// for — three quarters of a bar against a single line's eighth.
    /// </summary>
    private static int ManaForLines(int cleared)
    {
        return cleared switch
        {
            1 => 12,
            2 => 28,
            3 => 48,
            4 => 75,
            _ => 0
        };
    }

    public void QueueGarbage(int lineCount)
    {
        if (running && lineCount > 0)
            PendingGarbage = Mathf.Clamp(PendingGarbage + lineCount, 0, 20);
    }

    /// <summary>
    /// Resolves a spell against this board immediately. Offensive spells carve
    /// the stack without settling it, so the wells and overhangs they leave are
    /// the actual damage; Heal mends the caster's own garbage instead.
    /// </summary>
    public void ReceiveAbility(MagicAbilityDefinition ability)
    {
        if (!running || ability == null)
            return;

        List<Vector2Int> affected;
        switch (ability.Effect)
        {
            case MagicEffect.CarveColumns:
            {
                int columns = Mathf.Min(ability.ColumnCount, Model.Width);
                int start = Mathf.Clamp(
                    Model.FindTallestColumn() - columns / 2, 0, Model.Width - columns);
                affected = Model.CarveColumns(start, columns);
                break;
            }

            case MagicEffect.Crater:
            {
                // Clamped so the full crater lands on the board, and sunk below
                // the surface so the stack above it survives as an overhang —
                // the sealed holes are the real damage.
                int half = Mathf.Max(1, ability.CraterWidth / 2);
                int centerX = Mathf.Clamp(Model.FindTallestColumn(), half, Model.Width - half);
                int centerY = Mathf.Max(1, Model.GetColumnHeight(centerX) - ability.CraterDepth);
                affected = Model.CarveCrater(
                    centerX, centerY, ability.CraterWidth, ability.CraterHeight);
                break;
            }

            default:
            {
                int cancelled = Mathf.Min(PendingGarbage, ability.GarbageCancelled);
                PendingGarbage -= cancelled;
                if (cancelled > 0)
                    GarbageCancelled?.Invoke(this, cancelled);

                Model.MendGarbageRows(ability.MendRows);
                affected = new List<Vector2Int>();
                break;
            }
        }

        RefreshLockedView();
        AbilityResolved?.Invoke(this, ability, affected.ToArray());
    }

    public void Stop()
    {
        running = false;
    }

    private bool TryMove(Vector2Int direction, bool resetLockDelay)
    {
        Vector2Int next = ActivePosition + direction;
        if (!Model.IsValid(ActiveType, next, ActiveRotation))
            return false;

        ActivePosition = next;
        if (resetLockDelay)
            lockTimer = 0f;

        // Only player-driven sideways nudges click; gravity steps stay silent.
        if (resetLockDelay && direction.y == 0)
            GameAudio.Play(GameSfx.PieceMove);

        RefreshActiveView();
        return true;
    }

    private bool SoftDrop()
    {
        if (!TryMove(Vector2Int.down, true))
            return false;

        Score++;
        fallTimer = 0f;
        return true;
    }

    /// <summary>
    /// Sonic-drop semantics: the piece slams to the stack but keeps the normal
    /// lock delay, so there is still a beat to slide or rotate it — and a
    /// second press while already grounded commits it instantly.
    /// </summary>
    private bool HardDrop()
    {
        int distance = 0;
        while (Model.IsValid(ActiveType, ActivePosition + Vector2Int.down, ActiveRotation))
        {
            ActivePosition += Vector2Int.down;
            distance++;
        }

        if (distance == 0)
        {
            RefreshActiveView();
            LockActivePiece();
            return true;
        }

        Score += distance * 2;
        fallTimer = 0f;
        lockTimer = 0f;
        GameAudio.Play(GameSfx.HardDrop);
        RefreshActiveView();
        return true;
    }

    private bool TryRotate(int direction)
    {
        int nextRotation = TetrominoDefinitions.NormalizeRotation(ActiveRotation + direction);
        foreach (Vector2Int kick in SrsRotationSystem.GetKickTests(
                     ActiveType,
                     ActiveRotation,
                     nextRotation))
        {
            Vector2Int nextPosition = ActivePosition + kick;
            if (!Model.IsValid(ActiveType, nextPosition, nextRotation))
                continue;

            ActiveRotation = nextRotation;
            ActivePosition = nextPosition;
            lockTimer = 0f;
            GameAudio.Play(GameSfx.PieceRotate);
            RefreshActiveView();
            return true;
        }

        return false;
    }

    private bool Hold()
    {
        if (holdUsed)
            return false;

        TetriminoType outgoing = ActiveType;
        if (HeldType.HasValue)
        {
            TetriminoType incoming = HeldType.Value;
            HeldType = outgoing;
            SpawnPiece(incoming, false);
        }
        else
        {
            HeldType = outgoing;
            SpawnNextPiece(false);
        }

        holdUsed = true;
        GameAudio.Play(GameSfx.Hold);
        return running;
    }

    private void LockActivePiece()
    {
        if (!running)
            return;

        Vector2Int[] lockedCells = GetActiveBoardCells();
        int cleared = Model.Place(ActiveType, ActivePosition, ActiveRotation, ActiveManaCell);
        int manaCellsCleared = Model.LastClearManaCells;
        ActiveManaCell = -1;
        PieceLocked?.Invoke(this, lockedCells);
        // Duck the lock thud when a clear is about to sing over it.
        GameAudio.Play(GameSfx.PieceLock, cleared > 0 ? 0.45f : 1f);

        if (cleared >= 4)
            GameAudio.Play(GameSfx.Tetris);
        else if (cleared > 0)
            GameAudio.Play(GameSfx.LineClear);

        if (cleared > 0)
        {
            int[] clearedRows = new int[Model.LastClearedRows.Count];
            for (int i = 0; i < clearedRows.Length; i++)
                clearedRows[i] = Model.LastClearedRows[i];

            LinesCleared?.Invoke(this, clearedRows);
        }

        AwardLineClear(cleared);

        int attack = CalculateAttack(cleared);
        int cancelled = Mathf.Min(attack, PendingGarbage);
        attack -= cancelled;
        PendingGarbage -= cancelled;

        if (cancelled > 0)
        {
            GarbageCancelled?.Invoke(this, cancelled);
            GameAudio.Play(GameSfx.GarbageBlocked);
        }

        if (attack > 0)
            AttackGenerated?.Invoke(this, attack);

        // Clears charge the bar instead of casting outright. Bigger clears pay
        // far more, and a gold cell riding along pays more still — so it stays
        // worth both building for four and steering the mana block into it.
        GainMana(ManaForLines(cleared) + manaCellsCleared * ManaPerManaCell);

        bool garbageOverflow = false;
        if (PendingGarbage > 0)
        {
            int garbageToApply = PendingGarbage;
            PendingGarbage = 0;
            int sharedHole = garbageRandom.Next(Model.Width);
            garbageOverflow = Model.AddGarbage(
                garbageToApply,
                line => line % 3 == 0 ? garbageRandom.Next(Model.Width) : sharedHole);
            GarbageApplied?.Invoke(this, garbageToApply);
            GameAudio.Play(GameSfx.GarbageLand);
        }

        RefreshLockedView();

        if (garbageOverflow || Model.HasCellsAboveVisibleHeight())
        {
            EndGame();
            return;
        }

        SpawnNextPiece(true);
    }

    private void AwardLineClear(int cleared)
    {
        if (cleared > 0)
        {
            combo++;
            Lines += cleared;
            int previousLevel = Level;
            Level = 1 + Lines / 10;
            if (Level > previousLevel)
                GameAudio.Play(GameSfx.LevelUp);
        }
        else
        {
            combo = -1;
        }

        int baseScore = cleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0
        };

        Score += baseScore * Level;
        if (combo > 0)
            Score += combo * 50 * Level;
    }

    private int CalculateAttack(int cleared)
    {
        int attack = cleared switch
        {
            2 => 1,
            3 => 2,
            4 => 4,
            _ => 0
        };

        if (cleared > 0 && combo > 1)
            attack += Mathf.Min(4, combo - 1);

        return attack;
    }

    private void SpawnNextPiece(bool resetHold)
    {
        TetriminoType type;
        if (sharedPieceQueue != null)
        {
            type = sharedPieceQueue.Claim(this, Time.unscaledTime);
        }
        else
        {
            type = nextType;
            nextType = randomizer.Next();
        }

        SpawnPiece(type, resetHold);
    }

    private void SpawnPiece(TetriminoType type, bool resetHold)
    {
        ActiveType = type;
        ActiveRotation = 0;
        ActivePosition = new Vector2Int(Model.Width / 2 - 1, Model.VisibleHeight);
        fallTimer = 0f;
        lockTimer = 0f;
        PieceSerial++;

        // One cell of some pieces is a gold mana cell: clearing the row it
        // ends up in casts this player's spell, so it is worth steering.
        ActiveManaCell = piecesSinceMana >= ManaPieceInterval &&
                         manaRandom.Next(100) < ManaSpawnPercent
            ? manaRandom.Next(TetrominoDefinitions.GetCells(type, 0).Length)
            : -1;

        piecesSinceMana = ActiveManaCell >= 0 ? 0 : piecesSinceMana + 1;

        if (resetHold)
            holdUsed = false;

        if (!Model.IsValid(ActiveType, ActivePosition, ActiveRotation))
        {
            EndGame();
            return;
        }

        RefreshActiveView();
        PieceSpawned?.Invoke(this, ActiveType);
    }

    private Vector2Int[] GetActiveBoardCells()
    {
        Vector2Int[] offsets = TetrominoDefinitions.GetCells(ActiveType, ActiveRotation);
        Vector2Int[] cells = new Vector2Int[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
            cells[i] = ActivePosition + offsets[i];

        return cells;
    }

    private void EndGame()
    {
        running = false;
        SetPieceVisible(activePieceView, false);
        SetPieceVisible(ghostPieceView, false);
        GameOver?.Invoke(this);
    }

    private void CachePiecePrefabs(TetriminoPiece[] piecePrefabs)
    {
        piecePrefabLookup.Clear();
        cellStyleLookup.Clear();

        if (piecePrefabs == null)
            return;

        foreach (TetriminoPiece prefab in piecePrefabs)
        {
            if (prefab == null)
                continue;

            piecePrefabLookup[prefab.Type] = prefab;
            SpriteRenderer[] renderers = prefab.GetVisualRenderers();
            if (renderers.Length == 0)
                continue;

            SpriteRenderer source = renderers[0];
            cellStyleLookup[prefab.Type] = new CellStyle(
                source.sprite,
                source.sharedMaterial,
                source.color,
                source.transform.localScale);
        }

        if (piecePrefabLookup.Count != 7)
            Debug.LogError(
                $"{DisplayName} needs one prefab for every tetromino type; found {piecePrefabLookup.Count}.");
    }

    private void CreateBoardView()
    {
        SpriteRenderer border = CreateBasicRenderer("Board Backdrop", transform, -20);
        border.transform.localPosition = new Vector3(Model.Width * 0.5f, Model.VisibleHeight * 0.5f, 0f);
        border.transform.localScale = new Vector3(Model.Width + 0.5f, Model.VisibleHeight + 0.5f, 1f);
        border.color = new Color(0.09f, 0.12f, 0.2f, 1f);

        Transform gridRoot = new GameObject("Grid").transform;
        gridRoot.SetParent(transform, false);
        for (int y = 0; y < Model.VisibleHeight; y++)
        {
            for (int x = 0; x < Model.Width; x++)
            {
                SpriteRenderer gridCell = CreateBasicRenderer($"Grid {x},{y}", gridRoot, -10);
                gridCell.transform.localPosition = CellPosition(x, y);
                gridCell.transform.localScale = GetPixelAlignedCellScale();
                gridCell.color = new Color(0.055f, 0.07f, 0.115f, 1f);
            }
        }

        lockedRoot = new GameObject("Locked Cells").transform;
        lockedRoot.SetParent(transform, false);
        activeRoot = new GameObject("Active Piece").transform;
        activeRoot.SetParent(transform, false);
        ghostRoot = new GameObject("Ghost Piece").transform;
        ghostRoot.SetParent(transform, false);
    }

    private void RefreshActiveView()
    {
        EnsurePieceViews();
        if (activePieceView == null || ghostPieceView == null)
            return;

        Vector2Int[] pieceCells = TetrominoDefinitions.GetCells(ActiveType, ActiveRotation);
        int ghostY = Model.FindLandingY(ActiveType, ActivePosition.x, ActivePosition.y, ActiveRotation);

        activePieceView.SetState(ActivePosition, ActiveRotation);
        SetBlockVisibility(activePieceView, pieceCells, ActivePosition, true);
        ApplyManaTint(activePieceView, 1f);

        Vector2Int ghostPosition = new Vector2Int(ActivePosition.x, ghostY);
        ghostPieceView.SetState(ghostPosition, ActiveRotation);
        SetBlockVisibility(
            ghostPieceView,
            pieceCells,
            ghostPosition,
            ghostY != int.MinValue);
        ApplyManaTint(ghostPieceView, 0.22f);
    }

    private void EnsurePieceViews()
    {
        if (activePieceView != null && activePieceView.Type == ActiveType)
            return;

        if (activePieceView != null)
            Destroy(activePieceView.gameObject);
        if (ghostPieceView != null)
            Destroy(ghostPieceView.gameObject);

        if (!piecePrefabLookup.TryGetValue(ActiveType, out TetriminoPiece prefab))
        {
            Debug.LogError($"No prefab is assigned for {ActiveType}.");
            return;
        }

        activePieceView = Instantiate(prefab, activeRoot, false);
        activePieceView.name = $"{ActiveType} Active";
        activePieceView.ConfigureRuntime(ActiveType, 20, 1f, battleGrid);

        ghostPieceView = Instantiate(prefab, ghostRoot, false);
        ghostPieceView.name = $"{ActiveType} Ghost";
        ghostPieceView.ConfigureRuntime(ActiveType, 10, 0.22f, battleGrid);
    }

    private void RefreshLockedView()
    {
        for (int i = lockedRoot.childCount - 1; i >= 0; i--)
            Destroy(lockedRoot.GetChild(i).gameObject);

        for (int y = 0; y < Model.VisibleHeight; y++)
        {
            for (int x = 0; x < Model.Width; x++)
            {
                int cell = Model.GetCell(x, y);
                if (cell == 0)
                    continue;

                SpriteRenderer renderer = CreateLockedCellRenderer($"Cell {x},{y}", lockedRoot, cell);
                renderer.transform.localPosition = CellPosition(x, y);
            }
        }
    }

    private SpriteRenderer CreateLockedCellRenderer(string objectName, Transform parent, int cellValue)
    {
        TetriminoType type = cellValue >= 1 && cellValue <= 7
            ? (TetriminoType)(cellValue - 1)
            : TetriminoType.I;

        if (!cellStyleLookup.TryGetValue(type, out CellStyle style) && cellStyleLookup.Count > 0)
        {
            foreach (CellStyle availableStyle in cellStyleLookup.Values)
            {
                style = availableStyle;
                break;
            }
        }

        GameObject cellObject = new GameObject(objectName);
        cellObject.transform.SetParent(parent, false);
        cellObject.transform.localScale = style.Scale == Vector3.zero ? Vector3.one : style.Scale;
        SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
        renderer.sprite = style.Sprite != null ? style.Sprite : GetCellSprite();
        if (style.Material != null)
            renderer.sharedMaterial = style.Material;
        renderer.sortingOrder = 0;
        renderer.color = cellValue switch
        {
            TetrisBoardModel.GarbageCell => new Color(0.48f, 0.52f, 0.6f),
            TetrisBoardModel.ManaCell => ManaColor,
            _ => style.Color
        };
        return renderer;
    }

    private static SpriteRenderer CreateBasicRenderer(string objectName, Transform parent, int sortingOrder)
    {
        GameObject cellObject = new GameObject(objectName);
        cellObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetCellSprite();
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Sprite GetCellSprite()
    {
        if (sharedCellSprite != null)
            return sharedCellSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Runtime Tetris Cell",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sharedCellSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sharedCellSprite.name = "Runtime Tetris Cell";
        return sharedCellSprite;
    }

    private Vector3 CellPosition(int x, int y)
    {
        if (battleGrid != null)
            return battleGrid.GetCellCenterLocal(new Vector3Int(x, y, 0));

        return new Vector3(x + 0.5f, y + 0.5f, 0f);
    }

    private Vector3 GetPixelAlignedCellScale()
    {
        Vector3 cellSize = battleGrid != null ? battleGrid.cellSize : Vector3.one;
        return new Vector3(
            cellSize.x * 14f / 16f,
            cellSize.y * 14f / 16f,
            1f);
    }

    private void SetBlockVisibility(
        TetriminoPiece piece,
        Vector2Int[] cells,
        Vector2Int position,
        bool visible)
    {
        SpriteRenderer[] renderers = piece.GetVisualRenderers();
        int count = Mathf.Min(renderers.Length, cells.Length);
        for (int i = 0; i < count; i++)
            renderers[i].enabled = visible && position.y + cells[i].y < Model.VisibleHeight;
    }

    /// <summary>
    /// Repaints the mana cell gold so the player can see which block is worth
    /// steering into a line. Runs every frame the piece view refreshes because
    /// the piece prefab is recreated whenever the type changes.
    /// </summary>
    private void ApplyManaTint(TetriminoPiece piece, float alpha)
    {
        if (piece == null)
            return;

        SpriteRenderer[] renderers = piece.GetVisualRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color color = i == ActiveManaCell
                ? ManaColor
                : cellStyleLookup.TryGetValue(ActiveType, out CellStyle style)
                    ? style.Color
                    : renderers[i].color;

            renderers[i].color = new Color(color.r, color.g, color.b, alpha);
        }
    }

    private static void SetPieceVisible(TetriminoPiece piece, bool visible)
    {
        if (piece == null)
            return;

        foreach (SpriteRenderer renderer in piece.GetVisualRenderers())
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}
