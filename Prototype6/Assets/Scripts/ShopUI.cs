using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ── ShopUI ───────────────────────────────────────────────────────────────────
// Lobby shop panel. Paginated carousel of unpurchased augment items.
// Show/hide via CanvasGroup (not SetActive) so MonoBehaviour subscriptions survive.
public class ShopUI : MonoBehaviour
{
    [Header("Shop Items")]
    public List<ShopItem> shopItems = new List<ShopItem>();

    [Header("Navigation")]
    public Button prevButton;
    public Button nextButton;
    public int cardsPerPage = 3;

    [Header("References")]
    public TextMeshProUGUI coinDisplay;
    public Transform cardContainer;
    public GameObject dimOverlay;

    private CanvasGroup canvasGroup;
    private int currentPage = 0;
    private readonly List<GameObject> activeCards = new List<GameObject>();

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(OnPrev);
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        Hide();

        // Restore purchases from PlayerPrefs into SceneFlowManager immediately on scene load,
        // so AugmentPool.Start() has the correct purchase list even if the shop is never opened.
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.RestoreShopPurchases(shopItems);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.RestoreShopPurchases(shopItems);

        currentPage = 0;
        ShowPage(currentPage);
        RefreshCoinDisplay();
        Show();

        if (dimOverlay != null)
            dimOverlay.SetActive(true);
    }

    public void Close()
    {
        Hide();
        if (dimOverlay != null)
            dimOverlay.SetActive(false);
    }

    public void ResetPurchases()
    {
        foreach (var item in shopItems)
        {
            if (item.augment == null) continue;
            item.purchased = false;
            PlayerPrefs.DeleteKey("shop_" + item.augment.augmentName);
        }
        PlayerPrefs.Save();
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.ClearShopPurchases();
    }

    public void TryPurchase(ShopItem item)
    {
        if (!CurrencyManager.SpendCoins(item.price)) return;

        item.purchased = true;
        PlayerPrefs.SetInt("shop_" + item.augment.augmentName, 1);
        PlayerPrefs.Save();

        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.AddShopPurchase(item.augment);

        // If the current page is now past the end, step back one
        List<ShopItem> remaining = GetUnpurchased();
        int totalPages = remaining.Count == 0 ? 0 : Mathf.CeilToInt((float)remaining.Count / cardsPerPage);
        if (totalPages > 0 && currentPage >= totalPages)
            currentPage = totalPages - 1;

        ShowPage(currentPage);
        RefreshCoinDisplay();
        CoinTrackerUI.Instance?.Refresh();
    }

    // ── Page display ──────────────────────────────────────────────────────────

    void ShowPage(int page)
    {
        ClearCards();

        List<ShopItem> unpurchased = GetUnpurchased();

        if (unpurchased.Count == 0)
        {
            BuildEmptyLabel();
            SetArrows(false, false);
            return;
        }

        int totalPages = Mathf.CeilToInt((float)unpurchased.Count / cardsPerPage);
        currentPage = Mathf.Clamp(page, 0, totalPages - 1);

        int start = currentPage * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, unpurchased.Count);

        for (int i = start; i < end; i++)
            BuildCard(unpurchased[i]);

        SetArrows(currentPage > 0, currentPage < totalPages - 1);
    }

    void OnPrev() => ShowPage(currentPage - 1);
    void OnNext() => ShowPage(currentPage + 1);

    // ── Card building ─────────────────────────────────────────────────────────

    void BuildCard(ShopItem item)
    {
        if (cardContainer == null) return;

        bool affordable = CurrencyManager.GetCoins() >= item.price;

        GameObject cardObj = new GameObject(item.augment.augmentName + "_Card", typeof(RectTransform));
        cardObj.transform.SetParent(cardContainer, false);
        cardObj.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 620f);

        // Background — always dark grey, matching AugmentUI
        Image bgImage = cardObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        // Outline — type-colored and always visible on affordable, dark on unaffordable.
        // Prismatic augments (top-strength tier) override with pearl-cyan + pink chromatic shadow.
        bool prismatic = item.augment.isPrismatic && affordable;
        Color typeColor = prismatic
            ? new Color(0.70f, 0.92f, 1.00f, 1f)  // pearl cyan idle
            : GetCardColor(item.augment.type);
        Color normalOutline = affordable ? typeColor   : new Color(0.25f, 0.25f, 0.25f, 1f);
        Color hoverOutline  = affordable
            ? (prismatic ? new Color(1.00f, 0.95f, 0.80f, 1f) : Color.white)  // pearl gold on hover when prismatic
            : normalOutline;

        // For prismatic cards, an outer pink Outline sits behind the inner cyan Outline to
        // produce a chromatic-edge ring. Both are centered (symmetric effectDistance), so the
        // card's visual footprint stays identical to non-prismatic cards (total 4px outline).
        if (prismatic)
        {
            Outline chroma = cardObj.AddComponent<Outline>();
            chroma.effectColor = new Color(1.00f, 0.55f, 0.90f, 0.90f);
            chroma.effectDistance = new Vector2(4, 4);
        }

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = normalOutline;
        outline.effectDistance = new Vector2(prismatic ? 3 : 4, prismatic ? 3 : 4);

        // Title — white, matching AugmentUI
        MakeLabel(cardObj, "Title",
            item.augment.augmentName, 36, FontStyles.Bold, Color.white,
            new Vector2(0f, 0.80f), new Vector2(1f, 0.97f), 16f);

        // Description — light grey, matching AugmentUI
        MakeLabel(cardObj, "Description",
            item.augment.description, 26, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0f, 0.20f), new Vector2(1f, 0.76f), 20f,
            wrap: true, autoSize: true);

        // Cost
        MakeLabel(cardObj, "Cost",
            $"$ {item.price}", 34, FontStyles.Bold,
            affordable ? Color.white : new Color(1f, 0.3f, 0.3f),
            new Vector2(0f, 0.03f), new Vector2(1f, 0.17f), 16f);

        if (affordable)
        {
            Button btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = bgImage;

            ShopCard shopCard = cardObj.AddComponent<ShopCard>();
            shopCard.item = item;
            shopCard.owner = this;
            shopCard.outline = outline;
            shopCard.normalColor = normalOutline;
            shopCard.hoverColor = hoverOutline;
            btn.onClick.AddListener(shopCard.OnClick);
        }

        activeCards.Add(cardObj);
    }

    void BuildEmptyLabel()
    {
        if (cardContainer == null) return;

        GameObject obj = new GameObject("EmptyLabel", typeof(RectTransform));
        obj.transform.SetParent(cardContainer, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 60f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Nothing left to buy";
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.6f, 0.6f, 0.6f);

        activeCards.Add(obj);
    }

    // Anchor-based label helper — mirrors ResultsScreen / AugmentUI patterns
    TextMeshProUGUI MakeLabel(GameObject parent, string goName, string text,
        float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, float hPadding = 10f,
        bool wrap = false, bool autoSize = false)
    {
        GameObject obj = new GameObject(goName, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(hPadding, 0f);
        rt.offsetMax = new Vector2(-hPadding, 0f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (wrap) tmp.textWrappingMode = TextWrappingModes.Normal;
        if (autoSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = fontSize;
        }
        else
        {
            tmp.fontSize = fontSize;
        }

        return tmp;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    List<ShopItem> GetUnpurchased()
    {
        var result = new List<ShopItem>();
        foreach (var item in shopItems)
            if (!item.purchased) result.Add(item);
        return result;
    }

    // Returns the hover glow colour for a card's outline — matches AugmentUI's scheme
    Color GetCardColor(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.NewWeapon:    return new Color(1.00f, 0.85f, 0.30f); // yellow
            case AugmentType.ModifyHealth: return new Color(1.00f, 0.30f, 0.30f); // red
            default:                       return new Color(0.30f, 0.75f, 1.00f); // blue
        }
    }

    void ClearCards()
    {
        foreach (var card in activeCards)
            if (card != null) Destroy(card);
        activeCards.Clear();
    }

    void RefreshCoinDisplay()
    {
        if (coinDisplay != null)
            coinDisplay.text = $"$ {CurrencyManager.GetCoins()}";
    }

    void SetArrows(bool hasPrev, bool hasNext)
    {
        SetArrowState(prevButton, hasPrev);
        SetArrowState(nextButton, hasNext);
    }

    void SetArrowState(Button btn, bool active)
    {
        if (btn == null) return;
        btn.interactable = active;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = active ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
