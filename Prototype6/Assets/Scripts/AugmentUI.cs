using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class A_AugmentUI : MonoBehaviour
{
    [Header("Card Settings")]
    public Vector2 cardSize = new Vector2(180f, 260f);
    public float cardSpacing = 30f;
    public Color cardBackgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    public Color cardOutlineDefault = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color cardOutlineHover = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("References")]
    public GameObject dimOverlay;

    private List<GameObject> activeCards = new List<GameObject>();
    public bool IsShowing { get; private set; }
    private CanvasGroup canvasGroup;
    private bool subscribedToLevelUp = false;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Hide();
        TrySubscribe();
    }

    void Update()
    {
        if (!subscribedToLevelUp)
            TrySubscribe();

        if (IsShowing && !PauseMenu.IsPaused && UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
            Reroll();
    }

    void TrySubscribe()
    {
        if (PauseMenu.AugmentsEnabled && XPManager.Instance != null && !subscribedToLevelUp)
        {
            XPManager.Instance.OnLevelUp += ShowAugmentSelection;
            subscribedToLevelUp = true;
        }
    }

    void Reroll()
    {
        ClearCards();

        if (AugmentPool.Instance == null) return;
        List<AugmentData> cards = AugmentPool.Instance.GetCards(3);
        if (cards.Count == 0) return;

        foreach (var augment in cards)
            CreateCard(augment);
    }

    void OnDestroy()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnLevelUp -= ShowAugmentSelection;
    }

    void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    void ShowAugmentSelection(int newLevel)
    {
        if (StageManager.Instance != null && StageManager.Instance.Result != StageManager.StageResult.None) return;
        if (AugmentPool.Instance == null) return;

        List<AugmentData> cards = AugmentPool.Instance.GetCards(3);
        if (cards.Count == 0) return;

        Time.timeScale = 0f;
        IsShowing = true;
        Show();

        if (dimOverlay != null)
            dimOverlay.SetActive(true);

        ClearCards();

        foreach (var augment in cards)
            CreateCard(augment);
    }

    void ClearCards()
    {
        foreach (var card in activeCards)
            Destroy(card);
        activeCards.Clear();
    }

    void GetAugmentColors(AugmentData data, out Color bg, out Color outlineIdle, out Color outlineHover)
    {
        bg = cardBackgroundColor;
        outlineIdle = cardOutlineDefault;

        // Prismatic (top-strength) tier: idle matches the normal grey; hover reveals pearl cyan
        // (and the sibling chroma outline reveals pink — handled in CreateCard).
        if (data.isPrismatic)
        {
            outlineHover = new Color(0.70f, 0.92f, 1.00f, 1f);
            return;
        }

        switch (data.type)
        {
            case AugmentType.NewWeapon:
                outlineHover = new Color(1.00f, 0.85f, 0.30f, 1f); // original yellow
                break;
            case AugmentType.ModifyHealth:
                outlineHover = new Color(1.00f, 0.30f, 0.30f, 1f); // red
                break;
            default: // ModifyWeapon, Tradeoff, ModifyAllWeapons, ModifyFireInterval, ModifyWeaponStat
                outlineHover = new Color(0.30f, 0.75f, 1.00f, 1f); // blue
                break;
        }
    }

    GameObject CreateCard(AugmentData data)
    {
        GameObject cardObj = new GameObject(data.augmentName + "_Card", typeof(RectTransform));
        cardObj.transform.SetParent(transform, false);

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = cardSize;

        GetAugmentColors(data, out Color bg, out Color outlineIdle, out Color outlineHover);

        Image bgImage = cardObj.AddComponent<Image>();
        bgImage.color = bg;

        // Prismatic cards stack an outer pink Outline under an inner cyan Outline (both centered).
        // Idle, both are the default grey so the card blends in with normal ones; hover reveals
        // the pink + pearl-cyan chroma via AugmentCard's hover swap.
        Outline chromaOutline = null;
        if (data.isPrismatic)
        {
            chromaOutline = cardObj.AddComponent<Outline>();
            chromaOutline.effectColor = cardOutlineDefault;
            chromaOutline.effectDistance = new Vector2(3, 3);
        }

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = outlineIdle;
        outline.effectDistance = new Vector2(data.isPrismatic ? 2 : 3, data.isPrismatic ? 2 : 3);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(cardObj.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = data.augmentName;
        titleTMP.fontSize = 20;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        // Description
        GameObject descObj = new GameObject("Description", typeof(RectTransform));
        descObj.transform.SetParent(cardObj.transform, false);

        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.1f);
        descRect.anchorMax = new Vector2(1, 0.65f);
        descRect.offsetMin = new Vector2(12, 0);
        descRect.offsetMax = new Vector2(-12, 0);

        TextMeshProUGUI descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.text = data.description;
        descTMP.fontSize = 14;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        descTMP.textWrappingMode = TextWrappingModes.Normal;

        // Card script
        AugmentCard card = cardObj.AddComponent<AugmentCard>();
        card.backgroundImage = bgImage;
        card.outline = outline;
        card.titleText = titleTMP;
        card.descriptionText = descTMP;
        card.defaultOutlineColor = outlineIdle;
        card.hoverOutlineColor = outlineHover;
        if (chromaOutline != null)
        {
            card.chromaOutline = chromaOutline;
            card.chromaDefaultColor = cardOutlineDefault;
            card.chromaHoverColor   = new Color(1.00f, 0.55f, 0.90f, 0.90f);
        }
        card.Setup(data, this);

        activeCards.Add(cardObj);
        return cardObj;
    }

    public void OnCardSelected(AugmentData data)
    {
        if (!IsShowing) return;

        AugmentPool.Instance.ApplyAugment(data);

        ClearCards();
        IsShowing = false;
        Hide();

        if (dimOverlay != null)
            dimOverlay.SetActive(false);

        if (StageManager.Instance == null || StageManager.Instance.Result == StageManager.StageResult.None)
            Time.timeScale = 1f;
    }
}
