using UnityEngine;
using UnityEngine.AI;

public static class ArenaSector
{
    public static string Name(int wave) => wave == 1 ? "Convocação" : wave == 2 ? "Assembleia" : "Promulgação";

    public static int Year(int wave) => wave == 1 ? 1985 : wave == 2 ? 1987 : 1988;
    public static Color Ink(int wave) => wave == 1 ? new Color(0.35f, 0.95f, 1f)
        : wave == 2 ? new Color(1f, 0.84f, 0.35f) : new Color(1f, 0.6f, 0.65f);

    public static GameObject Build(int wave, PlayerHealth player)
    {
        var sector = new GameObject(Name(wave));
        var notice = new GameObject("Painel do arquivo");
        notice.transform.SetParent(sector.transform);
        notice.transform.position = new Vector3(0f, 2.65f, 9.9f);
        var sign = notice.AddComponent<TextMesh>();
        sign.anchor = TextAnchor.UpperCenter;
        sign.alignment = TextAlignment.Center;
        sign.fontSize = 64;
        sign.characterSize = 0.078f;
        sign.color = wave == 1 ? Color.white : wave == 2 ? Color.yellow : new Color(1f, 0.5f, 0.25f);
        string bulletin = wave == 1 ? "1985 • EC 26 convoca a Constituinte" : wave == 2 ? "1987 • Instalação da Assembleia" : "1988 • Promulgação da Constituição";
        sign.text = "ARQUIVO 88\n" + bulletin + "\nFonte: Câmara dos Deputados";
        if (wave == 1) return sector;

        var positions = wave == 2
            ? new[] { new Vector3(-2, 0, 4), new Vector3(2, 0, -4) }
            : new[] { new Vector3(-4, 0, -3), new Vector3(4, 0, 3), new Vector3(0, 0, 4) };
        Vector3 size = wave == 2 ? new Vector3(3.8f, 1.7f, 1f) : new Vector3(1.8f, 2.3f, 1.8f);
        foreach (Vector3 ground in positions)
        {
            Vector3 center = ground + Vector3.up * size.y * 0.5f;
            // Preserva espaço para o jogador e para células deixadas entre ondas.
            if (Physics.CheckBox(center, size * 0.5f + new Vector3(0.65f, -0.1f, 0.65f),
                    Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide)) continue;
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = wave == 2 ? "Contêiner" : "Pilar";
            cover.transform.SetParent(sector.transform);
            cover.transform.position = center;
            cover.transform.localScale = size;
            var renderer = cover.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = player.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", wave == 2 ? new Color(0.18f, 0.56f, 0.60f) : new Color(0.55f, 0.32f, 0.15f));
            renderer.SetPropertyBlock(block);
            var obstacle = cover.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
        }
        return sector;
    }
}
