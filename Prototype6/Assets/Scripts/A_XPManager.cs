using System;
using UnityEngine;

public class A_XPManager : MonoBehaviour
{
    public static A_XPManager Instance { get; private set; }

    [Header("Leveling Curve")]
    public int baseXP = 5;
    public float growthFactor = 1.5f;

    public int CurrentXP { get; private set; }
    public int XPToNextLevel { get; private set; }
    public int CurrentLevel { get; private set; }

    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CurrentLevel = 1;
        CurrentXP = 0;
        XPToNextLevel = baseXP;
        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }

    public void AddXP(int amount)
    {
        CurrentXP += amount;

        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = Mathf.RoundToInt(baseXP * Mathf.Pow(growthFactor, CurrentLevel - 1));
            OnLevelUp?.Invoke(CurrentLevel);
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }
}
