using UnityEngine;
using TMPro;

public class A_HealthUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI heartsText;

    [Header("Display")]
    public Color heartColor = new Color(0.9f, 0.15f, 0.2f, 1f);
    public Color emptyHeartColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    public int heartFontSize = 28;

    private bool subscribed;

    void Update()
    {
        if (!subscribed && A_PlayerHealth.Instance != null)
        {
            A_PlayerHealth.Instance.OnHealthChanged += UpdateHearts;
            A_PlayerHealth.Instance.OnPlayerDied += OnDied;
            subscribed = true;
            UpdateHearts(A_PlayerHealth.Instance.CurrentHearts, A_PlayerHealth.Instance.maxHearts);
        }
    }

    void OnDestroy()
    {
        if (subscribed && A_PlayerHealth.Instance != null)
        {
            A_PlayerHealth.Instance.OnHealthChanged -= UpdateHearts;
            A_PlayerHealth.Instance.OnPlayerDied -= OnDied;
        }
    }

    void UpdateHearts(int current, int max)
    {
        if (heartsText == null) return;

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
