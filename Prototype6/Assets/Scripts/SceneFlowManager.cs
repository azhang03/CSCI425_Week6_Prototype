using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    const string LOBBY_SCENE = "LobbyScene";
    const string GAMEPLAY_SCENE = "Andrew_Scene";

    public List<StageData> allStages = new List<StageData>();
    public StageData SelectedStage { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToStage(StageData stage)
    {
        SelectedStage = stage;
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LOBBY_SCENE);
    }

    public void RetryCurrentStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public StageData GetNextStage()
    {
        if (SelectedStage == null) return null;

        for (int i = 0; i < allStages.Count; i++)
        {
            if (allStages[i] == SelectedStage && i + 1 < allStages.Count)
                return allStages[i + 1];
        }
        return null;
    }
}
