using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public sealed class EnemyHitFeedback : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private readonly Color wounded = new Color(1f, 0.5f, 0.04f);
    private EnemyHealth health;
    private MeshRenderer[] bodies;
    private MaterialPropertyBlock properties;
    private Vector3 restScale;

    private void Awake()
    {
        health = GetComponentInParent<EnemyHealth>();
        RefreshRenderers();
        properties = new MaterialPropertyBlock();
    }

    public void RefreshRenderers() => bodies = GetComponentsInChildren<MeshRenderer>(true);

    private void Start()
    {
        restScale = transform.localScale;
    }

    private void OnEnable() => health.Hurt += ShowImpact;
    private void OnDisable() => health.Hurt -= ShowImpact;

    private void ShowImpact()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateImpact());
    }

    private IEnumerator AnimateImpact()
    {
        Vector3 impactScale = Vector3.Scale(restScale, new Vector3(1.18f, 0.84f, 1.18f));
        const float duration = 0.18f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(impactScale, restScale, t);
            SetImpactColor(Color.Lerp(Color.white, wounded, t));
            yield return null;
        }

        transform.localScale = restScale;
        SetImpactColor(wounded);
    }

    private void SetImpactColor(Color color)
    {
        properties.SetColor(BaseColor, color);
        foreach (MeshRenderer renderer in bodies)
            if (renderer) renderer.SetPropertyBlock(properties);
    }
}
