using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class DepotTransit : MonoBehaviour
{
    [SerializeField] private Transform receiver;
    [SerializeField] private Transform dispatch;
    [SerializeField] private Transform exit;
    [SerializeField] private Transform cargoGate;
    [SerializeField] private GameObject serviceGate;
    [SerializeField] private GameObject exitGate;
    private PlayerShooter player;
    private bool loading;
    private float cycle;
    public bool Cleared { get; private set; }
    private string hint;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerShooter>();
        player.Pulsed += OnPulse;
        hint = "Doca 17 • Escolha a manutenção à esquerda ou a carga à direita.";
    }

    private void OnDestroy()
    {
        if (player) player.Pulsed -= OnPulse;
    }

    private void OnPulse(Vector3 origin, float radius)
    {
        if (!Cleared && Vector3.Distance(origin,receiver.position) <= radius)
            Release("Circuito de manutenção energizado");
    }

    private void Release(string reason)
    {
        Cleared = true;
        serviceGate.SetActive(false);
        exitGate.SetActive(false);
        hint = reason + " • Siga para a expedição no fundo do depósito.";
    }

    private void Update()
    {
        if (!player || loading) return;
        // Hold the barrier open while the player is under its footprint.
        Vector3 delta = player.transform.position - cargoGate.position;
        bool underneath = Mathf.Abs(delta.z) < 1.4f && Mathf.Abs(delta.x) < cargoGate.localScale.x * .5f + .5f;
        bool holdOpen = underneath && cargoGate.position.y >= 3.8f;
        if (!holdOpen)
            cycle += Time.deltaTime;
        float lift = holdOpen ? 1f : Mathf.Clamp01((Mathf.Sin(cycle * 1.7f) + .3f) * 1.7f);
        Vector3 gatePosition = cargoGate.position;
        gatePosition.y = Mathf.MoveTowards(gatePosition.y,1.5f + lift * 3.2f,Time.deltaTime*5f);
        cargoGate.position = gatePosition;

        if (!Cleared && Vector3.Distance(player.transform.position,dispatch.position) < 2.8f &&
            Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Release("Guia de carga carimbada");
        if (Cleared && Vector3.Distance(player.transform.position,exit.position) < 2.2f)
        {
            loading = true;
            SceneManager.LoadSceneAsync("Assets/Project/Scenes/PatioFerroviario.unity");
        }
    }

    private void OnGUI()
    {
        if (!player) return;
        string instruction = hint;
        if (!Cleared && player.transform.position.z > 9f)
            instruction = player.transform.position.x < 0f
                ? "Manutenção • Use E junto ao relé para abrir a passagem."
                : "Carga • Atravesse a cancela quando subir. Use R no posto elevado.";
        if (Keyboard.current != null && Keyboard.current.qKey.isPressed) DrawPlan();
        else instruction += "   [Q] Planta";
        float width = Mathf.Min(680f,Screen.width-24f);
        GUI.Box(new Rect((Screen.width-width)*.5f,Screen.height-72f,width,48f),instruction);
    }

    private void DrawPlan()
    {
        float scale = Mathf.Min(4f,(Screen.height-130f)/68f,(Screen.width-50f)/50f);
        if (scale <= 0f) return;
        Vector2 center = new Vector2(Screen.width*.5f,Screen.height*.5f);
        Vector2 Map(float x,float z) => center + new Vector2(x,26f-z)*scale;
        void Zone(float x,float z,float w,float d,Color color)
        {
            Vector2 top = Map(x-w*.5f,z+d*.5f);
            GUI.color = color;
            GUI.DrawTexture(new Rect(top.x,top.y,w*scale,d*scale),Texture2D.whiteTexture);
        }
        void Label(string value,float x,float z)
        {
            Vector2 pos = Map(x,z);
            GUI.color=Color.white;
            GUI.Label(new Rect(pos.x-44,pos.y-10,100,24),value);
        }
        Color previous = GUI.color;
        Zone(0,26,45,65,new Color(.08f,.12f,.14f,.96f));
        Zone(0,12.5f,13,4,Color.gray);
        Zone(0,29.5f,13,20,Color.gray);
        Zone(-14,26,3,3,Color.cyan);
        Zone(14,40,3,3,new Color(1f,.68f,.27f));
        Zone(0,54,4,2,Cleared?Color.green:Color.gray);
        Label("Expedição",0,58);
        Label("Relé",-12,31);
        Label("Despacho",15,45);
        Vector3 current = player.transform.position;
        Zone(current.x,current.z,2.2f,2.2f,Color.white);
        Label("Você",current.x,current.z-4);
        GUI.color=previous;
    }
}
