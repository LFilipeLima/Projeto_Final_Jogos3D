using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SortingBelt : MonoBehaviour
{
    public bool Routed { get; private set; }
    private PlayerShooter shooter;
    private TextMesh destination;
    private Transform lever;
    private Material surface;

    public IEnumerator Route(PlayerHealth player, Transform parcel, Material material)
    {
        surface = material;
        Part("Esteira", new Vector3(0,-.66f,0), new Vector3(3.8f,.35f,.85f), Color.gray);
        for (int i=-4;i<=4;i++)
            Part("Rolete",new Vector3(i*.39f,-.46f,0),new Vector3(.12f,.06f,.8f),Color.black);
        Part("Saída lateral",new Vector3(0,-.66f,1f),new Vector3(.9f,.35f,1.9f),Color.gray);
        lever = Part("Alavanca",new Vector3(0,-.4f,-.65f),new Vector3(.12f,.6f,.12f),Color.yellow);
        lever.localRotation = Quaternion.Euler(0,0,-32f);
        destination = new GameObject("Indicador de destino").AddComponent<TextMesh>();
        destination.transform.SetParent(transform,false);
        destination.transform.localPosition = new Vector3(0,.85f,0);
        destination.anchor = TextAnchor.MiddleCenter;
        destination.alignment = TextAlignment.Center;
        destination.fontSize = 48;
        destination.characterSize = .055f;
        destination.text = "LINHA 17   /   DESCARTE";
        shooter = player.GetComponent<PlayerShooter>();
        shooter.Pulsed += ReceivePulse;
        Vector3 entry = transform.TransformPoint(new Vector3(-1.55f,-.08f,0));
        Vector3 junction = transform.TransformPoint(new Vector3(0,-.08f,0));
        parcel.position = entry;
        while (!Routed || (parcel.position-junction).sqrMagnitude > .0001f)
        {
            parcel.position = Vector3.MoveTowards(parcel.position,junction,Time.deltaTime*1.4f);
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame &&
                Vector3.Distance(player.transform.position,lever.position) <= 1.7f) Divert();
            yield return null;
        }
        Vector3 output = junction + transform.forward*1.65f;
        while ((parcel.position-output).sqrMagnitude > .0001f)
        {
            parcel.position = Vector3.MoveTowards(parcel.position,output,Time.deltaTime*1.8f);
            yield return null;
        }
        destination.text = "ARQUIVO   /   RECEBIDO";
        ArchiveAudio.Play(ArchiveAudio.Cue.Document);
        Disconnect();
    }

    private Transform Part(string title,Vector3 center,Vector3 size,Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = title;
        go.transform.SetParent(transform,false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        go.GetComponent<Collider>().enabled = false;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = surface;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor",color);
        renderer.SetPropertyBlock(block);
        return go.transform;
    }

    private void ReceivePulse(Vector3 center,float range)
    {
        if ((lever.position-center).sqrMagnitude <= range*range) Divert();
    }

    private void Divert()
    {
        if (Routed) return;
        Routed = true;
        lever.localRotation = Quaternion.Euler(0,0,32f);
        destination.text = "LINHA 17   /   ARQUIVO";
        destination.color = Color.cyan;
    }

    private void Disconnect()
    {
        if (shooter) shooter.Pulsed -= ReceivePulse;
    }

    private void OnDisable() => Disconnect();
}
