using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    const string LOBBY_SCENE = "LobbyScene";
    const string GAMEPLAY_SCENE = "Andrew_Scene";

    public List<StageData> allStages = new List<StageData>();
    public StageData SelectedStage { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToStage(StageData stage)
    {
        SelectedStage = stage;
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.Transition(GAMEPLAY_SCENE);
        else
            SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.Transition(LOBBY_SCENE);
        else
            SceneManager.LoadScene(LOBBY_SCENE);
    }

    public void RetryCurrentStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    public StageData GetNextStage()
    {
        if (SelectedStage == null) return null;

        for (int i = 0; i < allStages.Count; i++)
        {
            if (allStages[i] == SelectedStage && i + 1 < allStages.Count)
                return allStages[i + 1];
        }
        return null;
    }

    // ── Shop purchases ────────────────────────────────────────────────────────
    private List<AugmentData> _shopPurchases = new List<AugmentData>();

    public void AddShopPurchase(AugmentData augment)
    {
        if (!_shopPurchases.Contains(augment))
            _shopPurchases.Add(augment);
    }

    public List<AugmentData> GetShopPurchases() => _shopPurchases;

    public void ClearShopPurchases() => _shopPurchases.Clear();

    // Called once from ShopUI.Open() to restore purchases saved in PlayerPrefs.
    // ShopUI passes every ShopItem it knows about so we can match by name.
    public void RestoreShopPurchases(List<ShopItem> allItems)
    {
        foreach (var item in allItems)
        {
            if (item.augment == null) continue;
            if (PlayerPrefs.GetInt("shop_" + item.augment.augmentName, 0) == 1)
            {
                item.purchased = true;
                AddShopPurchase(item.augment);
            }
        }
    }
}
