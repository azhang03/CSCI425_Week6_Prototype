using TMPro;
using UnityEngine;

public class HighScoreUI : MonoBehaviour
{
    public TextMeshProUGUI highScoreText;
    public string prefix = "HighScore: ";

    void Start()
    {
        UpdateHighScore();
    }

    public void UpdateHighScore()
    {
        highScoreText.text = prefix + A_ScoreManager.Instance.highScore;
    }
}