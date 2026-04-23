using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public enum StageResult
    {
        None,
        Win,
        Loss
    }

    [Header("Stage (overridden by SceneFlowManager if available)")]
    public StageData currentStage;

    public StageResult Result { get; private set; }
    public int StarsEarned { get; private set; }

    public event Action<StageResult, int> OnStageEnded;

    private bool subscribedSpawner;
    private bool subscribedHealth;

    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void EnsureStageMusic(StageData stage)
    {
        if (stage == null || stage.backgroundMusic == null) return;

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        if (musicSource.clip == stage.backgroundMusic && musicSource.isPlaying) return;

        musicSource.clip = stage.backgroundMusic;
        musicSource.Play();
    }

    private void StopStageMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    private void Update()
    {
        if (!subscribedSpawner && EnemySpawner.Instance != null)
        {
            if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.SelectedStage != null)
                currentStage = SceneFlowManager.Instance.SelectedStage;

            if (currentStage != null)
            {
                EnemySpawner.Instance.Configure(currentStage);

                if (ObstacleSpawner.Instance != null)
                    ObstacleSpawner.Instance.Configure(currentStage);

                EnsureStageMusic(currentStage);
            }

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

    private void HandleWin()
    {
        if (Result != StageResult.None) return;

        Result = StageResult.Win;
        StarsEarned = Mathf.Clamp(PlayerHealth.Instance.CurrentHearts, 1, 3);
        Time.timeScale = 0f;
        StopStageMusic();
        OnStageEnded?.Invoke(Result, StarsEarned);
    }

    private void HandleLoss()
    {
        if (Result != StageResult.None) return;

        Result = StageResult.Loss;
        StarsEarned = 0;
        Time.timeScale = 0f;
        StopStageMusic();
        OnStageEnded?.Invoke(Result, StarsEarned);
    }

    private void OnDestroy()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnAllWavesComplete -= HandleWin;

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnPlayerDied -= HandleLoss;
    }
}



