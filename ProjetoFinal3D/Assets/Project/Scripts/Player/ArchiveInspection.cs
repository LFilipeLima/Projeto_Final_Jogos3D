using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Reusable archive desk. The scene owner positions the desk and the two boxes.
public sealed class ArchiveInspection : MonoBehaviour
{
    [SerializeField] private Transform page;
    [SerializeField] private Transform box12;
    [SerializeField] private Transform box17;
    public Transform Document => page;
    public bool MarginDiscovered { get; private set; }
    public bool BoxRecovered { get; private set; }
    public event Action MarginFound;
    public event Action CorrectBoxRecovered;

    private PlayerHealth player;
    private bool reading;
    private float scan;
    private float exposure;
    private string response;
    private float responseUntil;
    private PlayerMovement movement;
    private PlayerShooter shooter;
    private PlayerCamera view;
    private bool restoreMovement, restoreShooter, restoreView;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        if (!player || !page || !box12 || !box17)
        {
            Debug.LogError("Arquivo: atribua a página, as caixas e um Player na cena.", this);
            enabled = false;
            return;
        }
        movement = player.GetComponent<PlayerMovement>();
        shooter = player.GetComponent<PlayerShooter>();
        if (Camera.main) view = Camera.main.GetComponent<PlayerCamera>();
    }

    private Transform Nearest()
    {
        Transform best = null;
        float distance = 2.4f * 2.4f;
        void Consider(Transform item)
        {
            float d = (item.position - player.transform.position).sqrMagnitude;
            if (d >= distance) return;
            best = item;
            distance = d;
        }
        Consider(page);
        Consider(box12);
        Consider(box17);
        return best;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !player || player.IsDead || (GameManager.Instance && GameManager.Instance.IsGameOver))
        {
            EndReading();
            return;
        }
        if (BoxRecovered) return;
        if (reading)
        {
            if (!keyboard.qKey.isPressed) { EndReading(); return; }
            float input = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            scan = Mathf.Clamp01(scan + input * Time.deltaTime * 0.7f);
            if (!MarginDiscovered && scan >= 0.79f)
            {
                exposure += Time.deltaTime;
                if (exposure >= 0.45f)
                {
                    MarginDiscovered = true;
                    MarginFound?.Invoke();
                }
            }
            else exposure = 0f;
            return;
        }

        Transform near = Nearest();
        if (near == page && keyboard.qKey.wasPressedThisFrame)
        {
            restoreMovement = movement && movement.enabled;
            restoreShooter = shooter && shooter.enabled;
            restoreView = view && view.enabled;
            if (restoreMovement) movement.enabled = false;
            if (restoreShooter) shooter.enabled = false;
            if (restoreView) view.enabled = false;
            reading = true;
            return;
        }
        if (!near || near == page || !keyboard.rKey.wasPressedThisFrame) return;
        if (!MarginDiscovered) response = "A etiqueta sozinha não identifica a remessa. Consulte a página.";
        else if (near == box12) response = "Caixa 12 • Material de escritório. A anotação indica outra remessa.";
        else
        {
            BoxRecovered = true;
            response = "Remessa 17 recuperada. Destino: galpão de triagem.";
            CorrectBoxRecovered?.Invoke();
        }
        responseUntil = Time.time + 4f;
    }

    private void EndReading()
    {
        if (!reading) return;
        reading = false;
        exposure = 0f;
        if (!player || player.IsDead || (GameManager.Instance && GameManager.Instance.IsGameOver)) return;
        if (restoreMovement && movement) movement.enabled = true;
        if (restoreShooter && shooter) shooter.enabled = true;
        if (restoreView && view) view.enabled = true;
    }

    private void OnDisable() => EndReading();

    private void OnGUI()
    {
        if (!player || player.IsDead || (GameManager.Instance && GameManager.Instance.IsGameOver)) return;
        if (reading) { DrawComparison(); return; }
        if (BoxRecovered && Time.time > responseUntil) return;
        Transform near = Nearest();
        string prompt = Time.time < responseUntil ? response :
            near == page ? "Segure Q para comparar a página. Use A/D para mover a lente." :
            near ? "R • Conferir etiqueta da caixa" :
            MarginDiscovered ? "Localize a caixa indicada na margem do original." :
            "Arquivo 88 • A cópia de descarte perdeu uma anotação. Examine a página no balcão.";
        float width = Mathf.Min(620, Screen.width - 24);
        GUI.Box(new Rect((Screen.width-width)/2f, Screen.height-105, width, 56), prompt);
    }

    private void DrawComparison()
    {
        // Fixed document coordinates keep both versions aligned at any window size.
        Matrix4x4 old = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 720f, Screen.height / 390f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-700*scale)/2f,(Screen.height-360*scale)/2f,0), Quaternion.identity, Vector3.one*scale);
        Fill(new Rect(0,0,700,360), new Color(.06f,.12f,.13f));
        GUI.Label(new Rect(24,14,650,28), "ARQUIVO 88   /   MESA DE CONFERÊNCIA                         PEÇA FICCIONAL");
        Rect paper = new Rect(24,52,652,228);
        Fill(paper, new Color(.87f,.84f,.72f));
        var ink = new GUIStyle(GUI.skin.label) {fontSize = 18};
        ink.normal.textColor = new Color(.12f,.17f,.16f);
        GUI.Label(new Rect(42,68,580,32), "FOLHA DE REMESSA     /     VIA DIGITAL", ink);
        GUI.Label(new Rect(42,118,355,110), "Origem: balcão de protocolo\nDestino: galpão de triagem\nEncaminhamento: descarte", ink);
        float edge = Mathf.Lerp(28, 550, scan);
        GUI.BeginGroup(new Rect(edge,55,124,221));
        Fill(new Rect(0,0,124,221), new Color(.69f,.83f,.76f));
        GUI.BeginGroup(new Rect(24-edge,-3,652,228));
        GUI.Label(new Rect(18,16,600,32),"FOLHA DE REMESSA     /     ORIGINAL",ink);
        GUI.Label(new Rect(18,66,360,110),"Origem: balcão de protocolo\nDestino: galpão de triagem\nEncaminhamento: descarte",ink);
        GUI.Label(new Rect(492,92,142,96),"RETER\nCaixa 17\nL. Nunes",ink);
        GUI.EndGroup();
        GUI.EndGroup();
        GUI.Label(new Rect(24,292,650,25), MarginDiscovered ? "Margem identificada: reter a caixa 17. Assinatura: L. Nunes." : "Mova a lente até a margem direita e mantenha a anotação visível.");
        GUI.Label(new Rect(24,323,650,25),"A / D   deslizar lente                         Solte Q para voltar ao arquivo");
        GUI.matrix = old;
    }

    private static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect,Texture2D.whiteTexture);
        GUI.color = old;
    }
}
