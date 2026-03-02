using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BasicReset : MonoBehaviour
{

    void Start()
    {

    }

    void Update()
    {

        if (Keyboard.current.rKey.isPressed)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Keyboard.current.escapeKey.isPressed)
        {
            Application.Quit();
        }


    }




}
