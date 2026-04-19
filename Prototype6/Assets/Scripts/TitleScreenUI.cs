using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button lobbyButton;

    void Start()
    {
        Time.timeScale = 1f;

        if (playButton != null)
            playButton.onClick.AddListener(GoToStage1);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(GoToLobby);
    }

    void GoToStage1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Andrew_Scene");
    }

    void GoToLobby()
    {
        Time.timeScale = 1f;

        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToLobby();
        else
            SceneManager.LoadScene("LobbyScene");
    }
}
