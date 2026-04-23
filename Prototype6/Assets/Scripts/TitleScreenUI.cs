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
            playButton.onClick.AddListener(GoToLobby);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(GoToLobby);
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;

        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToLobby();
        else
            SceneManager.LoadScene("LobbyScene");
    }
}
