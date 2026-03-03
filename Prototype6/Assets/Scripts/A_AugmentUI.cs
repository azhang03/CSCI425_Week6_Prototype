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
    private bool isShowing;
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Hide();

        if (A_XPManager.Instance != null)
            A_XPManager.Instance.OnLevelUp += ShowAugmentSelection;
    }

    void OnDestroy()
    {
        if (A_XPManager.Instance != null)
            A_XPManager.Instance.OnLevelUp -= ShowAugmentSelection;
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
        if (A_AugmentPool.Instance == null) return;

        List<A_AugmentData> cards = A_AugmentPool.Instance.GetCards(3);
        if (cards.Count == 0) return;

        Time.timeScale = 0f;
        isShowing = true;
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

    GameObject CreateCard(A_AugmentData data)
    {
        GameObject cardObj = new GameObject(data.augmentName + "_Card", typeof(RectTransform));
        cardObj.transform.SetParent(transform, false);

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = cardSize;

        Image bgImage = cardObj.AddComponent<Image>();
        bgImage.color = cardBackgroundColor;

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = cardOutlineDefault;
        outline.effectDistance = new Vector2(3, 3);

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
        A_AugmentCard card = cardObj.AddComponent<A_AugmentCard>();
        card.backgroundImage = bgImage;
        card.outline = outline;
        card.titleText = titleTMP;
        card.descriptionText = descTMP;
        card.defaultOutlineColor = cardOutlineDefault;
        card.hoverOutlineColor = cardOutlineHover;
        card.Setup(data, this);

        activeCards.Add(cardObj);
        return cardObj;
    }

    public void OnCardSelected(A_AugmentData data)
    {
        if (!isShowing) return;

        A_AugmentPool.Instance.ApplyAugment(data);

        ClearCards();
        isShowing = false;
        Hide();

        if (dimOverlay != null)
            dimOverlay.SetActive(false);

        Time.timeScale = 1f;
    }
}
