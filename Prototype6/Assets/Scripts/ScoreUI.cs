using UnityEngine;
using TMPro;

public class A_ScoreUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI scoreText;

    [Header("Display")]
    public string prefix = "Kills: ";

    private bool subscribed;

    void Start()
    {
        if (scoreText != null)
            scoreText.text = prefix + "0";
    }

    void Update()
    {
        if (!subscribed && A_ScoreManager.Instance != null)
        {
            A_ScoreManager.Instance.OnScoreChanged += UpdateDisplay;
            subscribed = true;
            UpdateDisplay(A_ScoreManager.Instance.KillCount);
        }
    }

    void OnDestroy()
    {
        if (subscribed && A_ScoreManager.Instance != null)
            A_ScoreManager.Instance.OnScoreChanged -= UpdateDisplay;
    }

    void UpdateDisplay(int kills)
    {
        if (scoreText == null) return;
        scoreText.text = prefix + kills;
    }
}
