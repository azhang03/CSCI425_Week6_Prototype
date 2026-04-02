using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public A_ScoreManager scoreManager;
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            scoreManager.ResetGame();
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (XPManager.Instance != null)
                XPManager.Instance.AddXP(1);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            if (PlayerHealth.Instance != null)
                PlayerHealth.Instance.TakeDamage(1);
        }

        //if (Keyboard.current.escapeKey.isPressed)
        //{
        //    Application.Quit();
        //}
    }
}
