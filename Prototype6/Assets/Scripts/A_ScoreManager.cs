using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class A_ScoreManager : MonoBehaviour
{
    public static A_ScoreManager Instance { get; private set; }

    public int KillCount { get; private set; }

    private static int _highScore = 0;
    public int highScore
    {
        get => _highScore;
        set => _highScore = value;
    }

    public event Action<int> OnScoreChanged;

    void Awake()
    {
        Instance = this;
        KillCount = 0;
    }

    public void AddScore(int points)
    {
        KillCount += points;
        OnScoreChanged?.Invoke(KillCount);
    }

    public void AddKill()
    {
        AddScore(1);
    }

    public void EndGame()
    {
        if (KillCount > _highScore)
            _highScore = KillCount;
    }

    public void ResetGame()
    {
        if (KillCount > _highScore)
            _highScore = KillCount;

        KillCount = 0;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
