using System;
using UnityEngine;

/// <summary>
/// Scene-root component that owns the <see cref="ScreenRouter"/> and every
/// piece of Unity wiring the screens need (grid, prefabs, camera, the
/// OnGUI-only <see cref="RetroTheme"/>/<see cref="BattleArtLibrary"/>
/// instances). Screens depend on <see cref="IGameFlow"/> for navigation, not
/// on this concrete type.
/// </summary>
public sealed class GameFlowController : MonoBehaviour, IGameFlow
{
    [SerializeField] private bool startAtMainMenu = true;
    [SerializeField] private TetrisGameMode initialMode = TetrisGameMode.VersusCpu;
    [SerializeField] private CpuDifficulty initialCpuDifficulty = CpuDifficulty.Easy;
    [SerializeField] private Grid battleGrid;
    [SerializeField] private TetriminoPiece[] piecePrefabs = Array.Empty<TetriminoPiece>();
    [SerializeField] private float readyDuration = 1.1f;
    [SerializeField] private float startDuration = 0.65f;
    [SerializeField] private float resultDuration = 2.25f;
    [SerializeField] private float sharedPieceCloseClaimWindow = 0.1f;

    private readonly ScreenRouter router = new ScreenRouter();
    private readonly MainMenuModel mainMenuModel = new MainMenuModel();
    private readonly CharacterSelectModel characterSelectModel = new CharacterSelectModel();
    private readonly StoryDirector storyDirector = new StoryDirector(new PrologueStoryScript());

    private MatchDirector matchDirector;
    private BattleEffectsController battleEffects;
    private StoryBattleBridge storyBridge;
    private RetroTheme theme;
    private BattleArtLibrary art;

    private TitleMenuScreen titleMenuScreen;
    private CharacterSelectScreen characterSelectScreen;
    private StoryScreen storyScreen;
    private BattleScreen battleScreen;

    private TetrisGameMode pendingVersusMode = TetrisGameMode.VersusCpu;
    private bool screensReady;

    /// <summary>Raised once the result beat of a match finishes. True when player one won.</summary>
    public event Action<bool> MatchEnded;

    private void Awake()
    {
        mainMenuModel.Difficulty = initialCpuDifficulty;

        battleEffects = GetComponent<BattleEffectsController>();
        if (battleEffects == null)
            battleEffects = gameObject.AddComponent<BattleEffectsController>();

        MatchSettings settings = new MatchSettings(
            readyDuration,
            startDuration,
            resultDuration,
            sharedPieceCloseClaimWindow);
        matchDirector = new MatchDirector(transform, battleGrid, piecePrefabs, battleEffects, settings);
        matchDirector.MatchEnded += OnMatchDirectorEnded;

        storyBridge = GetComponent<StoryBattleBridge>();
        if (storyBridge != null)
            storyBridge.BattleResolved += OnStoryBattleResolved;

        ConfigureCamera();
    }

    private void Update()
    {
        if (!screensReady)
            return;

        UiInput input = UiInput.Sample();
        IGameScreen current = router.Current;

        if (input.Pause && (current == battleScreen || current == storyScreen))
        {
            ShowTitleMenu();
            return;
        }

        router.Tick(Time.deltaTime, input);
    }

    private void OnGUI()
    {
        EnsureScreens();
        using (RetroGui.ReferenceCanvas())
        {
            router.Draw();
        }
    }

    private void OnDestroy()
    {
        if (storyBridge != null)
            storyBridge.BattleResolved -= OnStoryBattleResolved;

        if (matchDirector != null)
            matchDirector.MatchEnded -= OnMatchDirectorEnded;

        theme?.Dispose();
    }

    /// <summary>
    /// <see cref="RetroTheme"/> reads <c>GUI.skin</c>, which only exists
    /// inside <c>OnGUI</c>, so the theme, art library, and every screen are
    /// built lazily on the first GUI pass rather than in Awake/Start.
    /// </summary>
    private void EnsureScreens()
    {
        if (screensReady)
            return;

        theme = new RetroTheme();
        art = new BattleArtLibrary();

        titleMenuScreen = new TitleMenuScreen(mainMenuModel, this, theme);
        characterSelectScreen = new CharacterSelectScreen(
            characterSelectModel, this, theme, art, () => mainMenuModel.Difficulty);
        storyScreen = new StoryScreen(storyDirector, this, theme, art, () => mainMenuModel.Difficulty);
        battleScreen = new BattleScreen(matchDirector, this, theme, art);

        screensReady = true;

        if (startAtMainMenu)
            ShowTitleMenu();
        else
            StartMatch(initialMode);
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.025f, 0.03f, 0.055f, 1f);
    }

    public void ShowTitleMenu()
    {
        CancelStoryIfRunning();
        matchDirector.Clear();
        mainMenuModel.ShowRoot();
        router.GoTo(titleMenuScreen);
    }

    public void ShowCharacterSelect(TetrisGameMode versusMode)
    {
        CancelStoryIfRunning();
        matchDirector.Clear();
        pendingVersusMode = versusMode;
        characterSelectModel.Begin(versusMode);
        router.GoTo(characterSelectScreen);
    }

    public void CloseCharacterSelect()
    {
        if (pendingVersusMode == TetrisGameMode.VersusCpu)
            mainMenuModel.ShowCpuDifficulty();
        else
            mainMenuModel.ShowRoot(2);

        router.GoTo(titleMenuScreen);
    }

    public void BeginStory()
    {
        matchDirector.Clear();
        storyDirector.Begin();
        router.GoTo(storyScreen);
    }

    public void RequestStoryBattle()
    {
        storyDirector.EnterBattle();
        if (storyBridge != null)
            storyBridge.RequestBattle(storyDirector.Script.BattleId);
        else
            StartMatch(TetrisGameMode.VersusCpu);
    }

    public void BeginMatch(TetrisGameMode mode)
    {
        CancelStoryIfRunning();
        StartMatch(mode);
    }

    public void RestartMatch()
    {
        StartMatch(matchDirector.Mode);
    }

    /// <summary>Called by <see cref="StoryBattleBridge"/> once it has a battle to start.</summary>
    public void StartBattle(TetrisGameMode mode)
    {
        StartMatch(mode);
    }

    private void StartMatch(TetrisGameMode mode)
    {
        MatchSetup setup = storyDirector.IsRunning
            ? new MatchSetup(mode, mainMenuModel.Difficulty, 0, 1, true, storyDirector.Script.EncounterTitle)
            : mode == TetrisGameMode.Solo
                ? MatchSetup.Solo()
                : new MatchSetup(
                    mode,
                    mainMenuModel.Difficulty,
                    characterSelectModel.PlayerOneIndex,
                    characterSelectModel.PlayerTwoIndex,
                    false);

        matchDirector.Begin(setup);
        router.GoTo(battleScreen);
    }

    private void CancelStoryIfRunning()
    {
        if (!storyDirector.IsRunning)
            return;

        storyBridge?.CancelBattle();
        storyDirector.Cancel();
    }

    private void OnMatchDirectorEnded(bool playerOneWon)
    {
        MatchEnded?.Invoke(playerOneWon);
    }

    private void OnStoryBattleResolved(string battleId, bool playerWon)
    {
        if (!storyDirector.IsRunning ||
            !string.Equals(battleId, storyDirector.Script.BattleId, StringComparison.Ordinal))
            return;

        storyDirector.ReportResult(playerWon);
        router.GoTo(storyScreen);
    }
}
