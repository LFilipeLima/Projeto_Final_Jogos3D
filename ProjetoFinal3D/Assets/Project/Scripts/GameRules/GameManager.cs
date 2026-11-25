using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referências do Jogador")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Componentes desativados automaticamente ao encerrar a partida (ex.: PlayerMovement, PlayerShooter, PlayerCamera).")]
    [SerializeField] private MonoBehaviour[] playerControlScripts;

    [Header("Ondas e Spawns")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject energyCellPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float delayBetweenWaves = 3f;
    [SerializeField] private float minCellSpawnHeight = 0.6f;

    [Header("Configurações de Estado")]
    [SerializeField] private bool lockCursorDuringPlay = false;

    public const int TotalWavesCount = 3;
    public const int TotalEnemiesCount = 15;
    public const int TotalCellsCount = TotalEnemiesCount;
    public const int PointsPerCell = 100;
    public const int MaxPossibleScore = TotalCellsCount * PointsPerCell;

    private static readonly int[] WaveEnemyCounts = { 3, 5, 7 };

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

    private readonly HashSet<int> processedEnemyIds = new HashSet<int>();
    private readonly List<Action> cleanupSubscriptions = new List<Action>();
    private Coroutine waveRoutine;
    private GameObject sector;
    private RelayCourse relayCourse;
    public ArchiveTransmission Transmission { get; private set; }
    public string RelayHint
    {
        get
        {
            if (relayCourse == null || !IsPlaying) return null;
            if (Transmission) return "Terminal verde no radar • Aproxime-se e segure R para transmitir.";
            if (relayCourse.Hint != null) return relayCourse.Hint;
            if (CurrentWave == 1 && RemainingEnemies > 0)
                return "Arquivo 88 • Desative os drones para abrir a rota de recuperação.";
            return null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Instância duplicada detectada e destruída.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.AddComponent<ArchiveAudio>();
        relayCourse = GetComponent<RelayCourse>();
        if (!relayCourse && gameObject.scene.path == "Assets/Project/Scenes/MainArena.unity")
            relayCourse = gameObject.AddComponent<RelayCourse>();
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
        StopWaveRoutine();
        CleanupEnemySubscriptions();

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
        StartWave(1);
    }

    private readonly int[] recoveredByWave = new int[TotalWavesCount];
    public int RecoveredIn(int wave) => recoveredByWave[wave - 1];
    public int DocumentsIn(int wave) => WaveEnemyCounts[wave - 1];

    public void RegisterCellCollected() => RegisterDocument(CurrentWave);

    public void RegisterDocument(int wave)
    {
        if (IsGameOver)
        {
            return;
        }

        if (CollectedCells >= TotalCellsCount)
        {
            Debug.LogWarning("[GameManager] Tentativa de registar célula além do limite máximo.");
            return;
        }

        if (wave < 1 || wave > TotalWavesCount) return;
        recoveredByWave[wave - 1]++;
        ArchiveAudio.Play(recoveredByWave[wave - 1] == DocumentsIn(wave)
            ? ArchiveAudio.Cue.Series : ArchiveAudio.Cue.Document);
        if (recoveredByWave[wave - 1] == DocumentsIn(wave) && playerHealth)
            playerHealth.RecoverOne();
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

        StopWaveRoutine();
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

        StopWaveRoutine();
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

    private void StartWave(int waveNumber)
    {
        if (IsGameOver)
        {
            return;
        }

        if (waveNumber < 1 || waveNumber > TotalWavesCount)
        {
            Debug.LogWarning($"[GameManager] Número de onda inválido ignorado: {waveNumber}");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[GameManager] Enemy Prefab não atribuído no Inspector.");
            return;
        }

        CurrentWave = waveNumber;
        if (relayCourse && playerHealth)
        {
            if (sector) Destroy(sector);
            sector = ArenaSector.Build(waveNumber, playerHealth);
        }
        int countToSpawn = WaveEnemyCounts[waveNumber - 1];
        RemainingEnemies = countToSpawn;

        Debug.Log($"[GameManager] Iniciando Onda {CurrentWave}/{TotalWavesCount} com {countToSpawn} inimigo(s).");

        for (int i = 0; i < countToSpawn; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, countToSpawn);
            Quaternion spawnRot = GetSpawnRotation(spawnPos);
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPos, spawnRot);
            enemyInstance.name = $"{enemyPrefab.name}_Onda{CurrentWave}_{i + 1}";

            BindEnemyContracts(enemyInstance);
            if (relayCourse && playerHealth && enemyInstance.GetComponent<EnemyController>())
            {
                int model = i % 3;
                if (model != 0)
                    enemyInstance.AddComponent<SpecialEnemy>().Configure(playerHealth, model == 1);
                if (enemyInstance.GetComponentInChildren<EnemyHitFeedback>())
                    enemyInstance.AddComponent<ArchiveDroneModel>().Assemble(model);
            }
        }

        NotifyStatsChanged();
    }

    private Vector3 GetSpawnPosition(int index, int totalInWave)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[index % spawnPoints.Length];
            if (point != null)
            {
                // Pequeno deslocamento lateral caso a onda tenha mais inimigos que pontos de spawn.
                int lap = index / spawnPoints.Length;
                if (lap == 0)
                {
                    return point.position;
                }

                float angle = lap * 45f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (1.2f * lap);
                return point.position + offset;
            }
        }

        Debug.LogWarning("[GameManager] Nenhum SpawnPoint configurado; usando posições circulares padrão.");
        float fallbackAngle = (index / (float)Mathf.Max(1, totalInWave)) * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(fallbackAngle) * 6f, 1f, Mathf.Sin(fallbackAngle) * 6f);
    }

    private Quaternion GetSpawnRotation(Vector3 spawnPos)
    {
        if (playerHealth == null)
        {
            return Quaternion.identity;
        }

        Vector3 toPlayer = playerHealth.transform.position - spawnPos;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            return Quaternion.LookRotation(toPlayer.normalized);
        }

        return Quaternion.identity;
    }

    private void BindEnemyContracts(GameObject enemyInstance)
    {
        Transform playerTransform = playerHealth != null ? playerHealth.transform : null;

        // 1. Contrato EnemyController.Initialize(Transform player)
        Component enemyController = enemyInstance.GetComponent("EnemyController");
        if (enemyController != null)
        {
            MethodInfo initMethod = enemyController.GetType().GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Transform) },
                null
            );

            if (initMethod != null)
            {
                initMethod.Invoke(enemyController, new object[] { playerTransform });
            }
            else
            {
                Debug.LogWarning("[GameManager] EnemyController encontrado, mas sem método público Initialize(Transform).");
            }
        }

        // 2. Contrato EnemyHealth.Died (event Action<Vector3>)
        Component enemyHealth = enemyInstance.GetComponent("EnemyHealth");
        if (enemyHealth != null)
        {
            EventInfo diedEvent = enemyHealth.GetType().GetEvent("Died", BindingFlags.Instance | BindingFlags.Public);
            if (diedEvent != null && diedEvent.EventHandlerType == typeof(Action<Vector3>))
            {
                Action<Vector3> handler = deathPos => HandleEnemyDied(enemyInstance, deathPos);
                diedEvent.AddEventHandler(enemyHealth, handler);
                cleanupSubscriptions.Add(() =>
                {
                    if (enemyHealth != null)
                    {
                        diedEvent.RemoveEventHandler(enemyHealth, handler);
                    }
                });
                return;
            }

            Debug.LogWarning("[GameManager] EnemyHealth encontrado, mas sem evento público Action<Vector3> Died.");
        }

        // 3. Fallback isolado exclusivo para testes na cena RulesTest
        RulesTestEnemyTarget testTarget = enemyInstance.GetComponent<RulesTestEnemyTarget>();
        if (testTarget != null)
        {
            Action<Vector3> handler = deathPos => HandleEnemyDied(enemyInstance, deathPos);
            testTarget.Died += handler;
            cleanupSubscriptions.Add(() =>
            {
                if (testTarget != null)
                {
                    testTarget.Died -= handler;
                }
            });
            return;
        }

        Debug.LogError($"[GameManager] O prefab '{enemyInstance.name}' não possui EnemyHealth nem RulesTestEnemyTarget.", enemyInstance);
    }

    private void HandleEnemyDied(GameObject enemyInstance, Vector3 deathPosition)
    {
        if (IsGameOver)
        {
            return;
        }

        if (enemyInstance != null)
        {
            int instanceId = enemyInstance.GetInstanceID();
            if (!processedEnemyIds.Add(instanceId))
            {
                return;
            }
        }

        RemainingEnemies = Mathf.Max(0, RemainingEnemies - 1);
        DefeatedEnemies++;

        SpawnEnergyCell(deathPosition);
        NotifyStatsChanged();

        Debug.Log($"[GameManager] Inimigo derrotado ({DefeatedEnemies}/{TotalEnemiesCount}). Restantes na onda {CurrentWave}: {RemainingEnemies}");

        if (RemainingEnemies == 0)
        {
            OnCurrentWaveCleared();
        }
    }

    private void SpawnEnergyCell(Vector3 deathPosition)
    {
        if (energyCellPrefab == null)
        {
            Debug.LogError("[GameManager] Energy Cell Prefab não atribuído no Inspector.");
            return;
        }

        Vector3 spawnPos = deathPosition;
        if (spawnPos.y < minCellSpawnHeight)
        {
            spawnPos.y = minCellSpawnHeight;
        }

        GameObject pickup = Instantiate(energyCellPrefab, spawnPos, Quaternion.identity);
        if (pickup.TryGetComponent(out EnergyCell document))
            document.Catalog(CurrentWave);
    }

    private void OnCurrentWaveCleared()
    {
        if (IsGameOver)
        {
            return;
        }

        if (CurrentWave < TotalWavesCount)
        {
            int nextWave = CurrentWave + 1;
            Debug.Log($"[GameManager] Onda {CurrentWave} concluída! Preparando onda {nextWave}.");
            StopWaveRoutine();
            waveRoutine = StartCoroutine(StartNextWaveAfterDelay(nextWave));
            return;
        }

        // Última onda (Onda 3) concluída: não iniciar onda extra.
        if (CollectedCells < TotalCellsCount)
        {
            ShowCollectRemainingPrompt = true;
            NotifyStatsChanged();
        }

        CheckVictoryCondition();
    }

    private IEnumerator StartNextWaveAfterDelay(int nextWave)
    {
        if (relayCourse != null)
        {
            yield return relayCourse.Run(CurrentWave, playerHealth, spawnPoints, NotifyStatsChanged);
            if (relayCourse.Hint != null) yield break;
        }

        yield return new WaitForSeconds(delayBetweenWaves);
        waveRoutine = null;

        if (IsGameOver)
        {
            yield break;
        }

        StartWave(nextWave);
    }

    private void StopWaveRoutine()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    private void CleanupEnemySubscriptions()
    {
        for (int i = 0; i < cleanupSubscriptions.Count; i++)
        {
            cleanupSubscriptions[i]?.Invoke();
        }
        cleanupSubscriptions.Clear();
        processedEnemyIds.Clear();
    }

    protected void CheckVictoryCondition()
    {
        if (IsGameOver)
        {
            return;
        }

        if (DefeatedEnemies >= TotalEnemiesCount && CollectedCells >= TotalCellsCount)
        {
            if (!relayCourse || !relayCourse.RemessaRetida)
            {
                TriggerVictory();
                return;
            }
            if (Transmission) return;
            var terminal = new GameObject("Terminal da remessa 17");
            terminal.transform.SetParent(transform);
            Transmission = terminal.AddComponent<ArchiveTransmission>();
            Vector3 parcel = relayCourse.RetainedParcel.position;
            Transmission.Activate(this, parcel + Vector3.right * 1.3f, TriggerVictory);
            ShowCollectRemainingPrompt = false;
            NotifyStatsChanged();
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
        if (!playing && relayCourse != null)
        {
            relayCourse.Clear();
            NotifyStatsChanged();
        }

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
    [ContextMenu("Teste/Derrotar Todos os Inimigos da Onda Atual")]
    private void DebugClearCurrentWave()
    {
        if (IsGameOver) return;

        RulesTestEnemyTarget[] targets = FindObjectsByType<RulesTestEnemyTarget>(FindObjectsSortMode.None);
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].KillImmediately();
        }
    }

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