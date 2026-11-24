using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    private PlayerMovement movement;
    private PlayerShooter shooter;

    private EnergyCell[] scanResults = System.Array.Empty<EnergyCell>();
    private RelayCourse relay;
    private float nextScan;

    private void Update()
    {
        if (!ScannerOpen() || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.2f;
        scanResults = FindObjectsByType<EnergyCell>(FindObjectsSortMode.None);
    }

    private bool ScannerOpen() => gameManager && gameManager.IsPlaying &&
        (gameManager.Transmission || gameManager.ShowCollectRemainingPrompt || (Keyboard.current != null && Keyboard.current.qKey.isPressed));

    private void DrawScanner()
    {
        Camera view = Camera.main;
        if (!view) return;
        float size = Mathf.Min(180f, Screen.width * 0.29f, Screen.height * 0.32f);
        Rect map = new Rect(Screen.width - size - 12f, Screen.height - size - 34f, size, size);
        GUI.Box(map, "ARQUIVO 88   /   RADAR");
        Rect list = new Rect(map.x, map.y - 102f, size, 100f);
        GUILayout.BeginArea(list, GUI.skin.box);
        GUILayout.Label("Série completa: +1 vida");
        for (int wave = 1; wave <= gameManager.TotalWaves; wave++)
        {
            GUILayout.Label($"{ArenaSector.Year(wave)}    {gameManager.RecoveredIn(wave)} / {gameManager.DocumentsIn(wave)}");
        }
        GUILayout.EndArea();
        Vector2 center = map.center + Vector2.up * 6f;
        Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        void Mark(Vector3 position, Color color, float width)
        {
            Vector3 delta = position - playerHealth.transform.position;
            Vector2 offset = new Vector2(Vector3.Dot(delta, right), -Vector3.Dot(delta, forward));
            Vector2 point = center + Vector2.ClampMagnitude(offset / 27f, 1f) * (size * 0.5f - 18f);
            GUI.color = color;
            GUI.DrawTexture(new Rect(point.x-width/2, point.y-width/2, width, width), Texture2D.whiteTexture);
        }
        Color saved = GUI.color;
        foreach (var cell in scanResults)
            if (cell) Mark(cell.transform.position, ArenaSector.Ink(cell.ArchiveWave), 5f);
        if (relay && relay.ActiveRing) Mark(relay.ActiveRing.position, Color.green, 9f);
        if (gameManager.Transmission) Mark(gameManager.Transmission.transform.position, Color.green, 10f);
        Mark(playerHealth.transform.position, Color.white, 7f);
        GUI.color = saved;
        GUI.Label(new Rect(map.x, map.yMax, size, 25), gameManager.Transmission ? "Terminal • verde" : "Você • centro    Aro • verde");
    }

    private void OnGUI()
    {
        if (!gameManager || !gameManager.IsPlaying || !movement || !shooter) return;
        if (ScannerOpen()) DrawScanner();
        float height = Screen.width < 750 ? 54f : 46f;
        GUILayout.BeginArea(new Rect(10, Screen.height - height - 8, Mathf.Min(Screen.width - 20, 680), height), GUI.skin.box);
        GUILayout.Label($"Espaço • Esquiva {ReadyIn(movement.DashCooldown)}     E • Pulso {ReadyIn(shooter.PulseCooldown)}");
        GUILayout.Label($"WASD mover   ·   Clique disparar   ·   Tab câmera   ·   Roda zoom   ·   Q radar   ·   M som {(ArchiveAudio.Muted ? "off" : "on")}");
        GUILayout.EndArea();
    }

    private static string ReadyIn(float seconds) => seconds > 0 ? $"({seconds:0.0}s)" : "(pronto)";

    private void Awake()
    {
        ResolveReferences();
        if (gameManager) relay = gameManager.GetComponent<RelayCourse>();
        if (relay && victoryRestartButton)
        {
            Button finish = Instantiate(victoryRestartButton, victoryRestartButton.transform.parent);
            finish.name = "ArchiveReturnButton";
            finish.onClick = new Button.ButtonClickedEvent();
            finish.onClick.AddListener(ArchiveEpilogue.ReturnToDesk);
            finish.GetComponentInChildren<TMP_Text>(true).text = "Voltar ao balcão";
            ((RectTransform)finish.transform).anchoredPosition += Vector2.left * 145f;
            ((RectTransform)victoryRestartButton.transform).anchoredPosition += Vector2.right * 145f;
        }
        if (playerHealth)
        {
            movement = playerHealth.GetComponent<PlayerMovement>();
            shooter = playerHealth.GetComponent<PlayerShooter>();
        }
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
            waveText.text = $"{gameManager.CurrentWave}/{gameManager.TotalWaves} • {ArenaSector.Name(gameManager.CurrentWave)}";
        }

        if (enemiesText != null)
        {
            enemiesText.text = $"Drones: {gameManager.RemainingEnemies}";
        }

        if (cellsText != null)
        {
            cellsText.text = $"Fichas: {gameManager.CollectedCells}/{gameManager.TotalCells}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Pontuação: {gameManager.Score}/{gameManager.MaxScore}";
        }

        if (objectiveHintText != null)
        {
            objectiveHintText.gameObject.SetActive(gameManager.RelayHint != null || gameManager.ShowCollectRemainingPrompt);
            if (gameManager.RelayHint != null)
                objectiveHintText.text = gameManager.RelayHint;
            if (gameManager.ShowCollectRemainingPrompt)
            {
                int remaining = Mathf.Max(0, gameManager.TotalCells - gameManager.CollectedCells);
                objectiveHintText.text = $"Acervo incompleto • Fichas a recuperar: {remaining}.";
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
                string receipt = relay && relay.RemessaRetida ? "Remessa 17 retida" : "Acervo recuperado";
                victorySummaryText.text = $"{receipt} • {gameManager.Score} pontos\n1985 · {gameManager.RecoveredIn(1)} fichas    1987 · {gameManager.RecoveredIn(2)}    1988 · {gameManager.RecoveredIn(3)}";
            }

            if (isDefeat && defeatSummaryText != null)
            {
                defeatSummaryText.text = $"Transmissão interrompida.\nPontuação: {gameManager.Score}/{gameManager.MaxScore}";
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