using UnityEngine;

public sealed class BlinkingObjectiveHint : MonoBehaviour
{
    [SerializeField] private Renderer indicator;
    [SerializeField] private Light pointLight;
    [SerializeField] private Color pulseColor = new Color(1f, .65f, .22f);
    [SerializeField] private float pulseSpeed = 2.4f;
    [SerializeField] private float minimumIntensity = .15f;
    [SerializeField] private float maximumIntensity = 1.7f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock properties;

    private void Awake()
    {
        if (!indicator) indicator = GetComponentInChildren<Renderer>();
        if (!pointLight) pointLight = GetComponentInChildren<Light>();
        properties = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * .5f;
        float intensity = Mathf.Lerp(minimumIntensity, maximumIntensity, pulse);
        if (pointLight) pointLight.intensity = intensity;
        if (indicator)
        {
            indicator.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, pulseColor * Mathf.Lerp(.55f, 1.4f, pulse));
            indicator.SetPropertyBlock(properties);
        }
    }
}
