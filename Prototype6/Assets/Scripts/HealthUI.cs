using UnityEngine;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI heartsText;

    [Header("Display")]
    public Color heartColor = new Color(0.9f, 0.15f, 0.2f, 1f);
    public Color emptyHeartColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    public int heartFontSize = 28;

    [Header("Damage FX")]
    [Tooltip("How many heart particles spawn when a heart is lost.")]
    public int heartBurstCount = 20;

    private bool subscribed;
    private int  lastKnownHealth = -1;

    void Update()
    {
        if (!subscribed && PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged += UpdateHearts;
            PlayerHealth.Instance.OnPlayerDied += OnDied;
            subscribed = true;
            UpdateHearts(PlayerHealth.Instance.CurrentHearts, PlayerHealth.Instance.maxHearts);
        }
    }

    void OnDestroy()
    {
        if (subscribed && PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged -= UpdateHearts;
            PlayerHealth.Instance.OnPlayerDied -= OnDied;
        }
    }

    void UpdateHearts(int current, int max)
    {
        if (heartsText == null) return;

        int prev = lastKnownHealth;
        bool tookDamage = prev >= 0 && current < prev;
        int firstLostIndex = current;
        int lastLostIndex  = prev - 1;

        string filled = ColorTag(heartColor);
        string empty = ColorTag(emptyHeartColor);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append($"<size={heartFontSize}>");

        for (int i = 0; i < max; i++)
        {
            if (i < current)
                sb.Append($"<color={filled}>\u2665</color> ");
            else
                sb.Append($"<color={empty}>\u2665</color> ");
        }

        sb.Append("</size>");
        heartsText.text = sb.ToString().TrimEnd();

        if (tookDamage)
        {
            // ForceMeshUpdate so characterInfo reflects the freshly-set string.
            heartsText.ForceMeshUpdate();
            for (int i = firstLostIndex; i <= lastLostIndex; i++)
                ExplodeHeart(i);
        }

        lastKnownHealth = current;
    }

    void ExplodeHeart(int heartIndex)
    {
        if (heartsText == null) return;

        Canvas canvas = heartsText.canvas;
        if (canvas == null) return;

        var textInfo = heartsText.textInfo;
        if (textInfo == null || textInfo.characterInfo == null)
            return;

        // Visible char layout from UpdateHearts: heart, space, heart, space...
        // so heart N lives at visible-character index N * 2.
        int charIdx = heartIndex * 2;
        if (charIdx < 0 || charIdx >= textInfo.characterCount) return;

        var info = textInfo.characterInfo[charIdx];
        Vector3 localCenter = (info.bottomLeft + info.topRight) * 0.5f;
        Vector3 worldCenter = heartsText.transform.TransformPoint(localCenter);

        HeartParticles.Spawn(canvas, worldCenter, heartColor, heartFontSize, heartBurstCount);
    }

    void OnDied()
    {
        if (heartsText == null) return;
        string empty = ColorTag(emptyHeartColor);
        heartsText.text = $"<size={heartFontSize}><color={empty}>\u2665 \u2665 \u2665</color></size>";
    }

    static string ColorTag(Color c)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
    }
}
