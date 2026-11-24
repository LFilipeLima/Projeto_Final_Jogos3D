using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public sealed class EnemyHitFeedback : MonoBehaviour
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private readonly Color wounded = new Color(1f, 0.5f, 0.04f);
    private EnemyHealth health;
    private MeshRenderer body;
    private MaterialPropertyBlock properties;
    private Vector3 restScale;

    private void Awake()
    {
        health = GetComponentInParent<EnemyHealth>();
        body = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
    }

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
        body.GetPropertyBlock(properties);
        const float duration = 0.18f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(impactScale, restScale, t);
            properties.SetColor(BaseColor, Color.Lerp(Color.white, wounded, t));
            body.SetPropertyBlock(properties);
            yield return null;
        }

        transform.localScale = restScale;
        properties.SetColor(BaseColor, wounded);
        body.SetPropertyBlock(properties);
    }
}
