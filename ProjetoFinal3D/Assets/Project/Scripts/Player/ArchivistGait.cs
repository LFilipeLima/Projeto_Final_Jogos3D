using UnityEngine;

public sealed class ArchivistGait : MonoBehaviour
{
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    private Vector3 previous;
    private float phase;
    private float amplitude;

    private void OnEnable() => previous = transform.position;

    private void LateUpdate()
    {
        Vector3 travel = transform.position - previous;
        previous = transform.position;
        travel.y = 0f;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float step = Mathf.Min(travel.magnitude, 8f * dt);
        phase += step * 5.5f;
        amplitude = Mathf.MoveTowards(amplitude, Mathf.Clamp01(step / (2f * dt)), 8f * dt);
        float angle = Mathf.Sin(phase) * amplitude * 23f;
        leftLeg.localRotation = Quaternion.Euler(angle, 0f, 0f);
        rightLeg.localRotation = Quaternion.Euler(-angle, 0f, 0f);
    }
}
