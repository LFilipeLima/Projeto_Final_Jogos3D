using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Spawns each wave across the spawn points configured for the arena.</summary>
public sealed class WaveEnemyScript : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private ArenaConfiguration arena;
    [SerializeField, Min(1)] private int waves = 3;
    [SerializeField, Min(1)] private int enemiesPerWave = 4;
    [SerializeField, Min(0f)] private float delayBetweenSpawns = 0.5f;
    [SerializeField, Min(0f)] private float delayBetweenWaves = 2f;
    [SerializeField, Min(0.1f)] private float navMeshSearchRadius = 1f;

    private readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
    private int nextSpawnPointIndex;

    private void Start()
    {
        if (enemyPrefab == null || player == null || arena == null ||
            arena.EnemySpawnPoints == null || arena.EnemySpawnPoints.Count == 0)
        {
            Debug.LogError(
                "WaveEnemyScript: configure Enemy Prefab, Player, Arena e pelo menos um Enemy Spawn Point.",
                this);
            enabled = false;
            return;
        }

        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        for (int wave = 0; wave < waves; wave++)
        {
            for (int enemyIndex = 0; enemyIndex < enemiesPerWave; enemyIndex++)
            {
                SpawnEnemy();

                if (delayBetweenSpawns > 0f && enemyIndex < enemiesPerWave - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
            }

            while (HasAliveEnemies())
            {
                yield return new WaitForSeconds(0.25f);
            }

            if (wave < waves - 1 && delayBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }
    }

    private void SpawnEnemy()
    {
        IReadOnlyList<SpawnPoint> spawnPoints = arena.EnemySpawnPoints;

        // Cycle through all configured points so waves are shared across the arena.
        for (int attempt = 0; attempt < spawnPoints.Count; attempt++)
        {
            int index = nextSpawnPointIndex % spawnPoints.Count;
            nextSpawnPointIndex++;
            SpawnPoint spawnPoint = spawnPoints[index];

            if (spawnPoint == null ||
                !spawnPoint.TryGetNavMeshPosition(navMeshSearchRadius, out Vector3 spawnPosition))
            {
                continue;
            }

            EnemyController enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                spawnPoint.Rotation);

            enemy.Initialize(player);
            aliveEnemies.Add(enemy);
            return;
        }

        Debug.LogError(
            "WaveEnemyScript: nenhum Enemy Spawn Point válido foi encontrado sobre a NavMesh.",
            this);
    }

    private bool HasAliveEnemies()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null);
        return aliveEnemies.Count > 0;
    }

    private void OnValidate()
    {
        waves = Mathf.Max(1, waves);
        enemiesPerWave = Mathf.Max(1, enemiesPerWave);
        delayBetweenSpawns = Mathf.Max(0f, delayBetweenSpawns);
        delayBetweenWaves = Mathf.Max(0f, delayBetweenWaves);
        navMeshSearchRadius = Mathf.Max(0.1f, navMeshSearchRadius);
    }
}
