using UnityEngine;

public static class CurrencyManager
{
    const string KEY = "player_coins";

    public static int GetCoins() => PlayerPrefs.GetInt(KEY, 0);

    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt(KEY, GetCoins() + amount);
        PlayerPrefs.Save();
    }

    // Returns false if insufficient — caller must check before spending
    public static bool SpendCoins(int amount)
    {
        int current = GetCoins();
        if (current < amount) return false;
        PlayerPrefs.SetInt(KEY, current - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
