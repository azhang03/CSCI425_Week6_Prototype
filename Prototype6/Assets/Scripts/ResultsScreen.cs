using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsScreen : MonoBehaviour
{
    // Assign a star-shaped sprite in the Inspector for proper star look.
    // If left empty, falls back to Unity's built-in circle knob sprite.
    [SerializeField] public Sprite starSprite;

    private CanvasGroup canvasGroup;
    private bool subscribed;

    // Win UI refs
    private GameObject winRoot;
    private Image[] starImages;
    private TextMeshProUGUI winKillText;
    private GameObject nextStageBtn;
    private RectTransform winLobbyBtnRect;

    // Loss UI refs
    private GameObject lossRoot;
    private TextMeshProUGUI lossKillText;

    void Start()
    {
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        if (!subscribed && StageManager.Instance != null)
        {
            StageManager.Instance.OnStageEnded += OnStageEnded;
            subscribed = true;
        }
    }

    void OnDestroy()
    {
        if (subscribed && StageManager.Instance != null)
            StageManager.Instance.OnStageEnded -= OnStageEnded;
    }

    void OnStageEnded(StageManager.StageResult result, int stars)
    {
        int stageNumber = (StageManager.Instance.currentStage != null)
            ? StageManager.Instance.currentStage.stageNumber
            : 0;

        int kills = (A_ScoreManager.Instance != null) ? A_ScoreManager.Instance.KillCount : 0;

        if (result == StageManager.StageResult.Win)
        {
            if (stageNumber > 0)
                StageProgressData.SaveResult(stageNumber, stars);

            StageData stage = SceneFlowManager.Instance != null ? SceneFlowManager.Instance.SelectedStage : null;
            if (stage != null)
                CurrencyManager.AddCoins(stage.coinReward);

            ShowWin(stars, kills);
        }
        else
        {
            ShowLoss(kills);
        }

        SetVisible(true);
    }

    void ShowWin(int stars, int kills)
    {
        lossRoot.SetActive(false);
        winRoot.SetActive(true);

        Color earnedColor = new Color(1f, 0.84f, 0f);
        Color emptyColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);
        for (int i = 0; i < starImages.Length; i++)
            starImages[i].color = (i < stars) ? earnedColor : emptyColor;

        winKillText.text = "Kills: " + kills;

        StageData next = (SceneFlowManager.Instance != null) ? SceneFlowManager.Instance.GetNextStage() : null;
        bool hasNext = next != null;
        nextStageBtn.SetActive(hasNext);

        // Center Lobby if no Next Stage, offset left otherwise
        winLobbyBtnRect.anchoredPosition = hasNext ? new Vector2(100f, -110f) : new Vector2(0f, -110f);
    }

    void ShowLoss(int kills)
    {
        winRoot.SetActive(false);
        lossRoot.SetActive(true);

        lossKillText.text = "Kills: " + kills;
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
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        winRoot = BuildWinUI();
        lossRoot = BuildLossUI();
    }

    GameObject BuildWinUI()
    {
        GameObject root = new GameObject("WinUI", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        StretchFill(root.GetComponent<RectTransform>());

        MakeLabel(root, "Title", "STAGE CLEAR!", 56, FontStyles.Bold, Color.white, new Vector2(0f, 140f), new Vector2(500f, 80f));

        // Star images — assign starSprite in Inspector for star shape
        Sprite sprite = starSprite;

        starImages = new Image[3];
        float[] starX = { -65f, 0f, 65f };
        for (int i = 0; i < 3; i++)
        {
            GameObject starObj = new GameObject("Star" + i, typeof(RectTransform));
            starObj.transform.SetParent(root.transform, false);
            RectTransform srt = starObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(52f, 52f);
            srt.anchoredPosition = new Vector2(starX[i], 55f);
            Image img = starObj.AddComponent<Image>();
            img.sprite = sprite;
            starImages[i] = img;
        }

        winKillText = MakeLabel(root, "Kills", "Kills: 0", 30, FontStyles.Normal, Color.white, new Vector2(0f, -10f), new Vector2(300f, 50f));

        nextStageBtn = MakeButton(root, "NextStageBtn", "Next Stage", new Vector2(-100f, -110f), OnNextStage);
        GameObject lobbyBtn = MakeButton(root, "LobbyBtnWin", "Lobby", new Vector2(100f, -110f), OnLobby);
        winLobbyBtnRect = lobbyBtn.GetComponent<RectTransform>();

        return root;
    }

    GameObject BuildLossUI()
    {
        GameObject root = new GameObject("LossUI", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        StretchFill(root.GetComponent<RectTransform>());

        MakeLabel(root, "Title", "GAME OVER", 56, FontStyles.Bold, new Color(1f, 0.3f, 0.3f), new Vector2(0f, 110f), new Vector2(500f, 80f));

        lossKillText = MakeLabel(root, "Kills", "Kills: 0", 30, FontStyles.Normal, Color.white, new Vector2(0f, 20f), new Vector2(300f, 50f));

        MakeButton(root, "RetryBtn", "Retry", new Vector2(-100f, -80f), OnRetry);
        MakeButton(root, "LobbyBtnLoss", "Lobby", new Vector2(100f, -80f), OnLobby);

        return root;
    }

    void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI MakeLabel(GameObject parent, string goName, string content, float size, FontStyles style, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(goName, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);

        RectTransform labelRt = obj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = sizeDelta;
        labelRt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;
    }

    GameObject MakeButton(GameObject parent, string goName, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(goName, typeof(RectTransform));
        btnObj.transform.SetParent(parent.transform, false);

        RectTransform brt = btnObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(200f, 56f);
        brt.anchoredPosition = anchoredPos;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        btnOutline.effectDistance = new Vector2(2, 2);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(action);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI txtTMP = txtObj.AddComponent<TextMeshProUGUI>();
        txtTMP.text = label;
        txtTMP.fontSize = 24;
        txtTMP.fontStyle = FontStyles.Bold;
        txtTMP.color = Color.white;
        txtTMP.alignment = TextAlignmentOptions.Center;
        txtTMP.raycastTarget = false;

        return btnObj;
    }

    void OnNextStage()
    {
        if (SceneFlowManager.Instance == null) return;
        StageData next = SceneFlowManager.Instance.GetNextStage();
        if (next != null)
            SceneFlowManager.Instance.GoToStage(next);
    }

    void OnRetry()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.RetryCurrentStage();
        else
        {
            Time.timeScale = 1f;
            if (A_ScoreManager.Instance != null)
                A_ScoreManager.Instance.ResetGame();
        }
    }

    void OnLobby()
    {
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.GoToLobby();
    }
}
