using System;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;

public class A_ScoreManager : MonoBehaviour
{
    public static A_ScoreManager Instance { get; private set; }

    public int KillCount { get; private set; }

    public int highScore = 0;

    public event Action<int> OnScoreChanged;





    void Awake()
    {
        KillCount = 0;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
        if (KillCount > highScore)
        {
            highScore = KillCount;

        }

    }


    public void ResetGame()
    {

        if (KillCount > highScore)
        {
            highScore = KillCount;
        }
        KillCount = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }
}
