using UnityEngine;
using TMPro;

public class CoinTrackerUI : MonoBehaviour
{
    public static CoinTrackerUI Instance { get; private set; }

    public TextMeshProUGUI label;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable() => Refresh();

    public void Refresh()
    {
        if (label != null)
            label.text = $"Coins: {CurrencyManager.GetCoins()}";
    }
}
