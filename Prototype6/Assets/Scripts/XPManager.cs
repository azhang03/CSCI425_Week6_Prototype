using System;
using System.Collections;
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

    [Header("Level-Up Sequencing")]
    [Tooltip("Time in unscaled seconds the bar lingers at full before the level-up event fires (so the final-segment fill animation can play out).")]
    public float fillAnimationDelay = 0.35f;

    public int CurrentXP { get; private set; }
    public int XPToNextLevel { get; private set; }
    public int CurrentLevel { get; private set; }

    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;

    Coroutine levelUpCo;
    int       pendingOverflowXP;

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
        if (amount <= 0) return;

        // If a level-up sequence is already in flight, just stash the XP — it
        // will be re-added once the bar finishes shattering and resetting.
        if (levelUpCo != null)
        {
            pendingOverflowXP += amount;
            return;
        }

        CurrentXP += amount;

        if (CurrentXP >= XPToNextLevel)
        {
            // Stash any extra past the cap so the bar can show "exactly full"
            // for one beat. Add it back after the level-up sequence resolves.
            pendingOverflowXP += CurrentXP - XPToNextLevel;
            CurrentXP = XPToNextLevel;

            // Fire OnXPChanged with the bar at exactly full so the last
            // segment punches in.
            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);

            levelUpCo = StartCoroutine(LevelUpSequence());
            return;
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }

    IEnumerator LevelUpSequence()
    {
        // 1. Let the final-segment punch+fill animation play out.
        if (fillAnimationDelay > 0f)
            yield return new WaitForSecondsRealtime(fillAnimationDelay);

        // 2. Wrap XP and fire the level-up event. The augment UI listens
        //    to this and pauses the game (Time.timeScale = 0).
        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            XPToNextLevel = ComputeXPCost(CurrentLevel);
            OnLevelUp?.Invoke(CurrentLevel);
        }

        // 3. Wait for the augment UI to close (time resumes).
        //    Give the listeners one frame to actually pause first.
        yield return null;
        while (Time.timeScale < 0.99f)
            yield return null;

        // 4. Drain any pending XP banked while the sequence was running.
        int overflow = pendingOverflowXP;
        pendingOverflowXP = 0;
        if (overflow > 0)
            CurrentXP += overflow;

        // 5. Fire OnXPChanged with the post-wrap state. The XPBar treats
        //    this as a "level reset" (XP went down) and triggers the
        //    shatter + empty.
        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);

        levelUpCo = null;

        // 6. If the overflow itself triggered another level-up, recurse.
        if (CurrentXP >= XPToNextLevel)
        {
            int extra = CurrentXP - XPToNextLevel;
            pendingOverflowXP += extra;
            CurrentXP = XPToNextLevel;
            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
            levelUpCo = StartCoroutine(LevelUpSequence());
        }
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
