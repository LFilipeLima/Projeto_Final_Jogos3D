using UnityEngine;
using UnityEngine.AI;

public sealed class SpecialEnemy : MonoBehaviour
{
    private NavMeshAgent agent;
    private PlayerHealth player;
    private Material signalMaterial;
    private bool gunner;
    private AudioSource warningSpeaker;
    private LineRenderer warning;
    private Vector3 origin;
    private Vector3 direction;
    private float attackTime;
    private float nextAttack;
    private const float SlamRadius = 3f;

    public void Configure(PlayerHealth target, bool ranged)
    {
        player = target;
        gunner = ranged;
        warningSpeaker = gameObject.AddComponent<AudioSource>();
        warningSpeaker.playOnAwake = false;
        warningSpeaker.spatialBlend = 0.8f;
        warningSpeaker.minDistance = 5f;
        warningSpeaker.maxDistance = 25f;
        warningSpeaker.dopplerLevel = 0f;
        agent = GetComponent<NavMeshAgent>();
        GetComponent<EnemyController>().enabled = false;
        agent.stoppingDistance = ranged ? 7f : 1.7f;
        agent.speed = ranged ? 2f : 2.5f;
        signalMaterial = target.GetComponent<LineRenderer>().sharedMaterial;
        MeshRenderer body = GetComponentInChildren<MeshRenderer>();
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", ranged ? Color.magenta : Color.red);
        body.SetPropertyBlock(block);
        body.transform.localScale = ranged ? new Vector3(0.8f, 1.25f, 0.8f) : new Vector3(1.35f, 1f, 1.35f);
        nextAttack = Time.time + 1f;
    }

    private void Update()
    {
        if (!player || player.IsDead || !GameManager.Instance.IsPlaying)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            enabled = false;
            return;
        }
        if (!agent.isOnNavMesh) return;
        if (warning)
        {
            if (Time.time >= attackTime) ResolveAttack();
            return;
        }

        Vector3 delta = player.transform.position - transform.position;
        delta.y = 0f;
        bool clear = !agent.Raycast(player.transform.position, out _);
        agent.stoppingDistance = gunner && clear ? 7f : 1.7f;
        agent.isStopped = false;
        agent.SetDestination(player.transform.position);
        if (!clear || Time.time < nextAttack || delta.magnitude > (gunner ? 9f : 2.6f)) return;

        agent.isStopped = true;
        origin = GetComponent<Collider>().bounds.center;
        direction = delta.normalized;
        ArchiveAudio.Play(gunner ? ArchiveAudio.Cue.Gunner : ArchiveAudio.Cue.Heavy, warningSpeaker);
        attackTime = Time.time + (gunner ? 0.95f : 0.8f);
        if (gunner)
        {
            warning = ArenaSignal.Line(signalMaterial, Color.magenta, 2);
            warning.SetPosition(0, origin);
            warning.SetPosition(1, origin + direction * 12f);
        }
        else
            warning = ArenaSignal.Ring(transform.position + Vector3.up * 0.12f, SlamRadius, signalMaterial, Color.red, 2f);
    }

    private void ResolveAttack()
    {
        if (gunner)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerHealth>() == player)
                player.TakeDamage(1);
        }
        else
        {
            Vector3 delta = player.transform.position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude <= SlamRadius * SlamRadius && !agent.Raycast(player.transform.position, out _))
                player.TakeDamage(1);
        }
        Destroy(warning.gameObject);
        warning = null;
        nextAttack = Time.time + 2f;
    }

    private void OnDisable()
    {
        if (warning) Destroy(warning.gameObject);
    }
}
