using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class A_DeathScreen : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI highScoreText;
    private bool subscribed;

    void Start()
    {
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        if (!subscribed && A_PlayerHealth.Instance != null)
        {
            A_PlayerHealth.Instance.OnPlayerDied += OnPlayerDied;
            subscribed = true;
        }
    }

    void OnDestroy()
    {
        if (subscribed && A_PlayerHealth.Instance != null)
            A_PlayerHealth.Instance.OnPlayerDied -= OnPlayerDied;
    }

    void OnPlayerDied()
    {
        if (A_ScoreManager.Instance != null)
            A_ScoreManager.Instance.EndGame();

        if (scoreText != null && A_ScoreManager.Instance != null)
            scoreText.text = "Score: " + A_ScoreManager.Instance.KillCount;

        if (highScoreText != null && A_ScoreManager.Instance != null)
            highScoreText.text = "High Score: " + A_ScoreManager.Instance.highScore;

        Time.timeScale = 0f;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void BuildUI()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);

        // GAME OVER
        MakeLabel("Title", "GAME OVER", 60, FontStyles.Bold, Color.white, new Vector2(0f, 110f), new Vector2(500f, 80f));

        // Score
        scoreText = MakeLabel("Score", "Score: 0", 34, FontStyles.Normal, Color.white, new Vector2(0f, 30f), new Vector2(400f, 50f));

        // High Score
        highScoreText = MakeLabel("HighScore", "High Score: 0", 28, FontStyles.Normal, new Color(1f, 0.85f, 0.2f), new Vector2(0f, -30f), new Vector2(400f, 44f));

        // Try Again button
        GameObject btnObj = new GameObject("TryAgainButton");
        btnObj.transform.SetParent(transform, false);

        RectTransform brt = btnObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(240f, 64f);
        brt.anchoredPosition = new Vector2(0f, -110f);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        btn.colors = colors;
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(OnTryAgain);

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRt = btnTextObj.AddComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "Try Again";
        btnTmp.fontSize = 28;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.raycastTarget = false;
    }

    TextMeshProUGUI MakeLabel(string goName, string content, float size, FontStyles style, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(goName);
        obj.transform.SetParent(transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;
    }

    void OnTryAgain()
    {
        Time.timeScale = 1f;
        if (A_ScoreManager.Instance != null)
            A_ScoreManager.Instance.ResetGame();
    }
}
