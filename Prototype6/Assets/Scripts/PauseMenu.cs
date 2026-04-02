using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class A_PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    public static bool AugmentsEnabled { get; private set; } = false;

    private CanvasGroup canvasGroup;
    private bool built;
    private TextMeshProUGUI toggleLabel;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        IsPaused = false;
        Hide();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            A_AugmentUI augUI = FindAnyObjectByType<A_AugmentUI>();
            if (augUI != null && augUI.IsShowing)
                return;

            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (!built) BuildUI();
        Show();
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Hide();
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

    void BuildUI()
    {
        built = true;

        RectTransform selfRect = GetComponent<RectTransform>();
        selfRect.anchorMin = Vector2.zero;
        selfRect.anchorMax = Vector2.one;
        selfRect.offsetMin = Vector2.zero;
        selfRect.offsetMax = Vector2.zero;

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400f, 80f);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "PAUSED";
        titleTMP.fontSize = 48;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        CreateButton("ResumeBtn", "Resume", new Vector2(0.5f, 0.55f), Resume);
        CreateButton("ExitBtn", "Exit Game", new Vector2(0.5f, 0.43f), ExitGame);

        toggleLabel = CreateButton("ToggleAugments", GetToggleText(), new Vector2(0.5f, 0.28f), ToggleAugments);
    }

    string GetToggleText()
    {
        return "Augments: " + (AugmentsEnabled ? "ON" : "OFF");
    }

    void ToggleAugments()
    {
        AugmentsEnabled = !AugmentsEnabled;
        if (toggleLabel != null)
            toggleLabel.text = GetToggleText();
    }

    TextMeshProUGUI CreateButton(string name, string label, Vector2 anchorPos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(transform, false);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = anchorPos;
        btnRect.anchorMax = anchorPos;
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(220f, 50f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        btnOutline.effectDistance = new Vector2(2, 2);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(action);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI txtTMP = txtObj.AddComponent<TextMeshProUGUI>();
        txtTMP.text = label;
        txtTMP.fontSize = 22;
        txtTMP.alignment = TextAlignmentOptions.Center;
        txtTMP.color = Color.white;

        return txtTMP;
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
