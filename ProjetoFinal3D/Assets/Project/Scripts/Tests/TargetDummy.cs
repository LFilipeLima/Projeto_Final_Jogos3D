using UnityEngine;

public class TargetDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int health = 2;

    public void TakeDamage(int amount)
    {
        if (health <= 0 || amount <= 0)
            return;

        health = Mathf.Max(0, health - amount);
        Debug.Log($"Alvo: {health}/2", this);

        if (health == 0)
            Destroy(gameObject);
    }
}