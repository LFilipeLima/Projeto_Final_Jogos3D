using UnityEngine;

public static class ArenaSignal
{
    public static LineRenderer Line(Material material, Color color, int count)
    {
        var line = new GameObject("Sinal de combate").AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        line.SetPropertyBlock(block);
        line.startColor = line.endColor = Color.white;
        line.widthMultiplier = 0.12f;
        line.positionCount = count;
        return line;
    }

    public static LineRenderer Ring(Vector3 center, float radius, Material material, Color color, float duration)
    {
        var line = Line(material, color, 48);
        line.loop = true;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, center + radius * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
        }
        Object.Destroy(line.gameObject, duration);
        return line;
    }
}
