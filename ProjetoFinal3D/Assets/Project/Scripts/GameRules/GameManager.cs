using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referências do Jogador")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Componentes desativados automaticamente ao encerrar a partida (ex.: PlayerMovement, PlayerShooter, PlayerCamera).")]
    [SerializeField] private MonoBehaviour[] playerControlScripts;

    [Header("Configurações de Estado")]
    [SerializeField] private bool lockCursorDuringPlay = false;

    public const int TotalWavesCount = 3;
    public const int TotalEnemiesCount = 9;
    public const int TotalCellsCount = 9;
    public const int PointsPerCell = 100;
    public const int MaxPossibleScore = TotalCellsCount * PointsPerCell;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsGameOver => CurrentState != GameState.Playing;

    public PlayerHealth PlayerHealth => playerHealth;

    public int CurrentWave { get; private set; } = 0;
    public int TotalWaves => TotalWavesCount;
    public int RemainingEnemies { get; private set; } = 0;
    public int DefeatedEnemies { get; private set; } = 0;
    public int CollectedCells { get; private set; } = 0;
    public int TotalCells => TotalCellsCount;
    public int Score { get; private set; } = 0;
    public int MaxScore => MaxPossibleScore;
    public bool ShowCollectRemainingPrompt { get; private set; } = false;

    public event Action<GameState> GameStateChanged;
    public event Action StatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Instância duplicada detectada e destruída.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("[GameManager] PlayerHealth não encontrado na cena.");
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        NotifyStatsChanged();

        if (playerHealth != null && playerHealth.IsDead)
        {
            SetState(GameState.Defeat, forceNotify: true);
            return;
        }

        SetState(GameState.Playing, forceNotify: true);
    }

    public void RegisterCellCollected()
    {
        if (IsGameOver)
        {
            return;
        }

        if (CollectedCells >= TotalCellsCount)
        {
            Debug.LogWarning("[GameManager] Tentativa de registar célula além do limite máximo de 9.");
            return;
        }

        CollectedCells++;
        Score = CollectedCells * PointsPerCell;

        Debug.Log($"[GameManager] Célula coletada ({CollectedCells}/{TotalCellsCount}). Pontuação: {Score}/{MaxPossibleScore}");
        NotifyStatsChanged();
        CheckVictoryCondition();
    }

    public void TriggerVictory()
    {
        if (IsGameOver)
        {
            return;
        }

        ShowCollectRemainingPrompt = false;
        NotifyStatsChanged();
        SetState(GameState.Victory);
    }

    public void TriggerDefeat()
    {
        if (IsGameOver)
        {
            return;
        }

        ShowCollectRemainingPrompt = false;
        NotifyStatsChanged();
        SetState(GameState.Defeat);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    protected void CheckVictoryCondition()
    {
        if (IsGameOver)
        {
            return;
        }

        if (DefeatedEnemies >= TotalEnemiesCount && CollectedCells >= TotalCellsCount)
        {
            TriggerVictory();
        }
    }

    protected void NotifyStatsChanged()
    {
        StatsChanged?.Invoke();
    }

    private void HandlePlayerDied()
    {
        TriggerDefeat();
    }

    private void SetState(GameState newState, bool forceNotify = false)
    {
        if (!forceNotify && CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        ApplyStateBehavior(newState);

        Debug.Log($"[GameManager] Estado alterado para: {CurrentState}");
        GameStateChanged?.Invoke(CurrentState);
    }

    private void ApplyStateBehavior(GameState state)
    {
        bool playing = state == GameState.Playing;

        if (playerControlScripts != null)
        {
            for (int i = 0; i < playerControlScripts.Length; i++)
            {
                if (playerControlScripts[i] != null)
                {
                    playerControlScripts[i].enabled = playing;
                }
            }
        }

        if (playing && lockCursorDuringPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Teste/Simular Derrota")]
    private void DebugSimulateDefeat()
    {
        TriggerDefeat();
    }

    [ContextMenu("Teste/Simular Vitória")]
    private void DebugSimulateVictory()
    {
        TriggerVictory();
    }
#endif
}