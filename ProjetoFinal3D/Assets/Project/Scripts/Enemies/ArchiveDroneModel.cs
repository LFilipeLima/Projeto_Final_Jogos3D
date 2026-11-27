using UnityEngine;

public sealed class ArchiveDroneModel : MonoBehaviour
{
    private static readonly string[] ModelPaths =
    {
        "ArchiveDrones/DroneLeitor",
        "ArchiveDrones/DroneAtirador",
        "ArchiveDrones/DronePesado"
    };

    private Transform animatedPart;
    private Vector3 restPosition;

    public void Assemble(int model)
    {
        EnemyHitFeedback feedback = GetComponentInChildren<EnemyHitFeedback>();
        if (!feedback) return;

        MeshRenderer placeholder = feedback.GetComponent<MeshRenderer>();
        GameObject prefab = Resources.Load<GameObject>(ModelPaths[Mathf.Clamp(model, 0, ModelPaths.Length - 1)]);
        if (!prefab)
        {
            Debug.LogError($"Prefab de drone não encontrado em Resources/{ModelPaths[Mathf.Clamp(model, 0, ModelPaths.Length - 1)]}.", this);
            return;
        }

        if (placeholder) placeholder.enabled = false;
        Transform mount = feedback.transform;
        for (int i = mount.childCount - 1; i >= 0; i--)
            Destroy(mount.GetChild(i).gameObject);

        GameObject visual = Instantiate(prefab, mount, false);
        visual.name = prefab.name;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            if (child.name == "Elemento móvel")
            {
                animatedPart = child;
                break;
            }
        feedback.RefreshRenderers();
        if (animatedPart) restPosition = animatedPart.localPosition;
    }

    public void WindUp(float fraction)
    {
        if (!animatedPart) return;
        float travel = Mathf.Clamp01(fraction);
        animatedPart.localPosition = restPosition + Vector3.up * (.28f * travel);
    }
}
