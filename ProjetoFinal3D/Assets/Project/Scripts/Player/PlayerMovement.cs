using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalSpeed;
    private float dashEnd;
    private float nextDash;
    private Vector3 dashDirection;
    public float DashCooldown => Mathf.Max(0f, nextDash - Time.time);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector2 input = ReadMovement();

        // Usa a direção horizontal da câmera como referência.
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();
        }

        Vector3 direction = forward * input.y + right * input.x;
        direction = Vector3.ClampMagnitude(direction, 1f);

        // Mantém contato com o chão e aplica gravidade.
        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && Time.time >= nextDash)
        {
            dashDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
            dashEnd = Time.time + 0.22f;
            nextDash = Time.time + 1.6f;
        }
        if (Time.time < dashEnd) velocity = dashDirection * 12f;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    private Vector2 ReadMovement()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }
}