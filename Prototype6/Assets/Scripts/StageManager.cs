using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public enum StageResult { None, Win, Loss }

    [Header("Stage (overridden by SceneFlowManager if available)")]
    public StageData currentStage;

    public StageResult Result { get; private set; }
    public int StarsEarned { get; private set; }

    public event Action<StageResult, int> OnStageEnded;

    private bool subscribedSpawner;
    private bool subscribedHealth;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!subscribedSpawner && EnemySpawner.Instance != null)
        {
            if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.SelectedStage != null)
                currentStage = SceneFlowManager.Instance.SelectedStage;

            if (currentStage != null)
                EnemySpawner.Instance.Configure(currentStage);

            EnemySpawner.Instance.OnAllWavesComplete += HandleWin;
            subscribedSpawner = true;
        }

        if (!subscribedHealth && PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnPlayerDied += HandleLoss;
            subscribedHealth = true;
        }
    }

    public void ForceWin() => HandleWin();

    void HandleWin()
    {
        if (Result != StageResult.None) return;

        Result = StageResult.Win;
        StarsEarned = Mathf.Clamp(PlayerHealth.Instance.CurrentHearts, 1, 3);
        Time.timeScale = 0f;
        OnStageEnded?.Invoke(Result, StarsEarned);
    }

    void HandleLoss()
    {
        if (Result != StageResult.None) return;

        Result = StageResult.Loss;
        StarsEarned = 0;
        Time.timeScale = 0f;
        OnStageEnded?.Invoke(Result, StarsEarned);
    }

    void OnDestroy()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnAllWavesComplete -= HandleWin;

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnPlayerDied -= HandleLoss;
    }
}
