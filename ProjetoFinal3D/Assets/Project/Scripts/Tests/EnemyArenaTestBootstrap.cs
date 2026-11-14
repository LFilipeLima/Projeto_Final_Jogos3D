using UnityEngine;

/// <summary>Initializes one enemy only in the isolated EnemyArenaTest scene.</summary>
public sealed class EnemyArenaTestBootstrap : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private EnemyController enemy;

    private EnemyHealth enemyHealth;
    private int deathNotifications;

    private void Start()
    {
        if (player == null || enemy == null)
        {
            Debug.LogError("EnemyArenaTestBootstrap: configure Player e Enemy.", this);
            return;
        }

        enemyHealth = enemy.GetComponent<EnemyHealth>();
        enemyHealth.Died += OnEnemyDied;
        enemy.Initialize(player);
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Died -= OnEnemyDied;
        }
    }

    private void OnEnemyDied(Vector3 position)
    {
        deathNotifications++;
        Debug.Log($"Enemy Died #{deathNotifications} em {position}", this);
        if (deathNotifications > 1)
        {
            Debug.LogError("EnemyHealth.Died foi emitido mais de uma vez.", this);
        }
    }
}
