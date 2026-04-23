using System;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Leveling - Base Cost")]
    [Tooltip("XP required for the first level-up (level 1 -> 2).")]
    public int baseXP = 5;

    [Header("Linear Scaling")]
    [Tooltip("XP added to the cost after every N level-ups. Set to 0 to disable linear scaling.")]
    public int xpIncrement = 0;

    [Tooltip("How many level-ups must happen before +xpIncrement is applied. 1 = every level, 2 = every other level, etc. Must be >= 1.")]
    public int levelsPerIncrement = 1;

    [Header("Exponential Scaling")]
    [Tooltip("Multiplier compounded each level. 1.0 = no exponential growth. 1.5 = each level costs 50% more than the last (stacks on top of linear scaling).")]
    public float growthMultiplier = 1f;

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
        XPToNextLevel = ComputeXPCost(CurrentLevel);
        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }

    public void AddXP(int amount)
    {
        CurrentXP += amount;

        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = ComputeXPCost(CurrentLevel);
            OnLevelUp?.Invoke(CurrentLevel);
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }

    // Cost to level up FROM `level` to `level + 1`.
    //   linearCost = baseXP + floor((level - 1) / levelsPerIncrement) * xpIncrement
    //   final      = linearCost * growthMultiplier ^ (level - 1)
    int ComputeXPCost(int level)
    {
        int step = Mathf.Max(1, levelsPerIncrement);
        int stepsCompleted = (level - 1) / step;
        int linearCost = baseXP + stepsCompleted * xpIncrement;
        float scaled = linearCost * Mathf.Pow(growthMultiplier, level - 1);
        return Mathf.Max(1, Mathf.RoundToInt(scaled));
    }
}
