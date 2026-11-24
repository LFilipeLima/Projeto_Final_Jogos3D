using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-20)]
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float lookHeight = 0.5f;

    private Vector3 followVelocity;
    private float yaw;
    private float zoom = 1f;
    private float pitch;
    public bool FollowHeading { get; private set; } = true;

    private void Update()
    {
        if (!target || (GameManager.Instance != null && !GameManager.Instance.IsPlaying)) return;
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            FollowHeading = !FollowHeading;

        Cursor.lockState = FollowHeading ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !FollowHeading;
        Vector2 delta = mouse.delta.ReadValue();
        if (FollowHeading)
        {
            target.Rotate(Vector3.up, delta.x * 0.20f, Space.World);
            yaw = target.eulerAngles.y;
            pitch = Mathf.Clamp(pitch - delta.y * 0.12f, -55f, 65f);
        }
        else if (mouse.rightButton.isPressed)
            yaw += delta.x * 0.22f;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "PlayerCamera: arraste o Player para o campo Target.",
                this
            );

            enabled = false;
            return;
        }

        // Começa na posição correta, sem viajar desde a posição do editor.
        yaw = target.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.position = target.position + Vector3.up * lookHeight + transform.rotation * new Vector3(0.85f, 0.35f, -3.4f);
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && (GameManager.Instance == null || GameManager.Instance.IsPlaying))
        {
            zoom = Mathf.Clamp(zoom - mouse.scroll.ReadValue().y * 0.05f, 0.65f, 1.3f);
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                yaw = target.eulerAngles.y;
        }
        Vector3 focus = target.position + Vector3.up * lookHeight;
        Quaternion view = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 arm = FollowHeading
            ? view * new Vector3(0.85f, 0.35f, -3.4f * zoom)
            : Quaternion.Euler(0f, yaw, 0f) * (offset - Vector3.up * lookHeight) * zoom;
        Vector3 desiredPosition = focus + arm;
        int scenery = LayerMask.GetMask("Default");
        foreach (RaycastHit hit in Physics.SphereCastAll(focus, 0.2f, arm.normalized, arm.magnitude, scenery, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<EnemyHealth>() || hit.collider.GetComponentInParent<PlayerHealth>()) continue;
            float distance = Mathf.Max(0.4f, hit.distance - 0.15f);
            if (distance < Vector3.Distance(focus, desiredPosition))
                desiredPosition = focus + arm.normalized * distance;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime
        );

        if (FollowHeading) transform.rotation = view;
        else UpdateRotation();
    }

    private void UpdateRotation()
    {
        // Mantém o personagem enquadrado durante o giro e o deslocamento.
        Vector3 viewDirection = target.position + Vector3.up * lookHeight - transform.position;

        if (viewDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(viewDirection);
        }
    }
}