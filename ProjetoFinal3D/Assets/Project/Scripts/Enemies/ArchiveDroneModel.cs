using UnityEngine;

public sealed class ArchiveDroneModel : MonoBehaviour
{
    private static readonly Color Enamel = new Color(.11f, .16f, .18f);
    private static readonly Color Brass = new Color(.86f, .64f, .32f);
    private Material finish;
    private Transform mount;
    private Transform carriage;
    private bool press;

    public void Assemble(int model)
    {
        var shell = GetComponentInChildren<EnemyHitFeedback>();
        finish = shell.GetComponent<MeshRenderer>().sharedMaterial;
        mount = new GameObject("Ferramenta de arquivo").transform;
        mount.SetParent(shell.transform, false);
        if (model == 0)
        {
            Part("Cabeça de leitura", PrimitiveType.Cube, new Vector3(0,.32f,.26f), new Vector3(.84f,.32f,.52f), Enamel);
            Part("Janela do leitor", PrimitiveType.Cube, new Vector3(0,.32f,.53f), new Vector3(.66f,.09f,.04f), Color.cyan);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform fin = Part("Guia de fichas", PrimitiveType.Cube, new Vector3(side*.41f,.02f,0),
                    new Vector3(.10f,.48f,.52f), Brass);
                fin.localRotation = Quaternion.Euler(0,0,-side*16);
            }
        }
        else if (model == 1)
        {
            Part("Cabeçote óptico", PrimitiveType.Cube, new Vector3(0,.28f,.12f), new Vector3(.92f,.44f,.72f), Enamel);
            carriage = Part("Objetiva", PrimitiveType.Cylinder, new Vector3(0,.28f,.46f),
                new Vector3(.39f,.18f,.39f), Brass);
            carriage.localRotation = Quaternion.Euler(90,0,0);
            var lens = Part("Vidro da objetiva", PrimitiveType.Sphere, Vector3.zero, Vector3.one, Color.magenta);
            lens.SetParent(carriage,false);
            lens.localPosition = Vector3.up*.98f;
            lens.localScale = new Vector3(.85f,.13f,.85f);
        }
        else
        {
            press = true;
            for (int side = -1; side <= 1; side += 2)
                Part("Coluna da prensa", PrimitiveType.Cube, new Vector3(side*.43f,.14f,.17f),
                    new Vector3(.14f,1.1f,.35f), Brass);
            Part("Travessa", PrimitiveType.Cube, new Vector3(0,.65f,.17f), new Vector3(1f,.18f,.45f), Enamel);
            carriage = Part("Carimbo", PrimitiveType.Cube, new Vector3(0,-.28f,.32f),
                new Vector3(.85f,.27f,.62f), Enamel);
        }
    }

    private Transform Part(string title, PrimitiveType shape, Vector3 position, Vector3 size, Color color)
    {
        var piece = GameObject.CreatePrimitive(shape);
        piece.name = title;
        piece.transform.SetParent(mount,false);
        piece.transform.localPosition = position;
        piece.transform.localScale = size;
        piece.GetComponent<Collider>().enabled = false;
        var renderer = piece.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = finish;
        var ink = new MaterialPropertyBlock();
        ink.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(ink);
        return piece.transform;
    }

    public void WindUp(float fraction)
    {
        if (!carriage) return;
        float travel = Mathf.Clamp01(fraction);
        carriage.localPosition = press
            ? new Vector3(0,-.28f + .6f * travel,.32f)
            : new Vector3(0,.28f,.46f + .32f * travel);
    }
}
