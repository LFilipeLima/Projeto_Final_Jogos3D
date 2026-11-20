using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnergyCell : MonoBehaviour
{
    [Header("Animação Visual")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 2.5f;

    private Collider triggerCollider;
    private Rigidbody body;
    private Vector3 startPosition;
    private bool isCollected;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        startPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        float offsetY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = startPosition + Vector3.up * offsetY;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        GameManager manager = GameManager.Instance;
        if (manager != null && manager.IsGameOver)
        {
            return;
        }

        // Identifica exclusivamente o jogador pelo componente PlayerHealth (ignora inimigos e cenário).
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return;
        }

        Collect(manager);
    }

    private void Collect(GameManager manager)
    {
        isCollected = true;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (manager != null)
        {
            manager.RegisterCellCollected();
        }
        else
        {
            Debug.LogWarning("[EnergyCell] GameManager.Instance não encontrado ao coletar célula.");
        }

        Destroy(gameObject);
    }
}