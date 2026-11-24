using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ArchiveEpilogue : MonoBehaviour
{
    private const string ProtocolScene = "Assets/Project/Scenes/Protocolo.unity";
    private static bool returning;
    private PlayerMovement movement;
    private Transform paper;
    private Vector3 printedPosition;
    private float printingTime;
    private bool arrived;
    public bool Filed { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => returning = false;

    public static void ReturnToDesk()
    {
        if (returning || !GameManager.Instance || GameManager.Instance.CurrentState != GameState.Victory) return;
        returning = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(ProtocolScene);
    }

    public static bool BeginIfReturning(ArchiveInspection desk)
    {
        if (!returning) return false;
        returning = false;
        desk.enabled = false;
        var ending = desk.gameObject.AddComponent<ArchiveEpilogue>();
        ending.paper = desk.Document;
        return true;
    }

    private void Start()
    {
        movement = FindFirstObjectByType<PlayerMovement>();
        movement.GetComponent<PlayerShooter>().enabled = false;
        printedPosition = paper.position;
        paper.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Filed)
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                enabled = false;
                SceneManager.LoadSceneAsync(ProtocolScene);
            }
            return;
        }

        bool near = (movement.transform.position - printedPosition).sqrMagnitude <= 2.4f * 2.4f;
        if (near && !arrived)
        {
            arrived = true;
            paper.gameObject.SetActive(true);
            var ink = new GameObject("Assinatura recuperada").AddComponent<TextMesh>();
            ink.transform.SetParent(paper, true);
            ink.transform.position = printedPosition + Vector3.up * 0.026f;
            ink.transform.rotation = Quaternion.Euler(90, 0, 0);
            ink.anchor = TextAnchor.MiddleCenter;
            ink.alignment = TextAlignment.Center;
            ink.fontSize = 48;
            ink.characterSize = 0.023f;
            ink.color = new Color(0.05f,0.08f,0.10f);
            ink.text = "RETER • CAIXA 17\nL. Nunes";
        }
        if (!arrived) return;
        printingTime += Time.deltaTime;
        paper.position = printedPosition + paper.forward * (0.6f * (1f - Mathf.Clamp01(printingTime)));
        if (!near || printingTime < 1f || Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
        Filed = true;
        paper.gameObject.SetActive(false);
        movement.enabled = false;
        Camera.main.GetComponent<PlayerCamera>().enabled = false;
    }

    private void OnGUI()
    {
        string message = Filed ? "OCORRÊNCIA Nº 0001\nOriginal arquivado com a margem assinada.\n\nARQUIVO 88   •   FIM\nEnter para começar novamente" :
            !arrived ? "Protocolo • Volte ao balcão para receber o original." :
            printingTime < 1f ? "Imprimindo a folha de remessa…" :
            "“Reter. Caixa 17.” — L. Nunes\nAproxime-se do balcão e pressione R para arquivar.";
        float width = Mathf.Min(Screen.width-24, 540);
        GUI.Box(new Rect((Screen.width-width)/2f, Filed ? Screen.height*.35f : Screen.height-100,
            width, Filed ? 140 : 65), message);
    }
}
