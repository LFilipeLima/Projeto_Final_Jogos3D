using UnityEngine;

public sealed class DepotSpikeMachine : MonoBehaviour
{
    [SerializeField] private Transform spikeBank;
    [SerializeField] private BoxCollider damageZone;
    [SerializeField] private float cycleSpeed = 1.2f;
    [SerializeField] private float damageInterval = 1.1f;
    [SerializeField] private int damage = 1;

    private Vector3 retractedPosition;
    private Vector3 extendedPosition;
    private float nextDamageTime;

    public bool IsRunning { get; private set; } = true;

    private void Awake()
    {
        if (!spikeBank) spikeBank = transform.Find("Conjunto de espinhos");
        if (!damageZone) damageZone = GetComponent<BoxCollider>();
        if (spikeBank)
        {
            extendedPosition = spikeBank.localPosition;
            retractedPosition = extendedPosition + Vector3.down * .55f;
            spikeBank.localPosition = retractedPosition;
        }
    }

    private void Update()
    {
        if (!spikeBank) return;

        float phase = (Mathf.Sin(Time.time * cycleSpeed) + 1f) * .5f;
        float extension = IsRunning ? Mathf.SmoothStep(0f, 1f, phase) : 0f;
        spikeBank.localPosition = Vector3.Lerp(retractedPosition, extendedPosition, extension);

        if (IsRunning && extension > .72f && Time.time >= nextDamageTime)
        {
            DamagePlayerInZone();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    public void StopMachine() => IsRunning = false;

    private void DamagePlayerInZone()
    {
        if (!damageZone) return;

        Bounds bounds = damageZone.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation,
            Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            if (health && !health.IsDead)
            {
                health.TakeDamage(damage);
                return;
            }
        }
    }
}
