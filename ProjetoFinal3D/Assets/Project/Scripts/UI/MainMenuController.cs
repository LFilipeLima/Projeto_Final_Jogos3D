using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button controlsBackButton;
    [SerializeField] private Button beginMissionButton;
    [SerializeField] private Button missionBackButton;

    private void Awake()
    {
        if (startButton) startButton.onClick.AddListener(ShowMission);
        if (controlsButton) controlsButton.onClick.AddListener(ShowControls);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        if (controlsBackButton) controlsBackButton.onClick.AddListener(ShowTitle);
        if (beginMissionButton) beginMissionButton.onClick.AddListener(BeginMission);
        if (missionBackButton) missionBackButton.onClick.AddListener(ShowTitle);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowTitle();
    }

    public void ShowTitle() => ShowPanel(titlePanel, startButton);

    public void ShowControls() => ShowPanel(controlsPanel, controlsBackButton);

    public void ShowMission() => ShowPanel(missionPanel, beginMissionButton);

    public void BeginMission()
    {
        SceneManager.LoadScene("Protocolo");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void ShowPanel(GameObject panel, Selectable firstSelection)
    {
        if (titlePanel) titlePanel.SetActive(panel == titlePanel);
        if (controlsPanel) controlsPanel.SetActive(panel == controlsPanel);
        if (missionPanel) missionPanel.SetActive(panel == missionPanel);

        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(firstSelection ? firstSelection.gameObject : null);
    }
}
