using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 2;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event System.Action<UnityEngine.Vector3> Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (CurrentHealth > 0)
        {
            return;
        }

        IsDead = true;
        Vector3 deathPosition = transform.position;
        Died?.Invoke(deathPosition);
        Destroy(gameObject);
    }
}
