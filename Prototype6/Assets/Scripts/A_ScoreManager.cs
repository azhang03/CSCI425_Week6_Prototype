using System;
using UnityEngine;

public class A_ScoreManager : MonoBehaviour
{
    public static A_ScoreManager Instance { get; private set; }

    public int KillCount { get; private set; }

    public event Action<int> OnScoreChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddKill()
    {
        KillCount++;
        OnScoreChanged?.Invoke(KillCount);
    }
}
