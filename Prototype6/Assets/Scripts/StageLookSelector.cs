using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class StageLookSelector : MonoBehaviour
{
    [Header("References")]
    public Button openButton;

    // 0 = tilemap (default), 1..n = stageVariants[0..n-1]
    private int previewIndex = 0;

    public static bool IsSelecting { get; private set; }

    private GameObject      panel;
    private Image           previewImage;
    private TextMeshProUGUI indexLabel;

    void Start()
    {
        BuildPanel();
        panel.SetActive(false);

        if (SceneFlowManager.Instance != null)
            previewIndex = SceneFlowManager.Instance.SelectedVariant;

        if (openButton != null)
            openButton.onClick.AddListener(Open);
    }

    void Update()
    {
        if (!IsSelecting) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneFlowManager.Instance?.SetVariant(0);
            Close();
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SceneFlowManager.Instance?.SetVariant(previewIndex);
            Close();
            return;
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Navigate(1);
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)  Navigate(-1);
    }

    void Open()
    {
        if (SceneFlowManager.Instance != null)
            previewIndex = SceneFlowManager.Instance.SelectedVariant;
        IsSelecting = true;
        panel.SetActive(true);
        ShowPreview();
    }

    void Close()
    {
        IsSelecting = false;
        panel.SetActive(false);
    }

    void Navigate(int dir)
    {
        Sprite[] variants = SceneFlowManager.Instance?.stageVariants;
        int total    = (variants != null ? variants.Length : 0) + 1;
        previewIndex = (previewIndex + dir + total) % total;
        ShowPreview();
    }

    void ShowPreview()
    {
        Sprite[] variants = SceneFlowManager.Instance?.stageVariants;

        if (previewIndex == 0 || variants == null || variants.Length == 0)
        {
            previewImage.enabled = false;
            indexLabel.text      = "Tilemap (Default)";
        }
        else
        {
            previewImage.sprite  = variants[previewIndex - 1];
            previewImage.enabled = true;
            indexLabel.text      = $"{previewIndex} / {variants.Length}";
        }
    }

    void BuildPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        panel = new GameObject("StageSelectorPanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(400f, 380f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        MakeLabel(panel.transform, new Vector2(0f, 0.85f), new Vector2(1f, 1f),
            "STAGE LOOK", 20, true, Color.white);

        GameObject imgObj = new GameObject("Preview", typeof(RectTransform));
        imgObj.transform.SetParent(panel.transform, false);
        RectTransform irt = imgObj.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.1f, 0.25f);
        irt.anchorMax = new Vector2(0.9f, 0.85f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        previewImage = imgObj.AddComponent<Image>();
        previewImage.preserveAspect = true;

        GameObject labelObj = new GameObject("IndexLabel", typeof(RectTransform));
        labelObj.transform.SetParent(panel.transform, false);
        RectTransform lrt = labelObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.12f);
        lrt.anchorMax = new Vector2(1f, 0.25f);
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        indexLabel           = labelObj.AddComponent<TextMeshProUGUI>();
        indexLabel.alignment = TextAlignmentOptions.Center;
        indexLabel.fontSize  = 18;

        MakeLabel(panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.12f),
            "← → browse   Space: confirm   Esc: tilemap",
            13, false, new Color(0.7f, 0.7f, 0.7f));
    }

    TextMeshProUGUI MakeLabel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                              string text, int size, bool bold, Color color)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = size;
        tmp.color     = color;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
