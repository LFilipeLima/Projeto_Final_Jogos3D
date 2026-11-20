using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArenaHUD : MonoBehaviour
{
    [Header("Referências Principais")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Elementos da HUD")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private TMP_Text cellsText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text objectiveHintText;

    [Header("Telas de Encerramento")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TMP_Text victorySummaryText;
    [SerializeField] private TMP_Text defeatSummaryText;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button defeatRestartButton;

    private bool isSubscribed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
        RefreshAll();
    }

    private void Start()
    {
        // Garante leitura após todos os Awake() terem sido executados (ex.: PlayerHealth.Awake).
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>();
        }

        if (playerHealth == null)
        {
            if (gameManager != null && gameManager.PlayerHealth != null)
            {
                playerHealth = gameManager.PlayerHealth;
            }
            else
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }

        if (gameManager != null)
        {
            gameManager.StatsChanged += HandleStatsChanged;
            gameManager.GameStateChanged += HandleGameStateChanged;
        }

        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.AddListener(HandleRestartClicked);
        }

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }

        if (gameManager != null)
        {
            gameManager.StatsChanged -= HandleStatsChanged;
            gameManager.GameStateChanged -= HandleGameStateChanged;
        }

        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        isSubscribed = false;
    }

    public void RefreshAll()
    {
        if (playerHealth != null)
        {
            UpdateHealthText(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            UpdateHealthText(0, 0);
        }

        HandleStatsChanged();

        if (gameManager != null)
        {
            HandleGameStateChanged(gameManager.CurrentState);
        }
        else
        {
            SetEndPanels(victoryActive: false, defeatActive: false);
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        UpdateHealthText(current, max);
    }

    private void UpdateHealthText(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"Vida: {current}/{max}";
        }
    }

    private void HandleStatsChanged()
    {
        if (gameManager == null)
        {
            return;
        }

        if (waveText != null)
        {
            waveText.text = $"Onda: {gameManager.CurrentWave}/{gameManager.TotalWaves}";
        }

        if (enemiesText != null)
        {
            enemiesText.text = $"Inimigos: {gameManager.RemainingEnemies}";
        }

        if (cellsText != null)
        {
            cellsText.text = $"Células: {gameManager.CollectedCells}/{gameManager.TotalCells}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Pontuação: {gameManager.Score}/{gameManager.MaxScore}";
        }

        if (objectiveHintText != null)
        {
            objectiveHintText.gameObject.SetActive(gameManager.ShowCollectRemainingPrompt);
            if (gameManager.ShowCollectRemainingPrompt)
            {
                int remaining = Mathf.Max(0, gameManager.TotalCells - gameManager.CollectedCells);
                objectiveHintText.text = $"Última onda concluída! Colete as {remaining} célula(s) restante(s).";
            }
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        bool isVictory = newState == GameState.Victory;
        bool isDefeat = newState == GameState.Defeat;

        SetEndPanels(isVictory, isDefeat);

        if (gameManager != null)
        {
            if (isVictory && victorySummaryText != null)
            {
                victorySummaryText.text = $"Pontuação Final: {gameManager.Score}/{gameManager.MaxScore}";
            }

            if (isDefeat && defeatSummaryText != null)
            {
                defeatSummaryText.text = $"Pontuação: {gameManager.Score}/{gameManager.MaxScore}";
            }
        }
    }

    private void SetEndPanels(bool victoryActive, bool defeatActive)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(victoryActive);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(defeatActive);
        }
    }

    private void HandleRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }
}