using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float range = 30f;
    [SerializeField] private float shotInterval = 0.3f;
    [SerializeField] private float tracerDuration = 0.06f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Camera aimCamera;
    private PlayerCamera cameraRig;
    private LineRenderer tracer;
    private float nextShotTime;
    private float nextPulse;
    public event System.Action<Vector3,float> Pulsed;
    public float PulseCooldown => Mathf.Max(0f, nextPulse - Time.time);
    private float hideTracerTime;

    private void Awake()
    {
        tracer = GetComponent<LineRenderer>();

        tracer.useWorldSpace = true;
        tracer.positionCount = 2;
        tracer.startWidth = 0.04f;
        tracer.endWidth = 0.04f;
        tracer.enabled = false;

        if (firePoint == null)
        {
            Debug.LogError(
                "PlayerShooter: arraste o FirePoint para o campo Fire Point.",
                this
            );

            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (tracer.enabled && Time.time >= hideTracerTime)
        {
            tracer.enabled = false;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && Time.time >= nextPulse)
            Pulse();

        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
            if (aimCamera) cameraRig = aimCamera.GetComponent<PlayerCamera>();
        }
        if ((!cameraRig || !cameraRig.FollowHeading) && !mouse.rightButton.isPressed)
            AimAtCursor(mouse.position.ReadValue());

        if (mouse.leftButton.isPressed && Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + shotInterval;
        }
    }

    private void AimAtCursor(Vector2 screenPosition)
    {
        if (aimCamera == null)
            aimCamera = Camera.main;
        if (aimCamera == null)
            return;

        Ray cursorRay = aimCamera.ScreenPointToRay(screenPosition);
        Plane weaponPlane = new Plane(Vector3.up, firePoint.position);
        if (!weaponPlane.Raycast(cursorRay, out float distance))
            return;

        Vector3 direction = cursorRay.GetPoint(distance) - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnGUI()
    {
        if (!cameraRig || !cameraRig.FollowHeading || Event.current.type != EventType.Repaint) return;
        Vector2 center = aimCamera.pixelRect.center;
        float y = Screen.height - center.y;
        GUI.DrawTexture(new Rect(center.x - 6f, y - 1f, 12f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(center.x - 1f, y - 6f, 2f, 12f), Texture2D.whiteTexture);
    }

    private void Pulse()
    {
        const float radius = 3.5f;
        nextPulse = Time.time + 4f;
        ArchiveAudio.Play(ArchiveAudio.Cue.Pulse);
        Vector3 center = transform.position;
        Pulsed?.Invoke(center, radius);
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            Vector3 delta = enemy.GetComponent<Collider>().bounds.center - center;
            if (delta.sqrMagnitude > radius * radius) continue;
            if (Physics.Raycast(center, delta.normalized, out RaycastHit hit, delta.magnitude, hitMask, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<EnemyHealth>() != enemy) continue;
            enemy.TakeDamage(1);
        }
        ArenaSignal.Ring(center, radius, tracer.sharedMaterial, Color.cyan, 0.25f);
    }

    private void Shoot()
    {
        Vector3 origin = firePoint.position;
        Vector3 rayOrigin = origin - Vector3.Project(origin - transform.position, firePoint.forward);
        Vector3 target = origin + firePoint.forward * range;
        if (aimCamera)
        {
            Ray sight = cameraRig && cameraRig.FollowHeading
                ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f))
                : aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            float sightRange = range + Vector3.Distance(sight.origin, origin);
            target = FirstImpact(sight, sightRange, out RaycastHit sightHit)
                ? sightHit.point : sight.GetPoint(sightRange);
        }
        Vector3 direction = (target - rayOrigin).normalized;
        float reach = range + Vector3.Distance(origin, rayOrigin);
        Vector3 endPoint = rayOrigin + direction * reach;
        if (FirstImpact(new Ray(rayOrigin, direction), reach, out RaycastHit hit))
        {
            endPoint = hit.point;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(1);
            }

            Debug.Log("Tiro acertou: " + hit.collider.name, hit.collider);
        }

        ArchiveAudio.Play(ArchiveAudio.Cue.Shot);
        tracer.SetPosition(0, origin);
        tracer.SetPosition(1, endPoint);
        tracer.enabled = true;

        hideTracerTime = Time.time + tracerDuration;
    }

    private bool FirstImpact(Ray ray, float distance, out RaycastHit first)
    {
        first = default;
        float closest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, distance, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.distance >= closest) continue;
            first = hit;
            closest = hit.distance;
        }
        return first.collider != null;
    }

    private void OnDisable()
    {
        if (tracer != null)
        {
            tracer.enabled = false;
        }
    }
}
