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

    private SevenBagRandomizer randomizer;
    private SharedPieceQueue sharedPieceQueue;
    private System.Random garbageRandom;
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
            _ => false
        };
    }

    public void QueueGarbage(int lineCount)
    {
        if (running && lineCount > 0)
            PendingGarbage = Mathf.Clamp(PendingGarbage + lineCount, 0, 20);
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

    private bool HardDrop()
    {
        int distance = 0;
        while (Model.IsValid(ActiveType, ActivePosition + Vector2Int.down, ActiveRotation))
        {
            ActivePosition += Vector2Int.down;
            distance++;
        }

        Score += distance * 2;
        RefreshActiveView();
        LockActivePiece();
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
        return running;
    }

    private void LockActivePiece()
    {
        if (!running)
            return;

        Vector2Int[] lockedCells = GetActiveBoardCells();
        int cleared = Model.Place(ActiveType, ActivePosition, ActiveRotation);
        PieceLocked?.Invoke(this, lockedCells);

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

        if (attack > 0)
            AttackGenerated?.Invoke(this, attack);

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
            Level = 1 + Lines / 10;
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

        Vector2Int ghostPosition = new Vector2Int(ActivePosition.x, ghostY);
        ghostPieceView.SetState(ghostPosition, ActiveRotation);
        SetBlockVisibility(
            ghostPieceView,
            pieceCells,
            ghostPosition,
            ghostY != int.MinValue);
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
        renderer.color = cellValue == 8
            ? new Color(0.48f, 0.52f, 0.6f)
            : style.Color;
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
