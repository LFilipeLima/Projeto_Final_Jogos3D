using System;
using UnityEngine;

public class RulesTestEnemyTarget : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 2;

    public event Action<Vector3> Died;

    private int currentHealth;
    private bool isDead;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void KillImmediately()
    {
        if (!isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Vector3 deathPosition = transform.position;
        Died?.Invoke(deathPosition);
        Destroy(gameObject);
    }
}