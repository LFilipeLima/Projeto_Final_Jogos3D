using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class RailTransfer : MonoBehaviour
{
    [SerializeField] private Transform cart;
    [SerializeField] private Transform junction;
    [SerializeField] private Transform unloading;
    [SerializeField] private Transform switchStand;
    [SerializeField] private Transform arenaEntrance;
    private Vector3 start;
    private CharacterController player;
    private int station;
    private bool moving;
    private bool aligned;
    private bool loading;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>().GetComponent<CharacterController>();
        start = cart.position;
    }

    private bool Aboard()
    {
        Vector3 local = player.transform.position - cart.position;
        return Mathf.Abs(local.x) < 2.85f && Mathf.Abs(local.z) < 2.85f &&
               local.y > 0f && local.y < 2.1f;
    }

    private void Update()
    {
        if (!player || loading) return;
        if (player.transform.position.y < -4f)
        {
            moving = false;
            cart.position = station == 0 ? start : station == 1 ? junction.position : unloading.position;
            player.enabled = false;
            player.transform.position = cart.position + Vector3.up * 1.4f;
            player.enabled = true;
        }
        bool aboard = Aboard();
        if (moving)
        {
            Vector3 target = station == 0 ? junction.position : unloading.position;
            Vector3 next = Vector3.MoveTowards(cart.position,target,5f * Time.deltaTime);
            Vector3 displacement = next - cart.position;
            cart.position = next;
            Physics.SyncTransforms();
            if (aboard) player.Move(displacement);
            if (next == target) { moving = false; station++; }
            return;
        }
        if (station == 2 && Vector3.Distance(player.transform.position,arenaEntrance.position) < 2.2f)
        {
            loading = true;
            SceneManager.LoadSceneAsync("Assets/Project/Scenes/MainArena.unity");
            return;
        }
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
        if (station == 1 && !aligned && Vector3.Distance(player.transform.position,switchStand.position) < 2.4f)
        {
            aligned = true;
            switchStand.localRotation = Quaternion.Euler(0,0,35f);
        }
        else if (aboard && (station == 0 || (station == 1 && aligned))) moving = true;
    }

    private void OnGUI()
    {
        if (!player || loading) return;
        string text;
        if (moving) text = station == 0 ? "Próxima parada: torre de manobra." : "Próxima parada: desembarque do galpão.";
        else if (station == 2) text = "Desembarque pelo lado leste e siga a doca até a porta GALPÃO.";
        else if (station == 1 && !aligned) text = "Desça à esquerda na ilha. Use R na alavanca para alinhar o ramal leste.";
        else text = Aboard() ? "R • Despachar a plataforma" : "Embarque na plataforma da remessa 17.";
        float w = Mathf.Min(660,Screen.width-20);
        GUI.Box(new Rect((Screen.width-w)/2,Screen.height-73,w,47),text);
    }
}
