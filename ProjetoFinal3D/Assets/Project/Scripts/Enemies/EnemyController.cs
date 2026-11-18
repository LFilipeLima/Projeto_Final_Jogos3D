using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
public sealed class EnemyController : MonoBehaviour
{
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackInterval = 1.1f;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private Transform player;
    private PlayerHealth playerHealth;

    private float nextPathTime;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
    }

    public void Initialize(Transform player)
    {
        this.player = player;
        playerHealth = player != null
            ? player.GetComponentInParent<PlayerHealth>()
            : null;

        nextPathTime = Time.time;
        nextAttackTime = Time.time;
    }

    private void Update()
    {
        if (health.IsDead || player == null || playerHealth == null ||
            playerHealth.IsDead || !agent.isOnNavMesh)
        {
            StopPursuit();
            return;
        }

        if (Time.time >= nextPathTime)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            nextPathTime = Time.time + repathInterval;
        }

        if (Time.time < nextAttackTime || agent.pathPending ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            agent.remainingDistance > attackRange)
        {
            return;
        }

        Vector3 separation = player.position - transform.position;
        separation.y = 0f;

        if (separation.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        if (agent.Raycast(player.position, out _))
        {
            return;
        }

        playerHealth.TakeDamage(1);
        nextAttackTime = Time.time + attackInterval;

        if (playerHealth.IsDead)
        {
            StopPursuit();
        }
    }

    private void StopPursuit()
    {
        if (agent.isOnNavMesh && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void OnValidate()
    {
        repathInterval = Mathf.Max(0.05f, repathInterval);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackInterval = Mathf.Max(0.1f, attackInterval);
    }
}
