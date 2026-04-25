using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class StageSelectUI : MonoBehaviour
{
    [System.Serializable]
    public struct StageArtMapping
    {
        public int stageNumber;
        public Sprite recordSprite;
        public Sprite cornerSprite;
    }

    [Header("Text")]
    public TextMeshProUGUI titleText;

    [Header("Stars (3 Image components — drag star1, star2, star3)")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Carousel (optional)")]
    public Button leftRecordButton;
    public Button centerRecordButton;
    public Button rightRecordButton;
    public Image leftRecordImage;
    public Image centerRecordImage;
    public Image rightRecordImage;
    public Image leftLockOverlay;
    public Image centerLockOverlay;
    public Image rightLockOverlay;
    public Image centerCornerImage;
    public Sprite lockedOverlaySprite;
    public float sideScale = 0.88f;
    public float centerScale = 1.0f;
    public float hoverScaleMultiplier = 1.1f;
    public List<StageArtMapping> stageArt = new List<StageArtMapping>();

    [Header("Rotation Animation")]
    public float rotationDuration = 0.4f;
    public AnimationCurve rotationEasing = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private bool isRotating;

    [Header("Buttons")]
    public Button playButton;
    public Button shopButton;
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Lock Icon")]
    public GameObject lockIcon;

    [Header("Star Colors")]
    public Color earnedColor = new Color(1f, 0.84f, 0f);    // gold
    public Color unearnedColor = new Color(0.1f, 0.1f, 0.1f); // near-black silhouette

    private List<StageData> allStages;
    private int currentIndex;

    public bool debugMode = true;
    void Start()
    {
        if (SceneFlowManager.Instance == null) return;

        allStages = new List<StageData>(SceneFlowManager.Instance.allStages);
        allStages.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));

        // If carousel art mappings are provided, limit the selectable stages to those mappings.
        // This keeps the lobby carousel focused on the designed set (e.g., stages 1-3).
        if (stageArt != null && stageArt.Count > 0)
        {
            HashSet<int> allowed = new HashSet<int>();
            for (int i = 0; i < stageArt.Count; i++)
                allowed.Add(stageArt[i].stageNumber);

            allStages.RemoveAll(s => s == null || !allowed.Contains(s.stageNumber));
        }

        if (debugMode)
        {
            currentIndex = allStages.Count - 1;

        }
        else
        {
            currentIndex = 0;

        }

        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (leftArrowButton != null) leftArrowButton.onClick.AddListener(NavigateLeft);
        if (rightArrowButton != null) rightArrowButton.onClick.AddListener(NavigateRight);
        if (leftRecordButton != null) leftRecordButton.onClick.AddListener(NavigateLeft);
        if (rightRecordButton != null) rightRecordButton.onClick.AddListener(NavigateRight);

        if (leftRecordButton != null && leftRecordImage != null)
        {
            var dimmer = leftRecordButton.gameObject.AddComponent<HoverDimmer>();
            dimmer.target = leftRecordImage;
        }
        if (rightRecordButton != null && rightRecordImage != null)
        {
            var dimmer = rightRecordButton.gameObject.AddComponent<HoverDimmer>();
            dimmer.target = rightRecordImage;
        }

        EnsureHoverScaler(leftRecordButton);
        EnsureHoverScaler(centerRecordButton);
        EnsureHoverScaler(rightRecordButton);
        EnsureHoverScaler(playButton);
        EnsureHoverScaler(shopButton);

        RefreshDisplay();
    }

    void Update()
    {
        // DEBUG: P = save 3-star clear on current stage and refresh
        // DEBUG: O = clear all progress and refresh
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            StageData stage = allStages[currentIndex];
            StageProgressData.SaveResult(stage.stageNumber, 3);
            RefreshDisplay();
        }
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            StageProgressData.ClearAllProgress();
            CurrencyManager.Reset();
            FindAnyObjectByType<ShopUI>()?.ResetPurchases();
            CoinTrackerUI.Instance?.Refresh();
            RefreshDisplay();
        }
        // DEBUG: 6 = add 10 coins
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            CurrencyManager.AddCoins(10);
            CoinTrackerUI.Instance?.Refresh();
        }
    }

    void RefreshDisplay()
    {
        if (allStages == null || allStages.Count == 0) return;

        StageData stage = allStages[currentIndex];

        bool usingCarousel = HasCarousel();

        // Title
        if (titleText != null)
            titleText.text = $"STAGE {stage.stageNumber}\n{stage.stageName}";

        // Stars — tint each Image gold (earned) or black (unearned)
        int stars = StageProgressData.GetStars(stage.stageNumber);
        Image[] starImages = { star1, star2, star3 };
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
                starImages[i].color = (i < stars) ? earnedColor : unearnedColor;
        }

        // Arrows — show if more than 1 stage exists, greyed out if direction leads to locked stage
        bool multipleStages = allStages.Count > 1;
        if (leftArrowButton != null) leftArrowButton.gameObject.SetActive(multipleStages && !usingCarousel);
        if (rightArrowButton != null) rightArrowButton.gameObject.SetActive(multipleStages && !usingCarousel);

        if (multipleStages && !usingCarousel)
        {
            // Check if navigating right leads to an unlocked stage
            int nextIndex = (currentIndex + 1) % allStages.Count;
            bool rightUnlocked = StageProgressData.IsUnlocked(allStages[nextIndex].stageNumber);
            if (rightArrowButton != null) SetButtonGreyscale(rightArrowButton, rightUnlocked);

            // Check if navigating left leads to an unlocked stage
            int prevIndex = (currentIndex - 1 + allStages.Count) % allStages.Count;
            bool leftUnlocked = StageProgressData.IsUnlocked(allStages[prevIndex].stageNumber);
            if (leftArrowButton != null) SetButtonGreyscale(leftArrowButton, leftUnlocked);
        }

        // Lock icon — show next to right arrow if next sequential stage is locked
        if (lockIcon != null)
        {
            if (usingCarousel)
            {
                lockIcon.SetActive(false);
            }
            else
            {
            int nextStageNumber = stage.stageNumber + 1;
            bool nextExists = false;
            foreach (var s in allStages)
                if (s.stageNumber == nextStageNumber) { nextExists = true; break; }

            lockIcon.SetActive(nextExists && !StageProgressData.IsUnlocked(nextStageNumber));
            }
        }

        // Play button — only for unlocked stages
        if (playButton != null)
            playButton.gameObject.SetActive(StageProgressData.IsUnlocked(stage.stageNumber));

        RefreshCarousel();
    }

    void NavigateRight()
    {
        if (isRotating) return;
        if (allStages == null || allStages.Count == 0) return;
        if (HasCarousel() && allStages.Count > 1)
        {
            StartCoroutine(AnimateRotation(1));
        }
        else
        {
            currentIndex = (currentIndex + 1) % allStages.Count;
            RefreshDisplay();
        }
    }

    void NavigateLeft()
    {
        if (isRotating) return;
        if (allStages == null || allStages.Count == 0) return;
        if (HasCarousel() && allStages.Count > 1)
        {
            StartCoroutine(AnimateRotation(-1));
        }
        else
        {
            currentIndex = (currentIndex - 1 + allStages.Count) % allStages.Count;
            RefreshDisplay();
        }
    }

    IEnumerator AnimateRotation(int direction)
    {
        isRotating = true;
        int count = allStages.Count;

        RectTransform leftRT = leftRecordButton.GetComponent<RectTransform>();
        RectTransform centerRT = centerRecordButton.GetComponent<RectTransform>();
        RectTransform rightRT = rightRecordButton.GetComponent<RectTransform>();

        Transform parent = centerRT.parent;
        Vector3 posL = leftRT.position;
        Vector3 posC = centerRT.position;
        Vector3 posR = rightRT.position;

        int oldLeft = (currentIndex - 1 + count) % count;
        int oldCenter = currentIndex;
        int oldRight = (currentIndex + 1) % count;

        StageData outgoingStage;
        StageData currentToOuterStage;
        StageData outerToCenterStage;
        StageData incomingStage;
        Vector3 outerPos;
        Vector3 oppositeOuterPos;
        Vector3 movingToCenterStart;

        if (direction == 1)
        {
            outgoingStage = allStages[oldLeft];
            currentToOuterStage = allStages[oldCenter];
            outerToCenterStage = allStages[oldRight];
            incomingStage = allStages[(currentIndex + 2) % count];
            outerPos = posL;
            oppositeOuterPos = posR;
            movingToCenterStart = posR;
        }
        else
        {
            outgoingStage = allStages[oldRight];
            currentToOuterStage = allStages[oldCenter];
            outerToCenterStage = allStages[oldLeft];
            incomingStage = allStages[(currentIndex - 2 + count) % count];
            outerPos = posR;
            oppositeOuterPos = posL;
            movingToCenterStart = posL;
        }

        Vector3 outgoingStart = (direction == 1) ? posL : posR;

        leftRecordButton.gameObject.SetActive(false);
        centerRecordButton.gameObject.SetActive(false);
        rightRecordButton.gameObject.SetActive(false);
        if (leftLockOverlay != null) leftLockOverlay.gameObject.SetActive(false);
        if (centerLockOverlay != null) centerLockOverlay.gameObject.SetActive(false);
        if (rightLockOverlay != null) rightLockOverlay.gameObject.SetActive(false);
        if (centerCornerImage != null) centerCornerImage.gameObject.SetActive(false);

        RectTransform outerRT = (direction == 1) ? leftRT : rightRT;
        RectTransform oppositeOuterRT = (direction == 1) ? rightRT : leftRT;

        Vector2 centerSize = centerRT.rect.size;
        Vector2 outerSize = outerRT.rect.size;
        Vector2 oppositeOuterSize = oppositeOuterRT.rect.size;

        // Outgoing/incoming stay at side state the whole animation; moving cards interpolate
        // BOTH sizeDelta and localScale so the visible size transitions smoothly between
        // center-state and side-state along the easing curve.
        GameObject outgoingCard = CreateRotationCard(outerSize, parent, outgoingStage, IsLocked(outgoingStage));
        GameObject incomingCard = CreateRotationCard(oppositeOuterSize, parent, incomingStage, IsLocked(incomingStage));
        GameObject movingToOuterCard = CreateRotationCard(centerSize, parent, currentToOuterStage, IsLocked(currentToOuterStage));
        GameObject movingToCenterCard = CreateRotationCard(oppositeOuterSize, parent, outerToCenterStage, IsLocked(outerToCenterStage));

        outgoingCard.transform.position = outgoingStart;
        incomingCard.transform.position = posC;
        movingToOuterCard.transform.position = posC;
        movingToCenterCard.transform.position = movingToCenterStart;

        SetCardScale(outgoingCard, sideScale);
        SetCardScale(incomingCard, sideScale);
        SetCardScale(movingToOuterCard, centerScale);
        SetCardScale(movingToCenterCard, sideScale);

        // Sibling order (later = on top): outgoing back, incoming back, then movingToOuter, movingToCenter front.
        // SetSiblingIndex(parent.childCount - 1) places at the END (top); call in desired back→front order.
        outgoingCard.transform.SetSiblingIndex(parent.childCount - 1);
        incomingCard.transform.SetSiblingIndex(parent.childCount - 1);
        movingToOuterCard.transform.SetSiblingIndex(parent.childCount - 1);
        movingToCenterCard.transform.SetSiblingIndex(parent.childCount - 1);

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float eased = rotationEasing.Evaluate(t);

            outgoingCard.transform.position = Vector3.LerpUnclamped(outgoingStart, posC, eased);
            SetCardAlpha(outgoingCard, 1f - eased);

            incomingCard.transform.position = Vector3.LerpUnclamped(posC, oppositeOuterPos, eased);
            SetCardAlpha(incomingCard, eased);

            movingToOuterCard.transform.position = Vector3.LerpUnclamped(posC, outerPos, eased);
            SetCardSize(movingToOuterCard, Vector2.LerpUnclamped(centerSize, outerSize, eased));
            SetCardScale(movingToOuterCard, Mathf.LerpUnclamped(centerScale, sideScale, eased));

            movingToCenterCard.transform.position = Vector3.LerpUnclamped(movingToCenterStart, posC, eased);
            SetCardSize(movingToCenterCard, Vector2.LerpUnclamped(oppositeOuterSize, centerSize, eased));
            SetCardScale(movingToCenterCard, Mathf.LerpUnclamped(sideScale, centerScale, eased));

            yield return null;
        }

        Destroy(outgoingCard);
        Destroy(incomingCard);
        Destroy(movingToOuterCard);
        Destroy(movingToCenterCard);

        currentIndex = (currentIndex + direction + count) % count;

        RefreshDisplay();

        leftRecordButton.gameObject.SetActive(true);
        centerRecordButton.gameObject.SetActive(true);
        rightRecordButton.gameObject.SetActive(true);
        if (leftLockOverlay != null) leftLockOverlay.gameObject.SetActive(true);
        if (centerLockOverlay != null) centerLockOverlay.gameObject.SetActive(true);
        if (rightLockOverlay != null) rightLockOverlay.gameObject.SetActive(true);
        if (centerCornerImage != null) centerCornerImage.gameObject.SetActive(true);

        isRotating = false;
    }

    GameObject CreateRotationCard(Vector2 size, Transform parent, StageData stage, bool locked)
    {
        GameObject go = new GameObject("RotationCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        // Point-center anchors so sizeDelta is the literal size, independent of parent rect.
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        if (TryGetMapping(stage.stageNumber, out StageArtMapping mapping) && mapping.recordSprite != null)
            img.sprite = mapping.recordSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        if (locked && lockedOverlaySprite != null)
        {
            GameObject lockGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform lockRT = (RectTransform)lockGO.transform;
            lockRT.SetParent(rt, false);
            // Stretch to fill the card so the overlay scales/moves with it.
            lockRT.anchorMin = Vector2.zero;
            lockRT.anchorMax = Vector2.one;
            lockRT.pivot = new Vector2(0.5f, 0.5f);
            lockRT.sizeDelta = Vector2.zero;
            lockRT.anchoredPosition = Vector2.zero;

            Image lockImg = lockGO.GetComponent<Image>();
            lockImg.sprite = lockedOverlaySprite;
            lockImg.preserveAspect = true;
            lockImg.raycastTarget = false;
        }

        return go;
    }

    bool IsLocked(StageData stage)
    {
        return stage != null && !StageProgressData.IsUnlocked(stage.stageNumber);
    }

    void SetCardSize(GameObject card, Vector2 size)
    {
        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = size;
    }

    void SetCardAlpha(GameObject card, float a)
    {
        Image img = card.GetComponent<Image>();
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }

    void SetCardScale(GameObject card, float scale)
    {
        card.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void HoverLeftRecord()
    {
        if (leftRecordImage != null)
            leftRecordImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
    }

    public void UnhoverLeftRecord()
    {
        if (leftRecordImage != null)
            leftRecordImage.color = Color.white;
    }

    public void HoverRightRecord()
    {
        if (rightRecordImage != null)
            rightRecordImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
    }

    public void UnhoverRightRecord()
    {
        if (rightRecordImage != null)
            rightRecordImage.color = Color.white;
    }

    void SetButtonGreyscale(Button btn, bool active)
    {
        if (btn == null) return;
        btn.interactable = active;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = active ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    void OnPlayClicked()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToStage(allStages[currentIndex]);
    }

    void RefreshCarousel()
    {
        if (centerRecordImage == null && leftRecordImage == null && rightRecordImage == null) return;
        if (allStages == null || allStages.Count == 0) return;

        int count = allStages.Count;
        int leftIndex = (currentIndex - 1 + count) % count;
        int rightIndex = (currentIndex + 1) % count;

        StageData leftStage = allStages[leftIndex];
        StageData centerStage = allStages[currentIndex];
        StageData rightStage = allStages[rightIndex];

        ApplyCarouselSlot(leftStage, leftRecordImage, leftLockOverlay, leftRecordButton, sideScale);
        ApplyCarouselSlot(centerStage, centerRecordImage, centerLockOverlay, centerRecordButton, centerScale);
        ApplyCarouselSlot(rightStage, rightRecordImage, rightLockOverlay, rightRecordButton, sideScale);

        if (centerCornerImage != null)
        {
            if (TryGetMapping(centerStage.stageNumber, out StageArtMapping mapping) && mapping.cornerSprite != null)
            {
                centerCornerImage.enabled = true;
                centerCornerImage.sprite = mapping.cornerSprite;
                centerCornerImage.preserveAspect = true;
            }
            else
            {
                centerCornerImage.enabled = false;
            }
        }

        if (centerRecordButton != null)
            centerRecordButton.transform.SetAsLastSibling();
    }

    void ApplyCarouselSlot(StageData stage, Image recordImage, Image lockOverlay, Button recordButton, float scale)
    {
        if (recordImage != null)
        {
            if (TryGetMapping(stage.stageNumber, out StageArtMapping mapping) && mapping.recordSprite != null)
                recordImage.sprite = mapping.recordSprite;
            recordImage.preserveAspect = true;
        }

        bool unlocked = StageProgressData.IsUnlocked(stage.stageNumber);
        if (lockOverlay != null)
        {
            lockOverlay.enabled = !unlocked;
            if (lockedOverlaySprite != null) lockOverlay.sprite = lockedOverlaySprite;
            lockOverlay.preserveAspect = true;
        }

        if (recordButton != null)
        {
            HoverScaler scaler = recordButton.GetComponent<HoverScaler>();
            if (scaler != null)
                scaler.SetBaseScale(scale);
            else
                recordButton.transform.localScale = Vector3.one * scale;
        }
    }

    void EnsureHoverScaler(Button btn)
    {
        if (btn == null) return;
        HoverScaler scaler = btn.GetComponent<HoverScaler>();
        if (scaler == null) scaler = btn.gameObject.AddComponent<HoverScaler>();
        scaler.hoverMultiplier = hoverScaleMultiplier;
    }

    bool TryGetMapping(int stageNumber, out StageArtMapping mapping)
    {
        for (int i = 0; i < stageArt.Count; i++)
        {
            if (stageArt[i].stageNumber == stageNumber)
            {
                mapping = stageArt[i];
                return true;
            }
        }

        mapping = default;
        return false;
    }

    bool HasCarousel()
    {
        return (centerRecordImage != null || leftRecordImage != null || rightRecordImage != null)
               && (centerRecordButton != null || leftRecordButton != null || rightRecordButton != null);
    }
}
