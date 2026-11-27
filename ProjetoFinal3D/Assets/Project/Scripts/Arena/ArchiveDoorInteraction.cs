using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ArchiveDoorInteraction : MonoBehaviour
{
    [SerializeField] private Transform leftLeaf;
    [SerializeField] private Transform rightLeaf;
    [SerializeField] private float useDistance = 3.2f;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float turnSpeed = 150f;

    private PlayerShooter player;
    private Quaternion leftClosed;
    private Quaternion rightClosed;
    private bool isOpen;

    private void Awake()
    {
        if (!leftLeaf) leftLeaf = transform.Find("Folha esquerda");
        if (!rightLeaf) rightLeaf = transform.Find("Folha direita");
        if (leftLeaf) leftClosed = leftLeaf.localRotation;
        if (rightLeaf) rightClosed = rightLeaf.localRotation;
    }

    private void Start() => player = FindFirstObjectByType<PlayerShooter>();

    private void Update()
    {
        if (!player) player = FindFirstObjectByType<PlayerShooter>();
        if (!player) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame &&
            Vector3.Distance(player.transform.position, transform.position) <= useDistance)
            isOpen = !isOpen;

        float step = turnSpeed * Time.deltaTime;
        if (leftLeaf)
            leftLeaf.localRotation = Quaternion.RotateTowards(leftLeaf.localRotation,
                isOpen ? leftClosed * Quaternion.Euler(0f, openAngle, 0f) : leftClosed, step);
        if (rightLeaf)
            rightLeaf.localRotation = Quaternion.RotateTowards(rightLeaf.localRotation,
                isOpen ? rightClosed * Quaternion.Euler(0f, -openAngle, 0f) : rightClosed, step);
    }

    private void OnGUI()
    {
        if (!player || Vector3.Distance(player.transform.position, transform.position) > useDistance)
            return;

        const float width = 220f;
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height - 116f, width, 32f),
            isOpen ? "[E] Fechar porta" : "[E] Abrir porta");
    }
}
