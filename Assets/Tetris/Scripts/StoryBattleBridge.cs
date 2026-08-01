using System;
using UnityEngine;

/// <summary>
/// Stable seam between a future dialogue system (for example Yarn Spinner)
/// and the battle scene. Story code can request a battle without knowing
/// anything about board input, rendering, garbage, or CPU behavior.
/// </summary>
public sealed class StoryBattleBridge : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;

    private string activeBattleId;

    public event Action<string> BattleRequested;
    public event Action<string, bool> BattleResolved;

    public bool IsBattleActive => !string.IsNullOrEmpty(activeBattleId);

    private void Awake()
    {
        if (gameFlowController == null)
            gameFlowController = GetComponent<GameFlowController>();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.MatchEnded += OnMatchEnded;
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.MatchEnded -= OnMatchEnded;
    }

    public void RequestBattle(string battleId)
    {
        if (string.IsNullOrWhiteSpace(battleId))
            throw new ArgumentException("A battle ID is required.", nameof(battleId));

        activeBattleId = battleId;
        BattleRequested?.Invoke(battleId);
        gameFlowController?.StartBattle(TetrisGameMode.VersusCpu);
    }

    public void ReportBattleResult(string battleId, bool playerWon)
    {
        BattleResolved?.Invoke(battleId, playerWon);
    }

    public void CancelBattle()
    {
        activeBattleId = null;
    }

    private void OnMatchEnded(bool playerWon)
    {
        if (!IsBattleActive)
            return;

        string completedBattleId = activeBattleId;
        activeBattleId = null;
        ReportBattleResult(completedBattleId, playerWon);
    }
}
