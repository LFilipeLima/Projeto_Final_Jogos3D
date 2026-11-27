using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DispatchManifest : MonoBehaviour
{
    private const float StartReadingRange = 2.4f;
    private const float KeepReadingRange = 5.5f;

    public Transform Parcel { get; private set; }
    public bool Restored { get; private set; }
    private bool reading;
    private bool rejected;

    private static GUIStyle titleStyle;
    private static GUIStyle sectionStyle;
    private static GUIStyle bodyStyle;
    private static GUIStyle keyStyle;
    private static GUIStyle feedbackStyle;

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
            var keyboard = Keyboard.current;
            if (reading && delta.sqrMagnitude > KeepReadingRange * KeepReadingRange)
                reading = false;

            if (keyboard != null)
            {
                if (!reading && delta.sqrMagnitude <= StartReadingRange * StartReadingRange && keyboard.rKey.wasPressedThisFrame)
                {
                    reading = true;
                    rejected = false;
                }
                else if (reading)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame)
                    {
                        reading = false;
                    }
                    else if (PressedOption(keyboard, 1))
                    {
                        rejected = true;
                    }
                    else if (PressedOption(keyboard, 2))
                    {
                        Restored = true;
                    }
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
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .9f, 1.35f);
        EnsureStyles(scale);
        float width = Mathf.Min(Screen.width - 32f, 820f * scale);
        float height = Mathf.Min(Screen.height - 32f, 480f * scale);
        Rect panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
        GUI.Box(panel, GUIContent.none);

        float padding = 18f * scale;
        GUILayout.BeginArea(new Rect(panel.x + padding, panel.y + padding, panel.width - padding * 2f, panel.height - padding * 2f));
        GUILayout.Label("ARQUIVO 88   /   GUIA DE TRIAGEM 017", titleStyle);
        GUILayout.Space(8f * scale);

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("DOCUMENTOS / LORE", sectionStyle);
        GUILayout.Label("Cópia digital: o campo de assinatura está vazio. Pela regra de triagem, a remessa seria descartada.", bodyStyle);
        GUILayout.Label("Original: a margem tem uma anotação assinada por L. Nunes — “Reter. Caixa 17.”", bodyStyle);
        GUILayout.EndVertical();

        GUILayout.Space(8f * scale);
        GUILayout.Label("ESCOLHA UMA RESPOSTA", sectionStyle);
        DrawOption("1", "Confirmar o descarte indicado pela cópia.", scale);
        DrawOption("2", "Reter a remessa 17 seguindo a anotação do original.", scale);

        GUILayout.Space(6f * scale);
        if (rejected)
            GUILayout.Label("Resposta não aceita: a cópia omite a anotação manuscrita do original. Compare as duas informações e tente novamente.", feedbackStyle);
        else
            GUILayout.Label("Pressione 1 ou 2 no teclado para responder. O teclado numérico também funciona.", bodyStyle);
        GUILayout.Label("WASD e câmera continuam ativos  •  Esc fecha o diálogo", bodyStyle);
        GUILayout.EndArea();
    }

    private static bool PressedOption(Keyboard keyboard, int option)
    {
        return option == 1
            ? keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame
            : keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
    }

    private static void DrawOption(string number, string text, float scale)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label(number, keyStyle, GUILayout.Width(46f * scale), GUILayout.Height(42f * scale));
        GUILayout.Label(text, bodyStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndHorizontal();
    }

    private static void EnsureStyles(float scale)
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            sectionStyle = new GUIStyle(GUI.skin.label);
            bodyStyle = new GUIStyle(GUI.skin.label);
            keyStyle = new GUIStyle(GUI.skin.button);
            feedbackStyle = new GUIStyle(GUI.skin.label);
        }

        titleStyle.fontSize = Mathf.RoundToInt(25f * scale);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.wordWrap = true;

        sectionStyle.fontSize = Mathf.RoundToInt(19f * scale);
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.wordWrap = true;

        bodyStyle.fontSize = Mathf.RoundToInt(17f * scale);
        bodyStyle.wordWrap = true;
        bodyStyle.richText = true;

        keyStyle.fontSize = Mathf.RoundToInt(26f * scale);
        keyStyle.fontStyle = FontStyle.Bold;
        keyStyle.alignment = TextAnchor.MiddleCenter;

        feedbackStyle.fontSize = Mathf.RoundToInt(17f * scale);
        feedbackStyle.fontStyle = FontStyle.Bold;
        feedbackStyle.wordWrap = true;
    }
}
