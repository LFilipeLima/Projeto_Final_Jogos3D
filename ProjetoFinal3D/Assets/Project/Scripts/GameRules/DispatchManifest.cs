using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DispatchManifest : MonoBehaviour
{
    public Transform Parcel { get; private set; }
    public bool Restored { get; private set; }
    private bool reading;
    private bool rejected;

    public IEnumerator Inspect(LineRenderer marker, PlayerHealth player, Material surface)
    {
        Vector3 position = marker.transform.position;
        var parcel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Parcel = parcel.transform;
        parcel.name = "Remessa 17";
        parcel.transform.SetParent(transform);
        parcel.transform.position = position + new Vector3(0.85f, -0.55f, 0.15f);
        parcel.transform.localScale = new Vector3(0.85f,0.72f,0.64f);
        var collider = parcel.GetComponent<BoxCollider>();
        collider.enabled = false;
        var body = parcel.GetComponent<MeshRenderer>();
        body.sharedMaterial = surface;
        var ink = new MaterialPropertyBlock();
        ink.SetColor("_BaseColor", new Color(.68f,.45f,.23f));
        body.SetPropertyBlock(ink);
        var label = new GameObject("Etiqueta da remessa").AddComponent<TextMesh>();
        label.transform.SetParent(parcel.transform, false);
        label.transform.localPosition = new Vector3(0f,0f,-.51f);
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = .052f;
        label.fontSize = 48;
        label.text = "PROTOCOLO\n17";

        while (!Restored)
        {
            var delta = player.transform.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.001f) marker.transform.rotation = Quaternion.LookRotation(delta);
            marker.widthMultiplier = 0.15f;
            if (delta.sqrMagnitude > 2.4f * 2.4f) reading = false;
            else if (Keyboard.current != null)
            {
                if (!reading && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    reading = true;
                    rejected = false;
                }
                else if (reading)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame) rejected = true;
                    if (Keyboard.current.digit2Key.wasPressedThisFrame) Restored = true;
                }
            }
            yield return null;
        }

        reading = false;
        label.text = "17\nRETIDA";
        ArchiveAudio.Play(ArchiveAudio.Cue.Series);
    }

    private void OnGUI()
    {
        if (!reading || !GameManager.Instance || GameManager.Instance.IsGameOver) return;
        float w = Mathf.Min(530f,Screen.width-20f);
        GUILayout.BeginArea(new Rect((Screen.width-w)*0.5f,(Screen.height-230f)*0.5f,w,230f),GUI.skin.box);
        GUILayout.Label("ARQUIVO 88    /    GUIA DE TRIAGEM 017");
        GUILayout.Space(8);
        GUILayout.Label("Cópia digital    •    Assinatura: campo vazio");
        GUILayout.Label("Regra de triagem    •    Sem assinatura → descarte");
        GUILayout.Label("Original    •    Margem: “Reter. Caixa 17.” — L. Nunes");
        GUILayout.Space(8);
        GUILayout.Label("1   Confirmar o descarte indicado pela cópia");
        GUILayout.Label("2   Anexar a margem assinada e reter a remessa");
        if (rejected) GUILayout.Label("Conferência recusada. A cópia omitiu a assinatura da margem.");
        GUILayout.EndArea();
    }
}
