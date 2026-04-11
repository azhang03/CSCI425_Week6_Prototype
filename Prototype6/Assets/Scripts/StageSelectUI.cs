using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class StageSelectUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI titleText;

    [Header("Stars (3 Image components — drag star1, star2, star3)")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Buttons")]
    public Button playButton;
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

        if (debugMode)
        {
            currentIndex = allStages.Count - 1;

        }
        else
        {
            currentIndex = 0;

        }


        playButton.onClick.AddListener(OnPlayClicked);
        leftArrowButton.onClick.AddListener(NavigateLeft);
        rightArrowButton.onClick.AddListener(NavigateRight);

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
            RefreshDisplay();
        }
    }

    void RefreshDisplay()
    {
        if (allStages == null || allStages.Count == 0) return;

        StageData stage = allStages[currentIndex];

        // Title
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
        leftArrowButton.gameObject.SetActive(multipleStages);
        rightArrowButton.gameObject.SetActive(multipleStages);

        if (multipleStages)
        {
            // Check if navigating right leads to an unlocked stage
            int nextIndex = (currentIndex + 1) % allStages.Count;
            bool rightUnlocked = StageProgressData.IsUnlocked(allStages[nextIndex].stageNumber);
            SetButtonGreyscale(rightArrowButton, rightUnlocked);

            // Check if navigating left leads to an unlocked stage
            int prevIndex = (currentIndex - 1 + allStages.Count) % allStages.Count;
            bool leftUnlocked = StageProgressData.IsUnlocked(allStages[prevIndex].stageNumber);
            SetButtonGreyscale(leftArrowButton, leftUnlocked);
        }

        // Lock icon — show next to right arrow if next sequential stage is locked
        if (lockIcon != null)
        {
            int nextStageNumber = stage.stageNumber + 1;
            bool nextExists = false;
            foreach (var s in allStages)
                if (s.stageNumber == nextStageNumber) { nextExists = true; break; }

            lockIcon.SetActive(nextExists && !StageProgressData.IsUnlocked(nextStageNumber));
        }

        // Play button — only for unlocked stages
        playButton.gameObject.SetActive(StageProgressData.IsUnlocked(stage.stageNumber));
    }

    void NavigateRight()
    {
        int nextIndex = (currentIndex + 1) % allStages.Count;
        if (!StageProgressData.IsUnlocked(allStages[nextIndex].stageNumber)) return;
        currentIndex = nextIndex;
        RefreshDisplay();
    }

    void NavigateLeft()
    {
        int prevIndex = (currentIndex - 1 + allStages.Count) % allStages.Count;
        if (!StageProgressData.IsUnlocked(allStages[prevIndex].stageNumber)) return;
        currentIndex = prevIndex;
        RefreshDisplay();
    }

    void SetButtonGreyscale(Button btn, bool active)
    {
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
}
