using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class A_GameManager : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
