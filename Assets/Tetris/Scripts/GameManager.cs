using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class GameManager : MonoBehaviour
{
    private const float MenuWidth = 640f;
    private const float MenuHeight = 480f;
    private const string OpeningBattleId = "chapter1_opening";

    [SerializeField] private bool startAtMainMenu = true;
    [SerializeField] private TetrisGameMode initialMode = TetrisGameMode.VersusCpu;
    [SerializeField] private CpuDifficulty cpuDifficulty = CpuDifficulty.Easy;
    [SerializeField] private Grid battleGrid;
    [SerializeField] private TetriminoPiece[] piecePrefabs = Array.Empty<TetriminoPiece>();
    [SerializeField] private float readyDuration = 1.1f;
    [SerializeField] private float startDuration = 0.65f;
    [SerializeField] private float resultDuration = 2.25f;
    [SerializeField] private float sharedPieceCloseClaimWindow = 0.1f;

    private TetrisGameSession playerOne;
    private TetrisGameSession playerTwo;
    private SimpleTetrisCpu cpu;
    private SharedPieceQueue sharedPieceQueue;
    private BattleEffectsController battleEffects;
    private TetrisGameMode currentMode;
    private string matchMessage = string.Empty;
    private MatchPresentationState matchPresentationState;
    private float matchPresentationTimer;
    private string resultLoserName = string.Empty;
    private bool resultPlayerOneWon;

    private InputRepeatState playerOneRepeat;
    private InputRepeatState playerTwoRepeat;
    private GUIStyle titleStyle;
    private GUIStyle hudStyle;
    private GUIStyle helpStyle;
    private GUIStyle menuTitleStyle;
    private GUIStyle menuSubtitleStyle;
    private GUIStyle menuHeadingStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle selectedMenuButtonStyle;
    private GUIStyle menuDetailStyle;
    private GUIStyle menuFooterStyle;
    private Texture2D menuPanelTexture;
    private Texture2D menuButtonTexture;
    private Texture2D menuButtonHoverTexture;
    private Texture2D menuButtonSelectedTexture;
    private Texture2D storyBackdrop;
    private Texture2D lockedCharacterPortrait;
    private bool isMainMenuVisible;
    private MainMenuPage mainMenuPage;
    private int menuSelection;
    private TetrisGameMode pendingVersusMode = TetrisGameMode.VersusCpu;
    private int characterSelectStage;
    private int characterSelection;
    private int playerOneCharacterIndex;
    private int playerTwoCharacterIndex = 1;
    private string characterSelectMessage = string.Empty;
    private StoryBattleBridge storyBridge;
    private StoryPresentationState storyState;
    private int storyLineIndex;
    private int storySelection;
    private int storyResponse;
    private bool storyBattleActive;
    private bool storyBattleWon;
    private GUIStyle storyLocationStyle;
    private GUIStyle storyNameStyle;
    private GUIStyle storyDialogueStyle;
    private GUIStyle storyPromptStyle;
    private GUIStyle characterNameStyle;
    private GUIStyle characterTitleStyle;
    private GUIStyle matchCalloutStyle;
    private GUIStyle matchRoleStyle;
    private GUIStyle matchWinnerStyle;
    private GUIStyle matchLoserStyle;

    public event Action<bool> MatchEnded;

    public TetrisGameMode CurrentMode => currentMode;
    public bool IsMainMenuVisible => isMainMenuVisible;
    public CpuDifficulty SelectedCpuDifficulty => cpuDifficulty;
    public bool IsStoryPresentationVisible => storyState != StoryPresentationState.None;
    public bool IsStoryBattleActive => storyBattleActive;
    public bool IsCharacterSelectVisible =>
        isMainMenuVisible && mainMenuPage == MainMenuPage.CharacterSelect;
    public string SelectedPlayerOneCharacter =>
        BattleCharacterRoster.Get(playerOneCharacterIndex).DisplayName;
    public string SelectedPlayerTwoCharacter =>
        BattleCharacterRoster.Get(playerTwoCharacterIndex).DisplayName;

    private enum MainMenuPage
    {
        Root,
        CpuDifficulty,
        CharacterSelect
    }

    private enum StoryPresentationState
    {
        None,
        Opening,
        Choice,
        Challenge,
        Result
    }

    private enum MatchPresentationState
    {
        None,
        Ready,
        Start,
        Result
    }

    private readonly struct StoryLine
    {
        public StoryLine(string speaker, string text, int focus)
        {
            Speaker = speaker;
            Text = text;
            Focus = focus;
        }

        public string Speaker { get; }
        public string Text { get; }
        public int Focus { get; }
    }

    private static readonly StoryLine[] OpeningStoryLines =
    {
        new(
            "NARRATOR",
            "On the eve of the Starlight Festival, the ancient Moon Gate refuses to open.",
            0),
        new(
            "LYRA",
            "The gate is humming in seven colors. It wants a puzzle spell—let me try.",
            -1),
        new(
            "BRAM",
            "You? The festival trials need a steady hand, not another lucky constellation.",
            1)
    };

    private struct InputRepeatState
    {
        public int horizontalDirection;
        public float horizontalTimer;
        public float softDropTimer;
    }

    private void Awake()
    {
        storyBridge = GetComponent<StoryBattleBridge>();
        if (storyBridge != null)
            storyBridge.BattleResolved += OnStoryBattleResolved;
    }

    private void Start()
    {
        if (startAtMainMenu)
            ShowMainMenu();
        else
            BeginMode(initialMode);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (isMainMenuVisible)
        {
            PollMainMenu(keyboard);
            return;
        }

        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (Gamepad.all.Count > 0 && Gamepad.all[0].startButton.wasPressedThisFrame))
        {
            ShowMainMenu();
            return;
        }

        if (storyState != StoryPresentationState.None)
        {
            PollStoryPresentation(keyboard);
            return;
        }

        if (UpdateMatchPresentation())
            return;

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                CancelStoryMode();
                BeginMode(TetrisGameMode.Solo);
                return;
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                CancelStoryMode();
                BeginMode(TetrisGameMode.VersusCpu);
                return;
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                CancelStoryMode();
                BeginMode(TetrisGameMode.LocalVersus);
                return;
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                BeginMode(currentMode);
                return;
            }
        }

        if (playerOne == null)
            return;

        PollPlayerOneKeyboard(keyboard);
        PollGamepad(playerOne, 0, ref playerOneRepeat);

        if (currentMode == TetrisGameMode.LocalVersus && playerTwo != null)
        {
            PollPlayerTwoKeyboard(keyboard);
            PollGamepad(playerTwo, 1, ref playerTwoRepeat);
        }
        else
        {
            cpu?.Tick(Time.deltaTime);
        }

        // A command can resolve a story battle synchronously and tear down both
        // sessions before this Update call continues.
        if (playerOne == null)
            return;

        playerOne.Tick(Time.deltaTime);
        playerTwo?.Tick(Time.deltaTime);
    }

    private bool UpdateMatchPresentation()
    {
        if (matchPresentationState == MatchPresentationState.None)
            return false;

        matchPresentationTimer -= Time.unscaledDeltaTime;
        if (matchPresentationTimer > 0f)
            return true;

        if (matchPresentationState == MatchPresentationState.Ready)
        {
            matchPresentationState = MatchPresentationState.Start;
            matchPresentationTimer = Mathf.Max(0f, startDuration);
            return true;
        }

        if (matchPresentationState == MatchPresentationState.Start)
        {
            matchPresentationState = MatchPresentationState.None;
            matchPresentationTimer = 0f;
            return true;
        }

        matchPresentationState = MatchPresentationState.None;
        matchPresentationTimer = 0f;
        MatchEnded?.Invoke(resultPlayerOneWon);
        return true;
    }

    private void BeginMode(TetrisGameMode mode)
    {
        isMainMenuVisible = false;
        currentMode = mode;
        matchMessage = string.Empty;
        playerOneRepeat = default;
        playerTwoRepeat = default;
        ResetMatchPresentation();

        battleEffects?.ClearBattle();
        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        sharedPieceQueue = mode == TetrisGameMode.Solo
            ? null
            : new SharedPieceQueue(303, sharedPieceCloseClaimWindow);

        if (mode == TetrisGameMode.Solo)
        {
            playerOne = CreateSession("PLAYER", new Vector3Int(-5, -10, 0), 101);
        }
        else
        {
            EnsureCharacterAssets();
            int firstCharacter = storyBattleActive ? 0 : playerOneCharacterIndex;
            int secondCharacter = storyBattleActive ? 1 : playerTwoCharacterIndex;

            playerOne = CreateSession(
                BattleCharacterRoster.Get(firstCharacter).DisplayName,
                new Vector3Int(-12, -10, 0),
                101);
            playerTwo = CreateSession(
                BattleCharacterRoster.Get(secondCharacter).DisplayName,
                new Vector3Int(2, -10, 0),
                202);

            playerOne.AttackGenerated += (_, lines) => playerTwo?.QueueGarbage(lines);
            playerTwo.AttackGenerated += (_, lines) => playerOne?.QueueGarbage(lines);

            if (mode == TetrisGameMode.VersusCpu)
                cpu = new SimpleTetrisCpu(playerTwo, cpuDifficulty, 2026);
        }

        playerOne.GameOver += OnSessionGameOver;
        if (playerTwo != null)
            playerTwo.GameOver += OnSessionGameOver;

        battleEffects ??= GetComponent<BattleEffectsController>();
        battleEffects ??= gameObject.AddComponent<BattleEffectsController>();
        battleEffects.Initialize(playerOne, playerTwo, piecePrefabs);

        ConfigureCamera();
        matchPresentationState = MatchPresentationState.Ready;
        matchPresentationTimer = Mathf.Max(0f, readyDuration);
    }

    private TetrisGameSession CreateSession(string displayName, Vector3Int gridOrigin, int seed)
    {
        GameObject sessionObject = new GameObject($"{displayName} Board");
        sessionObject.transform.SetParent(
            battleGrid != null ? battleGrid.transform : transform,
            false);
        TetrisGameSession session = sessionObject.AddComponent<TetrisGameSession>();
        session.Initialize(
            displayName,
            battleGrid,
            gridOrigin,
            seed,
            piecePrefabs,
            sharedPieceQueue);
        return session;
    }

    private static void DestroySession(ref TetrisGameSession session)
    {
        if (session != null)
            Destroy(session.gameObject);

        session = null;
    }

    private void OnSessionGameOver(TetrisGameSession loser)
    {
        if (matchPresentationState == MatchPresentationState.Result)
            return;

        bool playerOneWon = playerTwo != null && loser == playerTwo;
        TetrisGameSession winner = loser == playerOne ? playerTwo : playerOne;

        if (currentMode == TetrisGameMode.Solo)
        {
            matchMessage = "GAME OVER\nPRESS R TO RETRY";
        }
        else
        {
            string winnerName = winner?.DisplayName ?? "OPPONENT";
            matchMessage = storyBattleActive
                ? $"{winnerName} IS THE WINNER"
                : $"{winnerName} IS THE WINNER\nPRESS R TO REMATCH";
        }

        resultLoserName = loser?.DisplayName ?? "PLAYER";
        resultPlayerOneWon = playerOneWon;
        playerOne?.Stop();
        playerTwo?.Stop();
        matchPresentationState = MatchPresentationState.Result;
        matchPresentationTimer = Mathf.Max(0f, resultDuration);
    }

    public void StartBattle(TetrisGameMode mode)
    {
        BeginMode(mode);
    }

    public void StartStoryBattle()
    {
        ResetMatchPresentation();
        storyBattleActive = true;
        storyBattleWon = false;
        storyLineIndex = 0;
        storySelection = 0;
        storyResponse = 0;
        storyState = StoryPresentationState.Opening;
        isMainMenuVisible = false;
        matchMessage = string.Empty;

        battleEffects?.ClearBattle();
        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        sharedPieceQueue = null;

        EnsureStoryBackdrop();
        ConfigureCamera();
    }

    public void StartCpuBattle(CpuDifficulty difficulty)
    {
        CancelStoryMode();
        cpuDifficulty = difficulty;
        BeginMode(TetrisGameMode.VersusCpu);
    }

    public void StartLocalBattle()
    {
        CancelStoryMode();
        BeginMode(TetrisGameMode.LocalVersus);
    }

    public void OpenCharacterSelect(TetrisGameMode versusMode)
    {
        if (versusMode != TetrisGameMode.VersusCpu &&
            versusMode != TetrisGameMode.LocalVersus)
        {
            throw new ArgumentException(
                "Character select is only available for versus modes.",
                nameof(versusMode));
        }

        CancelStoryMode();
        ResetMatchPresentation();
        EnsureCharacterAssets();
        pendingVersusMode = versusMode;
        characterSelectStage = 0;
        characterSelection = Mathf.Clamp(
            playerOneCharacterIndex,
            0,
            BattleCharacterRoster.Count - 1);
        characterSelectMessage = string.Empty;
        isMainMenuVisible = true;
        mainMenuPage = MainMenuPage.CharacterSelect;
        matchMessage = string.Empty;

        battleEffects?.ClearBattle();
        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        sharedPieceQueue = null;
        ConfigureCamera();
    }

    public bool UnlockCharacter(string characterId)
    {
        return BattleCharacterRoster.Unlock(characterId);
    }

    public void ShowMainMenu()
    {
        CancelStoryMode();
        ResetMatchPresentation();

        isMainMenuVisible = true;
        mainMenuPage = MainMenuPage.Root;
        menuSelection = 0;
        characterSelectStage = 0;
        characterSelection = playerOneCharacterIndex;
        characterSelectMessage = string.Empty;
        matchMessage = string.Empty;
        playerOneRepeat = default;
        playerTwoRepeat = default;

        battleEffects?.ClearBattle();
        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        sharedPieceQueue = null;

        ConfigureCamera();
    }

    private void CancelStoryMode()
    {
        storyBridge?.CancelBattle();
        storyState = StoryPresentationState.None;
        storyBattleActive = false;
        storyLineIndex = 0;
        storySelection = 0;
        storyResponse = 0;
    }

    private void BeginStoryEncounter()
    {
        storyState = StoryPresentationState.None;
        matchMessage = string.Empty;

        if (storyBridge != null)
            storyBridge.RequestBattle(OpeningBattleId);
        else
            BeginMode(TetrisGameMode.VersusCpu);
    }

    private void OnStoryBattleResolved(string battleId, bool playerWon)
    {
        if (!storyBattleActive || !string.Equals(battleId, OpeningBattleId, StringComparison.Ordinal))
            return;

        ResetMatchPresentation();
        storyBattleWon = playerWon;
        storySelection = 0;
        storyState = StoryPresentationState.Result;
        matchMessage = string.Empty;

        battleEffects?.ClearBattle();
        DestroySession(ref playerOne);
        DestroySession(ref playerTwo);
        cpu = null;
        sharedPieceQueue = null;
    }

    private void ResetMatchPresentation()
    {
        matchPresentationState = MatchPresentationState.None;
        matchPresentationTimer = 0f;
        resultLoserName = string.Empty;
        resultPlayerOneWon = false;
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

    private void PollPlayerOneKeyboard(Keyboard keyboard)
    {
        if (keyboard == null)
            return;

        bool leftPressed = keyboard.aKey.wasPressedThisFrame;
        bool rightPressed = keyboard.dKey.wasPressedThisFrame;
        HandleHorizontal(
            playerOne,
            leftPressed,
            rightPressed,
            keyboard.aKey.isPressed,
            keyboard.dKey.isPressed,
            ref playerOneRepeat);

        HandleSoftDrop(
            playerOne,
            keyboard.sKey.wasPressedThisFrame,
            keyboard.sKey.isPressed,
            ref playerOneRepeat);

        if (keyboard.wKey.wasPressedThisFrame)
            playerOne.ApplyCommand(TetrisCommand.RotateClockwise);
        if (keyboard.qKey.wasPressedThisFrame)
            playerOne.ApplyCommand(TetrisCommand.RotateCounterClockwise);
        if (keyboard.spaceKey.wasPressedThisFrame)
            playerOne.ApplyCommand(TetrisCommand.HardDrop);
        if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame)
            playerOne.ApplyCommand(TetrisCommand.Hold);
    }

    private void PollPlayerTwoKeyboard(Keyboard keyboard)
    {
        if (keyboard == null || playerTwo == null)
            return;

        HandleHorizontal(
            playerTwo,
            keyboard.leftArrowKey.wasPressedThisFrame,
            keyboard.rightArrowKey.wasPressedThisFrame,
            keyboard.leftArrowKey.isPressed,
            keyboard.rightArrowKey.isPressed,
            ref playerTwoRepeat);

        HandleSoftDrop(
            playerTwo,
            keyboard.downArrowKey.wasPressedThisFrame,
            keyboard.downArrowKey.isPressed,
            ref playerTwoRepeat);

        if (keyboard.upArrowKey.wasPressedThisFrame)
            playerTwo.ApplyCommand(TetrisCommand.RotateClockwise);
        if (keyboard.rightCtrlKey.wasPressedThisFrame)
            playerTwo.ApplyCommand(TetrisCommand.RotateCounterClockwise);
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            playerTwo.ApplyCommand(TetrisCommand.HardDrop);
        if (keyboard.rightShiftKey.wasPressedThisFrame)
            playerTwo.ApplyCommand(TetrisCommand.Hold);
    }

    private void PollGamepad(TetrisGameSession session, int gamepadIndex, ref InputRepeatState repeatState)
    {
        if (session == null || Gamepad.all.Count <= gamepadIndex)
            return;

        Gamepad gamepad = Gamepad.all[gamepadIndex];
        ButtonControl left = gamepad.dpad.left;
        ButtonControl right = gamepad.dpad.right;
        ButtonControl down = gamepad.dpad.down;

        HandleHorizontal(
            session,
            left.wasPressedThisFrame,
            right.wasPressedThisFrame,
            left.isPressed,
            right.isPressed,
            ref repeatState);

        HandleSoftDrop(
            session,
            down.wasPressedThisFrame,
            down.isPressed,
            ref repeatState);

        if (gamepad.buttonSouth.wasPressedThisFrame)
            session.ApplyCommand(TetrisCommand.RotateClockwise);
        if (gamepad.buttonWest.wasPressedThisFrame)
            session.ApplyCommand(TetrisCommand.RotateCounterClockwise);
        if (gamepad.buttonEast.wasPressedThisFrame)
            session.ApplyCommand(TetrisCommand.HardDrop);
        if (gamepad.leftShoulder.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame)
            session.ApplyCommand(TetrisCommand.Hold);
    }

    private void PollMainMenu(Keyboard keyboard)
    {
        if (mainMenuPage == MainMenuPage.CharacterSelect)
        {
            PollCharacterSelect(keyboard);
            return;
        }

        Gamepad gamepad = Gamepad.all.Count > 0 ? Gamepad.all[0] : null;
        bool moveUp =
            (keyboard != null &&
             (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
        bool moveDown =
            (keyboard != null &&
             (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.down.wasPressedThisFrame);
        bool confirm =
            (keyboard != null &&
             (keyboard.enterKey.wasPressedThisFrame ||
              keyboard.numpadEnterKey.wasPressedThisFrame ||
              keyboard.spaceKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        bool back =
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        int itemCount = mainMenuPage == MainMenuPage.Root ? 3 : 4;
        if (moveUp)
            menuSelection = (menuSelection - 1 + itemCount) % itemCount;
        else if (moveDown)
            menuSelection = (menuSelection + 1) % itemCount;

        if (back && mainMenuPage == MainMenuPage.CpuDifficulty)
        {
            mainMenuPage = MainMenuPage.Root;
            menuSelection = 1;
            return;
        }

        if (confirm)
            ActivateMenuSelection(menuSelection);
    }

    private void PollCharacterSelect(Keyboard keyboard)
    {
        Gamepad gamepad = Gamepad.all.Count > 0 ? Gamepad.all[0] : null;
        bool moveLeft =
            (keyboard != null &&
             (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.left.wasPressedThisFrame);
        bool moveRight =
            (keyboard != null &&
             (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.right.wasPressedThisFrame);
        bool confirm =
            (keyboard != null &&
             (keyboard.enterKey.wasPressedThisFrame ||
              keyboard.numpadEnterKey.wasPressedThisFrame ||
              keyboard.spaceKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        bool back =
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        if (moveLeft)
            MoveCharacterSelection(-1);
        else if (moveRight)
            MoveCharacterSelection(1);

        if (back)
        {
            BackFromCharacterSelect();
            return;
        }

        if (confirm)
            ConfirmCharacterSelection();
    }

    private void MoveCharacterSelection(int direction)
    {
        characterSelection =
            (characterSelection + direction + BattleCharacterRoster.Count) %
            BattleCharacterRoster.Count;
        characterSelectMessage = string.Empty;
    }

    private void ConfirmCharacterSelection()
    {
        if (!BattleCharacterRoster.IsUnlocked(characterSelection))
        {
            characterSelectMessage = "LOCKED  •  WIN ADVENTURES TO UNLOCK";
            return;
        }

        if (characterSelectStage == 0)
        {
            playerOneCharacterIndex = characterSelection;
            characterSelectStage = 1;
            characterSelection = playerOneCharacterIndex == 1 ? 0 : 1;
            characterSelectMessage = string.Empty;
            return;
        }

        playerTwoCharacterIndex = characterSelection;
        characterSelectMessage = string.Empty;
        BeginMode(pendingVersusMode);
    }

    private void BackFromCharacterSelect()
    {
        characterSelectMessage = string.Empty;
        if (characterSelectStage > 0)
        {
            characterSelectStage = 0;
            characterSelection = playerOneCharacterIndex;
            return;
        }

        if (pendingVersusMode == TetrisGameMode.VersusCpu)
        {
            mainMenuPage = MainMenuPage.CpuDifficulty;
            menuSelection = (int)cpuDifficulty;
        }
        else
        {
            mainMenuPage = MainMenuPage.Root;
            menuSelection = 2;
        }
    }

    private void PollStoryPresentation(Keyboard keyboard)
    {
        Gamepad gamepad = Gamepad.all.Count > 0 ? Gamepad.all[0] : null;
        bool moveUp =
            (keyboard != null &&
             (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
        bool moveDown =
            (keyboard != null &&
             (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.dpad.down.wasPressedThisFrame);
        bool confirm =
            (keyboard != null &&
             (keyboard.enterKey.wasPressedThisFrame ||
              keyboard.numpadEnterKey.wasPressedThisFrame ||
              keyboard.spaceKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

        if (storyState == StoryPresentationState.Choice ||
            storyState == StoryPresentationState.Result)
        {
            if (moveUp || moveDown)
                storySelection = 1 - storySelection;

            if (confirm)
                ResolveStorySelection();

            return;
        }

        if (confirm)
            AdvanceStoryDialogue();
    }

    private void AdvanceStoryDialogue()
    {
        if (storyState == StoryPresentationState.Opening)
        {
            storyLineIndex++;
            if (storyLineIndex >= OpeningStoryLines.Length)
            {
                storyState = StoryPresentationState.Choice;
                storySelection = 0;
            }

            return;
        }

        if (storyState == StoryPresentationState.Challenge)
            BeginStoryEncounter();
    }

    private void ResolveStorySelection()
    {
        if (storyState == StoryPresentationState.Choice)
        {
            storyResponse = storySelection;
            storyState = StoryPresentationState.Challenge;
            return;
        }

        if (storyState != StoryPresentationState.Result)
            return;

        if (!storyBattleWon && storySelection == 0)
        {
            BeginStoryEncounter();
            return;
        }

        ShowMainMenu();
    }

    private void ActivateMenuSelection(int selection)
    {
        if (mainMenuPage == MainMenuPage.Root)
        {
            switch (selection)
            {
                case 0:
                    StartStoryBattle();
                    break;
                case 1:
                    mainMenuPage = MainMenuPage.CpuDifficulty;
                    menuSelection = (int)cpuDifficulty;
                    break;
                case 2:
                    OpenCharacterSelect(TetrisGameMode.LocalVersus);
                    break;
            }

            return;
        }

        if (selection >= 0 && selection <= (int)CpuDifficulty.Hard)
        {
            cpuDifficulty = (CpuDifficulty)selection;
            OpenCharacterSelect(TetrisGameMode.VersusCpu);
            return;
        }

        mainMenuPage = MainMenuPage.Root;
        menuSelection = 1;
    }

    private static void HandleHorizontal(
        TetrisGameSession session,
        bool leftPressed,
        bool rightPressed,
        bool leftHeld,
        bool rightHeld,
        ref InputRepeatState state)
    {
        if (session == null)
            return;

        int direction = leftHeld == rightHeld ? 0 : leftHeld ? -1 : 1;
        bool pressed = leftPressed || rightPressed;

        if (pressed)
        {
            session.ApplyCommand(leftPressed ? TetrisCommand.MoveLeft : TetrisCommand.MoveRight);
            state.horizontalDirection = direction;
            state.horizontalTimer = 0.16f;
            return;
        }

        if (direction == 0)
        {
            state.horizontalDirection = 0;
            state.horizontalTimer = 0f;
            return;
        }

        if (direction != state.horizontalDirection)
        {
            state.horizontalDirection = direction;
            state.horizontalTimer = 0.16f;
            return;
        }

        state.horizontalTimer -= Time.deltaTime;
        if (state.horizontalTimer <= 0f)
        {
            session.ApplyCommand(direction < 0 ? TetrisCommand.MoveLeft : TetrisCommand.MoveRight);
            state.horizontalTimer = 0.055f;
        }
    }

    private static void HandleSoftDrop(
        TetrisGameSession session,
        bool pressed,
        bool held,
        ref InputRepeatState state)
    {
        if (session == null)
            return;

        if (pressed)
        {
            session.ApplyCommand(TetrisCommand.SoftDrop);
            state.softDropTimer = 0.055f;
            return;
        }

        if (!held)
        {
            state.softDropTimer = 0f;
            return;
        }

        state.softDropTimer -= Time.deltaTime;
        if (state.softDropTimer <= 0f)
        {
            session.ApplyCommand(TetrisCommand.SoftDrop);
            state.softDropTimer = 0.035f;
        }
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        if (storyState != StoryPresentationState.None)
        {
            DrawStoryPresentation();
            return;
        }

        if (isMainMenuVisible)
        {
            DrawMainMenu();
            return;
        }

        DrawBattleInterface();
    }

    private void DrawBattleInterface()
    {
        Matrix4x4 previousMatrix = BeginReferenceCanvas();

        Rect titlePanel = new Rect(178f, 7f, 284f, 29f);
        DrawPixelRect(titlePanel, new Color(0.025f, 0.045f, 0.11f, 0.94f));
        DrawRectBorder(titlePanel, new Color(0.33f, 0.75f, 0.94f, 0.85f), 1f);
        GUI.Label(
            titlePanel,
            storyBattleActive
                ? "MOON GATE DUEL"
                : currentMode == TetrisGameMode.Solo
                    ? "SOLO TRIAL"
                    : currentMode == TetrisGameMode.VersusCpu
                        ? "RIVAL BATTLE"
                        : "LOCAL VERSUS",
            titleStyle);

        if (playerOne != null)
            DrawSessionHud(playerOne, 8f);

        if (playerTwo != null)
            DrawSessionHud(playerTwo, 522f);

        if (sharedPieceQueue != null && playerTwo != null)
            DrawSharedNextPreview();
        else if (playerOne != null)
            DrawNextPreview(playerOne, 320f);

        if (playerTwo != null)
        {
            Rect versusBadge = new Rect(302f, 212f, 36f, 36f);
            GUI.Label(versusBadge, "VS", menuHeadingStyle);
        }

        if (playerTwo != null)
            DrawBattleCharacterPortraits();

        if (!string.IsNullOrEmpty(matchMessage))
        {
            Rect resultPanel = new Rect(125f, 195f, 390f, 86f);
            DrawPixelRect(resultPanel, new Color(0.02f, 0.035f, 0.09f, 0.97f));
            DrawRectBorder(resultPanel, new Color(1f, 0.76f, 0.25f), 2f);
            GUI.Label(
                new Rect(resultPanel.x + 10f, resultPanel.y + 8f, resultPanel.width - 20f, resultPanel.height - 16f),
                matchMessage,
                titleStyle);
        }

        GUI.Label(
            new Rect(12f, 451f, 616f, 20f),
            currentMode == TetrisGameMode.LocalVersus
                ? "P1  A/D • S • W/Q • SPACE • SHIFT      P2  ARROWS • CTRL • ENTER • R-SHIFT      ESC MENU"
                 : "MOVE A/D   DOWN S   ROTATE W/Q   DROP SPACE   HOLD SHIFT   START/ESC MENU",
            helpStyle);

        DrawMatchPresentationOverlay();
        EndReferenceCanvas(previousMatrix);
    }

    private void DrawMatchPresentationOverlay()
    {
        if (matchPresentationState == MatchPresentationState.None ||
            matchPresentationState == MatchPresentationState.Result)
            return;

        DrawPixelRect(
            new Rect(0f, 0f, MenuWidth, MenuHeight),
            new Color(0.005f, 0.01f, 0.035f, 0.58f));

        bool isReady = matchPresentationState == MatchPresentationState.Ready;
        Color accent = isReady
            ? new Color(0.3f, 0.88f, 1f)
            : new Color(1f, 0.78f, 0.26f);
        float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 8f) * 0.18f;
        Rect panel = new Rect(142f, 174f, 356f, 132f);

        DrawPixelRect(panel, new Color(0.018f, 0.035f, 0.1f, 0.98f));
        DrawRectBorder(
            panel,
            new Color(accent.r, accent.g, accent.b, pulse),
            3f);
        DrawPixelRect(
            new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, 2f),
            accent);

        GUI.Label(
            new Rect(panel.x + 12f, panel.y + 20f, panel.width - 24f, 56f),
            isReady ? "READY" : "START!",
            matchCalloutStyle);

        string matchup = playerTwo == null
            ? playerOne?.DisplayName ?? "PLAYER"
            : $"{playerOne?.DisplayName ?? "P1"}  VS  {playerTwo?.DisplayName ?? "P2"}";
        GUI.Label(
            new Rect(panel.x + 18f, panel.y + 83f, panel.width - 36f, 25f),
            matchup,
            matchRoleStyle);
    }

    private void DrawSessionHud(TetrisGameSession session, float x)
    {
        string hold = session.HeldType.HasValue ? session.HeldType.Value.ToString() : "—";
        string text =
            $"{session.DisplayName}" +
            (session == playerTwo && currentMode == TetrisGameMode.VersusCpu
                ? $"  {cpuDifficulty.ToString().ToUpperInvariant()}\n"
                : "\n") +
            $"SCORE  {session.Score:N0}\n" +
            $"LINES  {session.Lines}\n" +
            $"LEVEL  {session.Level}\n" +
            $"HOLD   {hold}\n" +
            $"GARBAGE {session.PendingGarbage}";

        Rect panel = new Rect(x, 48f, 110f, 113f);
        DrawPixelRect(panel, new Color(0.025f, 0.045f, 0.11f, 0.9f));
        DrawRectBorder(panel, new Color(0.24f, 0.52f, 0.72f, 0.8f), 1f);
        GUI.Label(new Rect(x + 7f, 54f, 98f, 102f), text, hudStyle);
    }

    private void DrawNextPreview(TetrisGameSession session, float centerX)
    {
        GUI.Label(
            new Rect(centerX - 38f, 39f, 76f, 14f),
            "NEXT",
            menuFooterStyle);
        DrawTetrominoDecoration(
            session.NextType,
            new Vector2(centerX, 64f),
            6f,
            0,
            1f);
    }

    private void DrawSharedNextPreview()
    {
        GUI.Label(
            new Rect(270f, 39f, 100f, 14f),
            "NEXT",
            menuFooterStyle);
        DrawTetrominoDecoration(
            sharedPieceQueue.NextType,
            new Vector2(320f, 64f),
            6f,
            0,
            1f);
    }

    private void DrawBattleCharacterPortraits()
    {
        EnsureCharacterAssets();
        int leftCharacter = storyBattleActive ? 0 : playerOneCharacterIndex;
        int rightCharacter = storyBattleActive ? 1 : playerTwoCharacterIndex;
        BattleCharacterDefinition left = BattleCharacterRoster.Get(leftCharacter);
        BattleCharacterDefinition right = BattleCharacterRoster.Get(rightCharacter);
        Rect leftPortrait = new Rect(8f, 177f, 108f, 170f);
        Rect rightPortrait = new Rect(524f, 177f, 108f, 170f);

        DrawPixelRect(leftPortrait, new Color(0.025f, 0.035f, 0.08f, 0.95f));
        DrawPixelRect(rightPortrait, new Color(0.025f, 0.035f, 0.08f, 0.95f));
        DrawCharacterPortrait(leftCharacter, leftPortrait);
        DrawCharacterPortrait(rightCharacter, rightPortrait);
        DrawRectBorder(leftPortrait, left.Accent, 2f);
        DrawRectBorder(rightPortrait, right.Accent, 2f);

        DrawPixelRect(new Rect(8f, 347f, 108f, 25f), new Color(0.08f, 0.03f, 0.13f, 0.96f));
        DrawPixelRect(new Rect(524f, 347f, 108f, 25f), new Color(0.02f, 0.1f, 0.16f, 0.96f));
        GUI.Label(new Rect(8f, 347f, 108f, 25f), left.DisplayName, menuHeadingStyle);
        GUI.Label(new Rect(524f, 347f, 108f, 25f), right.DisplayName, menuHeadingStyle);

        if (!string.IsNullOrEmpty(resultLoserName))
        {
            DrawCharacterOutcomeBadge(new Rect(8f, 375f, 108f, 23f), resultPlayerOneWon);
            DrawCharacterOutcomeBadge(new Rect(524f, 375f, 108f, 23f), !resultPlayerOneWon);
        }
    }

    private void DrawCharacterOutcomeBadge(Rect rect, bool isWinner)
    {
        Color border = isWinner
            ? new Color(1f, 0.78f, 0.26f)
            : new Color(0.92f, 0.3f, 0.56f);
        Color background = isWinner
            ? new Color(0.11f, 0.095f, 0.025f, 0.97f)
            : new Color(0.09f, 0.035f, 0.08f, 0.97f);

        DrawPixelRect(rect, background);
        DrawRectBorder(rect, border, 1f);
        GUI.Label(rect, isWinner ? "WINNER" : "LOSER", isWinner ? matchWinnerStyle : matchLoserStyle);
    }

    private void DrawCharacterPortrait(int characterIndex, Rect rect)
    {
        BattleCharacterDefinition character = BattleCharacterRoster.Get(characterIndex);
        switch (character.Portrait)
        {
            case BattleCharacterPortrait.Lyra when storyBackdrop != null:
                GUI.DrawTextureWithTexCoords(
                    rect,
                    storyBackdrop,
                    new Rect(0f, 0f, 0.41f, 1f),
                    true);
                break;
            case BattleCharacterPortrait.Bram when storyBackdrop != null:
                GUI.DrawTextureWithTexCoords(
                    rect,
                    storyBackdrop,
                    new Rect(0.59f, 0f, 0.41f, 1f),
                    true);
                break;
            case BattleCharacterPortrait.Locked when lockedCharacterPortrait != null:
                GUI.DrawTexture(rect, lockedCharacterPortrait, ScaleMode.ScaleAndCrop);
                break;
            default:
                DrawPixelRect(rect, new Color(0.015f, 0.018f, 0.04f));
                DrawPixelRect(
                    new Rect(rect.x + rect.width * 0.33f, rect.y + rect.height * 0.2f,
                        rect.width * 0.34f, rect.width * 0.38f),
                    Color.black);
                DrawPixelRect(
                    new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.52f,
                        rect.width * 0.64f, rect.height * 0.42f),
                    Color.black);
                break;
        }
    }

    private void DrawStoryPresentation()
    {
        EnsureStoryBackdrop();
        Matrix4x4 previousMatrix = BeginReferenceCanvas();

        if (storyBackdrop != null)
            GUI.DrawTexture(new Rect(0f, 0f, MenuWidth, MenuHeight), storyBackdrop, ScaleMode.ScaleAndCrop);
        else
            DrawPixelRect(new Rect(0f, 0f, MenuWidth, MenuHeight), new Color(0.025f, 0.035f, 0.09f));

        DrawPixelRect(new Rect(0f, 0f, MenuWidth, MenuHeight), new Color(0.01f, 0.015f, 0.05f, 0.12f));

        Rect locationRibbon = new Rect(172f, 13f, 296f, 28f);
        DrawPixelRect(locationRibbon, new Color(0.025f, 0.045f, 0.12f, 0.9f));
        DrawRectBorder(locationRibbon, new Color(1f, 0.78f, 0.3f), 1f);
        GUI.Label(locationRibbon, "PROLOGUE  •  THE MOON GATE", storyLocationStyle);

        StoryLine line = GetCurrentStoryLine();
        if (line.Focus < 0)
        {
            Color lyraAccent = new Color(1f, 0.56f, 0.88f, 0.9f);
            DrawPixelRect(new Rect(13f, 80f, 3f, 180f), lyraAccent);
            DrawPixelRect(new Rect(13f, 80f, 52f, 3f), lyraAccent);
            DrawPixelRect(new Rect(13f, 257f, 52f, 3f), lyraAccent);
        }
        else if (line.Focus > 0)
        {
            Color bramAccent = new Color(0.38f, 0.88f, 1f, 0.9f);
            DrawPixelRect(new Rect(624f, 80f, 3f, 180f), bramAccent);
            DrawPixelRect(new Rect(575f, 80f, 52f, 3f), bramAccent);
            DrawPixelRect(new Rect(575f, 257f, 52f, 3f), bramAccent);
        }

        if (storyState == StoryPresentationState.Choice)
            DrawStoryChoices();

        DrawStoryDialoguePanel(line);
        EndReferenceCanvas(previousMatrix);
    }

    private StoryLine GetCurrentStoryLine()
    {
        if (storyState == StoryPresentationState.Opening)
            return OpeningStoryLines[Mathf.Clamp(storyLineIndex, 0, OpeningStoryLines.Length - 1)];

        if (storyState == StoryPresentationState.Choice)
            return new StoryLine("LYRA", "How should I answer him?", -1);

        if (storyState == StoryPresentationState.Challenge)
        {
            return storyResponse == 0
                ? new StoryLine("BRAM", "Bold. Win this duel and I’ll stand aside.", 1)
                : new StoryLine("BRAM", "Together? Earn my trust in one clean duel first.", 1);
        }

        if (storyState == StoryPresentationState.Result)
        {
            return storyBattleWon
                ? new StoryLine("BRAM", "All right. That wasn’t luck. The Moon Gate chose you.", 1)
                : new StoryLine("LYRA", "I saw the pattern too late. I can solve it if I try again.", -1);
        }

        return new StoryLine(string.Empty, string.Empty, 0);
    }

    private void DrawStoryDialoguePanel(StoryLine line)
    {
        Rect dialoguePanel = new Rect(28f, 327f, 584f, 132f);
        DrawPixelRect(dialoguePanel, new Color(0.018f, 0.026f, 0.075f, 0.96f));
        DrawRectBorder(dialoguePanel, new Color(1f, 0.78f, 0.3f), 2f);

        float nameplateX = line.Focus < 0
            ? 45f
            : line.Focus > 0
                ? MenuWidth - 45f - 154f
                : (MenuWidth - 154f) * 0.5f;
        Rect nameplate = new Rect(nameplateX, 309f, 154f, 31f);
        DrawPixelRect(
            nameplate,
            line.Focus < 0
                ? new Color(0.25f, 0.055f, 0.28f, 0.98f)
                : line.Focus > 0
                    ? new Color(0.025f, 0.18f, 0.25f, 0.98f)
                    : new Color(0.08f, 0.09f, 0.16f, 0.98f));
        DrawRectBorder(nameplate, new Color(1f, 0.78f, 0.3f), 1f);
        GUI.Label(nameplate, line.Speaker, storyNameStyle);

        GUI.Label(new Rect(50f, 345f, 540f, 58f), line.Text, storyDialogueStyle);

        if (storyState == StoryPresentationState.Result)
        {
            string primary = storyBattleWon ? "FINISH PROLOGUE" : "REMATCH";
            DrawStoryActionButton(new Rect(84f, 411f, 218f, 34f), primary, 0);
            DrawStoryActionButton(new Rect(338f, 411f, 218f, 34f), "RETURN TO MENU", 1);
        }
        else if (storyState != StoryPresentationState.Choice)
        {
            GUI.Label(
                new Rect(360f, 421f, 225f, 22f),
                storyState == StoryPresentationState.Challenge
                    ? $"ENTER BATTLE  •  CPU {cpuDifficulty.ToString().ToUpperInvariant()}"
                    : "ENTER / SPACE TO CONTINUE  ▶",
                storyPromptStyle);
        }
    }

    private void DrawStoryChoices()
    {
        Rect choicePanel = new Rect(164f, 193f, 312f, 112f);
        DrawPixelRect(choicePanel, new Color(0.018f, 0.026f, 0.075f, 0.93f));
        DrawRectBorder(choicePanel, new Color(0.42f, 0.83f, 1f), 1f);
        DrawStoryActionButton(new Rect(180f, 207f, 280f, 36f), "THEN WATCH ME PROVE IT.", 0);
        DrawStoryActionButton(new Rect(180f, 255f, 280f, 36f), "WE COULD SOLVE IT TOGETHER.", 1);
    }

    private void DrawStoryActionButton(Rect rect, string text, int selection)
    {
        GUIStyle style = storySelection == selection ? selectedMenuButtonStyle : menuButtonStyle;
        int previousFontSize = style.fontSize;
        style.fontSize = 14;
        bool clicked = GUI.Button(rect, text, style);
        style.fontSize = previousFontSize;

        if (clicked)
        {
            storySelection = selection;
            ResolveStorySelection();
        }
    }

    private void EnsureStoryBackdrop()
    {
        if (storyBackdrop == null)
            storyBackdrop = Resources.Load<Texture2D>("Story/moon_gate_duel");
    }

    private void EnsureCharacterAssets()
    {
        EnsureStoryBackdrop();
        if (lockedCharacterPortrait == null)
            lockedCharacterPortrait = Resources.Load<Texture2D>("Characters/locked_portrait");
    }

    private void DrawMainMenu()
    {
        Matrix4x4 previousMatrix = BeginReferenceCanvas();

        DrawPixelRect(new Rect(0f, 0f, MenuWidth, MenuHeight), new Color(0.018f, 0.025f, 0.07f));
        DrawPixelRect(new Rect(12f, 12f, 616f, 456f), new Color(0.05f, 0.12f, 0.22f));
        DrawPixelRect(new Rect(15f, 15f, 610f, 450f), new Color(0.018f, 0.025f, 0.07f));

        int bob = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 2f) * 2f);
        DrawTetrominoDecoration(TetriminoType.T, new Vector2(72f, 90f + bob), 18f, 0, 0.9f);
        DrawTetrominoDecoration(TetriminoType.I, new Vector2(552f, 85f - bob), 16f, 1, 0.85f);
        DrawTetrominoDecoration(TetriminoType.L, new Vector2(86f, 384f - bob), 14f, 3, 0.65f);
        DrawTetrominoDecoration(TetriminoType.S, new Vector2(548f, 386f + bob), 14f, 0, 0.65f);

        GUI.Label(new Rect(70f, 31f, 500f, 44f), "MAGICAL BLOCK ADVENTURE", menuTitleStyle);
        GUI.Label(
            new Rect(70f, 73f, 500f, 22f),
            "A STORY-DRIVEN PUZZLE BATTLE",
            menuSubtitleStyle);

        bool selectingCharacter = mainMenuPage == MainMenuPage.CharacterSelect;
        Rect panelRect = selectingCharacter
            ? new Rect(28f, 105f, 584f, 338f)
            : new Rect(142f, 112f, 356f, 316f);
        GUI.DrawTexture(panelRect, menuPanelTexture);
        DrawRectBorder(panelRect, new Color(0.95f, 0.72f, 0.24f), 2f);

        if (mainMenuPage == MainMenuPage.Root)
            DrawRootMenu();
        else if (mainMenuPage == MainMenuPage.CpuDifficulty)
            DrawCpuDifficultyMenu();
        else
            DrawCharacterSelectMenu();

        GUI.Label(
            new Rect(80f, 444f, 480f, 20f),
            selectingCharacter
                ? "LEFT / RIGHT TO CHOOSE    •    ENTER / A TO CONFIRM    •    ESC / B BACK"
                : "ARROWS / W S TO CHOOSE    •    ENTER / A TO CONFIRM    •    MOUSE SUPPORTED",
            menuFooterStyle);

        EndReferenceCanvas(previousMatrix);
    }

    private static Matrix4x4 BeginReferenceCanvas()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float widthScale = Screen.width / MenuWidth;
        float heightScale = Screen.height / MenuHeight;
        float scale = Mathf.Min(widthScale, heightScale);
        if (scale >= 1f)
            scale = Mathf.Floor(scale);

        scale = Mathf.Max(0.1f, scale);
        float offsetX = Mathf.Round((Screen.width - MenuWidth * scale) * 0.5f);
        float offsetY = Mathf.Round((Screen.height - MenuHeight * scale) * 0.5f);
        GUI.matrix = Matrix4x4.TRS(
            new Vector3(offsetX, offsetY, 0f),
            Quaternion.identity,
            new Vector3(scale, scale, 1f));
        return previousMatrix;
    }

    private static void EndReferenceCanvas(Matrix4x4 previousMatrix)
    {
        GUI.matrix = previousMatrix;
        GUI.color = Color.white;
    }

    private void DrawRootMenu()
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "CHOOSE YOUR MODE", menuHeadingStyle);
        DrawMenuButton(
            new Rect(178f, 163f, 284f, 50f),
            "STORY MODE",
            "Begin the first adventure battle",
            0);
        DrawMenuButton(
            new Rect(178f, 247f, 284f, 50f),
            "VS CPU",
            $"Choose a rival difficulty  •  Current: {cpuDifficulty.ToString().ToUpperInvariant()}",
            1);
        DrawMenuButton(
            new Rect(178f, 331f, 284f, 50f),
            "VS PLAYER",
            "Two players on one screen",
            2);
    }

    private void DrawCpuDifficultyMenu()
    {
        GUI.Label(new Rect(160f, 127f, 320f, 28f), "SELECT CPU DIFFICULTY", menuHeadingStyle);
        DrawMenuButton(new Rect(178f, 162f, 284f, 43f), "EASY", "Patient, forgiving, and imperfect", 0);
        DrawMenuButton(new Rect(178f, 226f, 284f, 43f), "NORMAL", "Steadier decisions and faster play", 1);
        DrawMenuButton(new Rect(178f, 290f, 284f, 43f), "HARD", "Fast, efficient placement", 2);
        DrawMenuButton(new Rect(178f, 362f, 284f, 38f), "BACK", string.Empty, 3);
    }

    private void DrawCharacterSelectMenu()
    {
        EnsureCharacterAssets();
        string selectingLabel = characterSelectStage == 0
            ? "PLAYER 1 — CHOOSE YOUR CHAMPION"
            : pendingVersusMode == TetrisGameMode.VersusCpu
                ? "CHOOSE THE CPU RIVAL"
                : "PLAYER 2 — CHOOSE YOUR CHAMPION";
        GUI.Label(new Rect(70f, 116f, 500f, 26f), selectingLabel, menuHeadingStyle);

        string leftStatus = characterSelectStage == 0
            ? "P1  CHOOSING"
            : $"P1  {BattleCharacterRoster.Get(playerOneCharacterIndex).DisplayName}";
        string rightStatus = characterSelectStage == 0
            ? (pendingVersusMode == TetrisGameMode.VersusCpu ? $"CPU  {cpuDifficulty.ToString().ToUpperInvariant()}" : "P2  WAITING")
            : (pendingVersusMode == TetrisGameMode.VersusCpu ? "CPU  CHOOSING" : "P2  CHOOSING");
        GUI.Label(new Rect(46f, 143f, 250f, 20f), leftStatus, characterTitleStyle);
        GUI.Label(new Rect(344f, 143f, 250f, 20f), rightStatus, characterTitleStyle);

        const float cardWidth = 78f;
        const float cardGap = 10f;
        const float cardsStartX = 61f;
        for (int i = 0; i < BattleCharacterRoster.Count; i++)
        {
            DrawCharacterSelectCard(
                i,
                new Rect(cardsStartX + i * (cardWidth + cardGap), 166f, cardWidth, 139f));
        }

        BattleCharacterDefinition selected = BattleCharacterRoster.Get(characterSelection);
        bool selectedUnlocked = BattleCharacterRoster.IsUnlocked(characterSelection);
        Rect infoPanel = new Rect(78f, 318f, 484f, 72f);
        DrawPixelRect(infoPanel, new Color(0.02f, 0.04f, 0.1f, 0.96f));
        DrawRectBorder(infoPanel, selectedUnlocked ? selected.Accent : new Color(0.48f, 0.42f, 0.64f), 1f);
        GUI.Label(
            new Rect(92f, 325f, 456f, 24f),
            selectedUnlocked ? selected.DisplayName : "LOCKED CHALLENGER",
            characterNameStyle);
        GUI.Label(
            new Rect(92f, 350f, 456f, 18f),
            selectedUnlocked ? selected.Title : "WIN ADVENTURES TO REVEAL THIS CHARACTER",
            characterTitleStyle);
        GUI.Label(
            new Rect(92f, 370f, 456f, 16f),
            string.IsNullOrEmpty(characterSelectMessage)
                ? $"SLOT {characterSelection + 1} / {BattleCharacterRoster.Count}"
                : characterSelectMessage,
            menuFooterStyle);

        int previousBackFontSize = menuButtonStyle.fontSize;
        int previousConfirmFontSize = selectedMenuButtonStyle.fontSize;
        menuButtonStyle.fontSize = 14;
        selectedMenuButtonStyle.fontSize = 14;
        if (GUI.Button(new Rect(82f, 400f, 178f, 32f), "BACK", menuButtonStyle))
            BackFromCharacterSelect();
        if (GUI.Button(new Rect(380f, 400f, 178f, 32f), "CONFIRM", selectedMenuButtonStyle))
            ConfirmCharacterSelection();
        menuButtonStyle.fontSize = previousBackFontSize;
        selectedMenuButtonStyle.fontSize = previousConfirmFontSize;
    }

    private void DrawCharacterSelectCard(int characterIndex, Rect cardRect)
    {
        BattleCharacterDefinition character = BattleCharacterRoster.Get(characterIndex);
        bool unlocked = BattleCharacterRoster.IsUnlocked(characterIndex);
        bool selected = characterSelection == characterIndex;
        bool chosenByPlayerOne =
            characterSelectStage > 0 && playerOneCharacterIndex == characterIndex;

        DrawPixelRect(
            cardRect,
            selected
                ? new Color(0.1f, 0.2f, 0.32f, 1f)
                : new Color(0.025f, 0.045f, 0.11f, 0.98f));

        Rect portraitRect = new Rect(cardRect.x + 4f, cardRect.y + 4f, cardRect.width - 8f, 105f);
        if (unlocked)
            DrawCharacterPortrait(characterIndex, portraitRect);
        else if (lockedCharacterPortrait != null)
            GUI.DrawTexture(portraitRect, lockedCharacterPortrait, ScaleMode.ScaleAndCrop);
        else
            DrawCharacterPortrait(characterIndex, portraitRect);

        Color borderColor = selected
            ? new Color(1f, 0.8f, 0.31f)
            : unlocked
                ? character.Accent
                : new Color(0.32f, 0.29f, 0.44f);
        DrawRectBorder(cardRect, borderColor, selected ? 3f : 1f);

        DrawPixelRect(
            new Rect(cardRect.x + 3f, cardRect.y + 112f, cardRect.width - 6f, 23f),
            new Color(0.015f, 0.025f, 0.075f, 0.98f));
        GUI.Label(
            new Rect(cardRect.x + 2f, cardRect.y + 112f, cardRect.width - 4f, 23f),
            unlocked ? character.DisplayName : "LOCKED",
            characterTitleStyle);

        if (chosenByPlayerOne)
        {
            Rect badge = new Rect(cardRect.x + 3f, cardRect.y + 3f, 25f, 16f);
            DrawPixelRect(badge, new Color(0.45f, 0.08f, 0.42f, 0.95f));
            GUI.Label(badge, "P1", menuFooterStyle);
        }

        if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
        {
            characterSelection = characterIndex;
            characterSelectMessage = string.Empty;
        }
    }

    private void DrawMenuButton(Rect rect, string label, string detail, int selection)
    {
        GUIStyle style = menuSelection == selection ? selectedMenuButtonStyle : menuButtonStyle;
        if (GUI.Button(rect, label, style))
        {
            menuSelection = selection;
            ActivateMenuSelection(selection);
        }

        if (!string.IsNullOrEmpty(detail))
            GUI.Label(new Rect(rect.x - 18f, rect.yMax + 3f, rect.width + 36f, 20f), detail, menuDetailStyle);
    }

    private static void DrawTetrominoDecoration(
        TetriminoType type,
        Vector2 center,
        float cellSize,
        int rotation,
        float alpha)
    {
        Vector2Int[] cells = TetrominoDefinitions.GetCells(type, rotation);
        Color pieceColor = TetrominoDefinitions.GetColor(type);
        pieceColor.a = alpha;

        for (int i = 0; i < cells.Length; i++)
        {
            float x = Mathf.Round(center.x + cells[i].x * cellSize - cellSize * 0.5f);
            float y = Mathf.Round(center.y - cells[i].y * cellSize - cellSize * 0.5f);
            Rect cellRect = new Rect(x, y, cellSize - 1f, cellSize - 1f);
            DrawPixelRect(
                new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width, cellRect.height),
                new Color(0f, 0f, 0f, alpha * 0.45f));
            DrawPixelRect(cellRect, pieceColor);
            DrawRectBorder(cellRect, new Color(1f, 1f, 1f, alpha * 0.45f), 1f);
        }
    }

    private static void DrawRectBorder(Rect rect, Color color, float thickness)
    {
        DrawPixelRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawPixelRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawPixelRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawPixelRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static void DrawPixelRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void EnsureGuiStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.82f, 0.93f, 1f) }
        };

        hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        helpStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 8,
            normal = { textColor = new Color(0.7f, 0.76f, 0.86f) }
        };

        menuPanelTexture = CreateGuiTexture(new Color(0.035f, 0.055f, 0.13f, 0.97f));
        menuButtonTexture = CreateGuiTexture(new Color(0.055f, 0.11f, 0.22f, 1f));
        menuButtonHoverTexture = CreateGuiTexture(new Color(0.1f, 0.21f, 0.34f, 1f));
        menuButtonSelectedTexture = CreateGuiTexture(new Color(0.12f, 0.32f, 0.43f, 1f));

        menuTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 30,
            normal = { textColor = new Color(1f, 0.8f, 0.31f) }
        };

        menuSubtitleStyle = new GUIStyle(helpStyle)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.44f, 0.92f, 1f) }
        };

        menuHeadingStyle = new GUIStyle(titleStyle)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.82f, 0.93f, 1f) }
        };

        menuButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                background = menuButtonTexture,
                textColor = new Color(0.88f, 0.95f, 1f)
            },
            hover =
            {
                background = menuButtonHoverTexture,
                textColor = Color.white
            },
            active =
            {
                background = menuButtonSelectedTexture,
                textColor = Color.white
            }
        };

        selectedMenuButtonStyle = new GUIStyle(menuButtonStyle);
        selectedMenuButtonStyle.normal.background = menuButtonSelectedTexture;
        selectedMenuButtonStyle.normal.textColor = new Color(1f, 0.84f, 0.38f);

        menuDetailStyle = new GUIStyle(helpStyle)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.62f, 0.72f, 0.84f) }
        };

        menuFooterStyle = new GUIStyle(helpStyle)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.52f, 0.64f, 0.76f) }
        };

        matchCalloutStyle = new GUIStyle(menuTitleStyle)
        {
            fontSize = 34
        };

        matchRoleStyle = new GUIStyle(menuFooterStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.72f, 0.87f, 1f) }
        };

        matchWinnerStyle = new GUIStyle(menuHeadingStyle)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.84f, 0.38f) }
        };

        matchLoserStyle = new GUIStyle(menuHeadingStyle)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.5f, 0.7f) }
        };

        storyLocationStyle = new GUIStyle(titleStyle)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.88f, 0.56f) }
        };

        storyNameStyle = new GUIStyle(titleStyle)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        storyDialogueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.93f, 0.96f, 1f) }
        };

        storyPromptStyle = new GUIStyle(helpStyle)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.5f, 0.9f, 1f) }
        };

        characterNameStyle = new GUIStyle(menuHeadingStyle)
        {
            fontSize = 18,
            normal = { textColor = new Color(1f, 0.84f, 0.38f) }
        };

        characterTitleStyle = new GUIStyle(menuFooterStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.76f, 0.87f, 1f) }
        };
    }

    private static Texture2D CreateGuiTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (storyBridge != null)
            storyBridge.BattleResolved -= OnStoryBattleResolved;

        Destroy(menuPanelTexture);
        Destroy(menuButtonTexture);
        Destroy(menuButtonHoverTexture);
        Destroy(menuButtonSelectedTexture);
    }
}
