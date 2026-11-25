using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ArchiveTransmission : MonoBehaviour
{
    private const float SecondsPerSeries = 1.4f;
    private GameManager manager;
    private TextMesh display;
    private float elapsed;
    private int shownPercent = -1;
    public bool InReach { get; private set; }
    public bool Sending { get; private set; }
    public float Distance { get; private set; }
    public int CurrentYear => ArenaSector.Year(Mathf.Min(GameManager.TotalWavesCount, 1 + Mathf.FloorToInt(elapsed / SecondsPerSeries)));
    public float Progress => Mathf.Clamp01(elapsed / (SecondsPerSeries * GameManager.TotalWavesCount));
    private Action complete;

    public void Activate(GameManager game, Vector3 position, Action onComplete)
    {
        manager = game;
        complete = onComplete;
        transform.position = position;
        var baseUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseUnit.name = "Leitor de remessas";
        baseUnit.transform.SetParent(transform,false);
        baseUnit.transform.localScale = new Vector3(1.25f,.85f,.65f);
        baseUnit.GetComponent<Collider>().enabled = false;
        var surface = baseUnit.GetComponent<MeshRenderer>();
        surface.sharedMaterial = game.PlayerHealth.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        var ink = new MaterialPropertyBlock();
        ink.SetColor("_BaseColor",new Color(.10f,.43f,.39f));
        surface.SetPropertyBlock(ink);
        display = new GameObject("Visor de transmissão").AddComponent<TextMesh>();
        display.transform.SetParent(transform,false);
        display.transform.localPosition = Vector3.up * 1.1f;
        display.anchor = TextAnchor.MiddleCenter;
        display.alignment = TextAlignment.Center;
        display.fontSize = 48;
        display.characterSize = .053f;
        display.text = "ARQUIVO 88\nTRANSMISSÃO";
    }

    private void Update()
    {
        if (!manager || !manager.IsPlaying) return;
        Vector3 offset = manager.PlayerHealth.transform.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > .001f)
            display.transform.rotation = Quaternion.LookRotation(-offset);
        Distance = offset.magnitude;
        InReach = Distance <= 2.2f;
        Sending = InReach && Keyboard.current != null && Keyboard.current.rKey.isPressed;
        if (!Sending) return;

        elapsed += Time.deltaTime;
        int percent = Mathf.FloorToInt(100 * Progress);
        if (percent != shownPercent)
        {
            display.text = $"REMESSA 17   /   {CurrentYear}\n{percent}%";
            shownPercent = percent;
        }
        if (Progress < 1f) return;
        enabled = false;
        display.text = "ORIGINAL RECEBIDO\nL. NUNES";
        ArchiveAudio.Play(ArchiveAudio.Cue.Series);
        complete();
    }
}
