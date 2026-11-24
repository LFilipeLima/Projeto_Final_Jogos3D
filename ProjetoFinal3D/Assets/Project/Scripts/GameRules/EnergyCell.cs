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
    public int ArchiveWave { get; private set; }

    public void Catalog(int wave)
    {
        ArchiveWave = wave;
        gameObject.name = $"Ficha de {ArenaSector.Year(wave)}";
        var renderer = GetComponent<MeshRenderer>();
        if (!renderer) return;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", ArenaSector.Ink(wave));
        renderer.SetPropertyBlock(block);
        var stamp = new GameObject("Carimbo");
        stamp.transform.SetParent(transform, false);
        stamp.transform.localPosition = new Vector3(0f, 0f, -0.52f);
        var text = stamp.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = 0.031f;
        text.color = new Color(0.05f, 0.09f, 0.12f);
        text.text = $"ARQUIVO 88\n{ArenaSector.Year(wave)}";
    }

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
            if (ArchiveWave > 0) manager.RegisterDocument(ArchiveWave);
            else manager.RegisterCellCollected();
        }
        else
        {
            Debug.LogWarning("[EnergyCell] GameManager.Instance não encontrado ao coletar célula.");
        }

        Destroy(gameObject);
    }
}