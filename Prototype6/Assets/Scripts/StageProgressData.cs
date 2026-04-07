using UnityEngine;

public static class StageProgressData
{
    public static bool IsCleared(int stageNumber)
    {
        return PlayerPrefs.GetInt($"Stage_{stageNumber}_cleared", 0) == 1;
    }

    public static int GetStars(int stageNumber)
    {
        return PlayerPrefs.GetInt($"Stage_{stageNumber}_stars", 0);
    }

    public static bool IsUnlocked(int stageNumber)
    {
        return stageNumber == 1 || IsCleared(stageNumber - 1);
    }

    public static void SaveResult(int stageNumber, int stars)
    {
        PlayerPrefs.SetInt($"Stage_{stageNumber}_cleared", 1);
        PlayerPrefs.SetInt($"Stage_{stageNumber}_stars", Mathf.Max(GetStars(stageNumber), stars));
        PlayerPrefs.Save();
    }

    public static void ClearAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
