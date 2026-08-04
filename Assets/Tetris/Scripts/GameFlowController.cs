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
    private readonly OptionsModel optionsModel = new OptionsModel();
    private readonly CharacterSelectModel characterSelectModel = new CharacterSelectModel();
    private readonly SaveSlotMenuModel loadMenuModel = new SaveSlotMenuModel();
    private readonly StoryDirector storyDirector = new StoryDirector(new PrologueStoryScript());

    private MatchDirector matchDirector;
    private BattleEffectsController battleEffects;
    private StoryBattleBridge storyBridge;
    private SaveSlotCatalog saveSlots;
    private GameStats stats;
    private RetroTheme theme;
    private BattleArtLibrary art;

    private TitleMenuScreen titleMenuScreen;
    private OptionsScreen optionsScreen;
    private CharacterSelectScreen characterSelectScreen;
    private SaveSlotScreen loadGameScreen;
    private StoryScreen storyScreen;
    private BattleScreen battleScreen;

    private TetrisGameMode pendingVersusMode = TetrisGameMode.VersusCpu;
    private bool screensReady;

    /// <summary>Raised once the result beat of a match finishes. True when player one won.</summary>
    public event Action<bool> MatchEnded;

    private void Awake()
    {
        mainMenuModel.Difficulty = initialCpuDifficulty;

        // The bootstrap scene has normally done this already; the repeat call is
        // a no-op and is what lets this scene be entered directly in the editor.
        // Audio deliberately is not added to this object any more — it lives on
        // its own persistent host so it survives the scene load.
        GameBootstrap.EnsureInitialized();

        // Save slots and statistics share one store, so a build only ever has
        // a single save folder to reason about.
        IJsonStore store = new FileJsonStore();
        saveSlots = new SaveSlotCatalog(store);
        stats = new GameStats(store);
        stats.BeginSession();

        // Effects stay scene-local: they spawn world-space objects positioned
        // against the boards, which do not outlive the scene that holds them.
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

        TrackPlaytime();

        UiInput input = UiInput.Sample();

        // Story mode owns its own pause key: it opens the save/load menu
        // instead of abandoning the chapter.
        if (input.Pause && router.Current == battleScreen)
        {
            ShowTitleMenu();
            return;
        }

        router.Tick(Time.deltaTime, input);
    }

    /// <summary>
    /// Advances the analytics clock and the chapter clock. The total keeps
    /// running while the story is paused; the chapter clock does not, so a save
    /// slot's playtime only counts time actually spent playing.
    /// </summary>
    private void TrackPlaytime()
    {
        float delta = Time.unscaledDeltaTime;
        bool storyActive = storyDirector.IsRunning && !storyScreen.IsPaused;

        stats.AddPlaytime(delta, storyActive);
        if (storyActive)
            storyDirector.AddPlaytime(delta);
    }

    private void OnGUI()
    {
        EnsureScreens();
        using (RetroGui.ReferenceCanvas())
        {
            router.Draw();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        // Mobile and console builds can be suspended without ever reaching
        // OnDestroy, so this is the last reliable chance to persist counters.
        if (paused)
            stats?.Flush();
    }

    private void OnApplicationQuit()
    {
        stats?.Flush();
    }

    private void OnDestroy()
    {
        stats?.Flush();

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
        optionsScreen = new OptionsScreen(optionsModel, this, theme);
        characterSelectScreen = new CharacterSelectScreen(
            characterSelectModel, this, theme, art, () => mainMenuModel.Difficulty);
        loadGameScreen = new SaveSlotScreen(loadMenuModel, saveSlots, this, theme);
        storyScreen = new StoryScreen(
            storyDirector, this, theme, art, () => mainMenuModel.Difficulty, saveSlots);
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
        stats.Flush();
        RefreshSaveState();
        mainMenuModel.ShowRoot();
        router.GoTo(titleMenuScreen);
        GameAudio.PlayMusic(GameMusic.Menu);
    }

    public void ShowOptions()
    {
        router.GoTo(optionsScreen);
    }

    public void CloseOptions()
    {
        mainMenuModel.ShowRoot(MainMenuModel.OptionsRow);
        router.GoTo(titleMenuScreen);
    }

    public void ShowCharacterSelect(TetrisGameMode versusMode)
    {
        CancelStoryIfRunning();
        matchDirector.Clear();
        pendingVersusMode = versusMode;
        characterSelectModel.Begin(versusMode);
        router.GoTo(characterSelectScreen);
        GameAudio.PlayMusic(GameMusic.Menu);
    }

    public void CloseCharacterSelect()
    {
        if (pendingVersusMode == TetrisGameMode.VersusCpu)
            mainMenuModel.ShowCpuDifficulty();
        else
            mainMenuModel.ShowVersusMode(MainMenuModel.VersusPlayerRow);

        router.GoTo(titleMenuScreen);
        GameAudio.PlayMusic(GameMusic.Menu);
    }

    public void BeginStory()
    {
        matchDirector.Clear();
        storyDirector.Begin();
        router.GoTo(storyScreen);
        GameAudio.PlayMusic(GameMusic.Story);
    }

    public void ShowLoadGame()
    {
        matchDirector.Clear();
        router.GoTo(loadGameScreen);
        GameAudio.PlayMusic(GameMusic.Menu);
    }

    public void CloseLoadGame()
    {
        RefreshSaveState();
        mainMenuModel.ShowStory(MainMenuModel.LoadGameRow);
        router.GoTo(titleMenuScreen);
        GameAudio.PlayMusic(GameMusic.Menu);
    }

    public bool SaveStory(int slot)
    {
        if (!storyDirector.IsRunning)
            return false;

        StorySaveData data = StorySaveData.Capture(storyDirector, DateTime.UtcNow);
        if (!saveSlots.Save(slot, data))
            return false;

        stats.RecordStorySave();
        stats.Flush();
        mainMenuModel.HasAnySave = saveSlots.HasAnySave;
        return true;
    }

    public bool LoadStory(int slot)
    {
        if (!saveSlots.TryLoad(slot, out StorySaveData data))
            return false;

        StoryProgress progress = data.ToProgress();
        if (!storyDirector.CanRestore(progress))
            return false;

        // A chapter can be loaded from the pause menu mid-run, so tear down
        // whatever the previous run left behind before restoring.
        storyBridge?.CancelBattle();
        matchDirector.Clear();

        if (!storyDirector.Restore(progress))
            return false;

        router.GoTo(storyScreen);
        GameAudio.PlayMusic(GameMusic.Story);
        return true;
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
            : mode switch
            {
                TetrisGameMode.Marathon => MatchSetup.Marathon(),
                TetrisGameMode.Sprint => MatchSetup.Sprint(),
                _ => new MatchSetup(
                    mode,
                    mainMenuModel.Difficulty,
                    characterSelectModel.PlayerOneIndex,
                    characterSelectModel.PlayerTwoIndex,
                    false)
            };

        matchDirector.Begin(setup);
        router.GoTo(battleScreen);
        GameAudio.PlayMusic(GameMusic.Battle);
    }

    private void CancelStoryIfRunning()
    {
        if (!storyDirector.IsRunning)
            return;

        storyBridge?.CancelBattle();
        storyDirector.Cancel();
    }

    /// <summary>
    /// Re-reads the slot summaries so the title menu knows whether load game
    /// has anything to offer.
    /// </summary>
    private void RefreshSaveState()
    {
        saveSlots.Refresh();
        mainMenuModel.HasAnySave = saveSlots.HasAnySave;
    }

    private void OnMatchDirectorEnded(bool playerOneWon)
    {
        stats.RecordMatch(
            matchDirector.Mode,
            matchDirector.PlayerOneCharacter,
            playerOneWon,
            matchDirector.IsStoryBattle);

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
