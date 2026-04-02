using UnityEngine;
using TMPro;

public class A_DamagePopup : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private float lifetime = 0.8f;
    private float elapsed;
    private float floatSpeed = 1.5f;

    public static void Create(Vector3 position, int damage)
    {
        GameObject go = new GameObject("DamagePopup");
        go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.4f, 0f);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Entities";
        canvas.sortingOrder = 200;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 100f);
        go.transform.localScale = Vector3.one * 0.01f;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "-" + damage;
        tmp.fontSize = 48;
        tmp.color = new Color(1f, 0.2f, 0.2f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        go.AddComponent<A_DamagePopup>();
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
