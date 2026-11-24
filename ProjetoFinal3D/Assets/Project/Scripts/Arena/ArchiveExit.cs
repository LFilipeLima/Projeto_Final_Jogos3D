using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ArchiveExit : MonoBehaviour
{
    [SerializeField] private ArchiveInspection inspection;
    [SerializeField] private GameObject shutter;
    [SerializeField] private TextMesh sign;
    [SerializeField] private string destination;
    private bool loading;

    private void Awake()
    {
        if (ArchiveEpilogue.BeginIfReturning(inspection))
        {
            sign.text = "PROTOCOLO   /   ORIGINAL DISPONÍVEL";
            enabled = false;
        }
    }

    private void OnEnable()
    {
        inspection.CorrectBoxRecovered += Open;
        if (inspection.BoxRecovered) Open();
    }

    private void OnDisable() => inspection.CorrectBoxRecovered -= Open;

    private void Open()
    {
        shutter.SetActive(false);
        sign.text = "GALPÃO   /   ENTRE";
        sign.color = new Color(0.45f, 1f, 0.78f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (loading || !inspection.BoxRecovered || !other.GetComponentInParent<PlayerHealth>()) return;
        loading = true;
        SceneManager.LoadSceneAsync(destination);
    }
}
