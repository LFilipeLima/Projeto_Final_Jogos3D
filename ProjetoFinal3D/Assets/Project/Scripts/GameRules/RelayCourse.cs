using System.Collections;
using UnityEngine;

public sealed class RelayCourse : MonoBehaviour
{
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Transform[] route;
    private const float Radius = 1.35f;
    private LineRenderer ring;
    private DispatchManifest manifest;
    public Transform RetainedParcel => manifest ? manifest.Parcel : null;
    public bool RemessaRetida => manifest && manifest.Restored;

    public Transform ActiveRing => ring && ring.enabled ? ring.transform : null;
    public string Hint { get; private set; }

    public IEnumerator Run(int wave, PlayerHealth player, Transform[] spawnPoints, System.Action refresh)
    {
        int[] fallbackOrder = { 0, 3, 1, 2 };

        int[] stops = wave == 1 ? new[] { 0, 1 } : new[] { 2, 3, 0 };
        if (!ring)
            CreateRing(player);

        for (int i = 0; i < stops.Length; i++)
        {
            int index = stops[i];
            Transform stop = route != null && route.Length > index ? route[index] : null;
            if (!stop && spawnPoints != null && spawnPoints.Length > fallbackOrder[index])
                stop = spawnPoints[fallbackOrder[index]];
            if (!stop || !player)
            {
                Hint = "Circuito indisponível: confira os pontos da arena.";
                refresh();
                Debug.LogError(Hint, this);
                yield break;
            }

            Vector3 center = stop.position;
            ring.transform.position = center;
            ring.enabled = true;
            string vertical = center.z > transform.position.z ? "norte" : "sul";
            string horizontal = center.x > transform.position.x ? "leste" : "oeste";
            Hint = $"Retransmissão {i + 1}/{stops.Length} • Alcance o aro a {vertical}/{horizontal}";
            if (wave == 1 && i == 0)
            {
                Hint = $"Caixa 17 • Vá ao aro a {vertical}/{horizontal} e pressione R.";
                refresh();
                manifest = gameObject.AddComponent<DispatchManifest>();
                yield return manifest.Inspect(ring, player, player.GetComponentInChildren<MeshRenderer>().sharedMaterial);
                continue;
            }
            if (wave == 2 && i == 0 && RemessaRetida)
            {
                var line = new GameObject("Triagem da remessa 17");
                line.transform.SetParent(transform);
                line.transform.position = center;
                Hint = $"Esteira a {vertical}/{horizontal} • E: pulso ou R junto à alavanca.";
                refresh();
                yield return line.AddComponent<SortingBelt>().Route(player, manifest.Parcel,
                    player.GetComponentInChildren<MeshRenderer>().sharedMaterial);
                continue;
            }
            Debug.Log($"[Circuito] {Hint}", this);
            refresh();

            bool approached = false;
            while (true)
            {
                Vector3 towardPlayer = player.transform.position - center;
                towardPlayer.y = 0f;
                if (towardPlayer.sqrMagnitude > Radius * Radius)
                    approached = true;
                else if (approached)
                    break;
                if (towardPlayer.sqrMagnitude > 0.001f)
                    ring.transform.rotation = Quaternion.LookRotation(towardPlayer);
                ring.widthMultiplier = 0.13f + 0.04f * Mathf.Sin(Time.time * 5f);
                yield return null;
            }
        }
        Clear();
        refresh();
    }

    public void Clear()
    {
        Hint = null;
        if (ring) ring.enabled = false;
    }

    private void CreateRing(PlayerHealth player)
    {
        var marker = new GameObject("Aro do circuito");
        marker.transform.SetParent(transform, false);
        ring = marker.AddComponent<LineRenderer>();
        if (ringMaterial)
            ring.sharedMaterial = ringMaterial;
        else if (player && player.TryGetComponent(out LineRenderer tracer))
            ring.sharedMaterial = tracer.sharedMaterial;
        ring.startColor = ring.endColor = Color.cyan;
        ring.generateLightingData = true;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 64;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = 2f * Mathf.PI * i / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Radius);
        }
    }
}
