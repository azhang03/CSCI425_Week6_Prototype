using UnityEngine;
using UnityEngine.InputSystem;

public class A_WorldRotator2D : MonoBehaviour
{
    [Header("Assign the player transform here")]
    public Transform player;

    [Header("Rotation Settings")]
    public float rotationSpeed = 120f;

    void Update()
    {
        float direction = 0f;

        if (Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.qKey.isPressed)
        {
            direction += 1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed ||
            Keyboard.current.eKey.isPressed)
        {
            direction -= 1f;
        }

        if (direction != 0f)
        {
            RotateWorld(direction * rotationSpeed * Time.deltaTime);
        }
    }

    public void RotateWorld(float angle)
    {
        if (player == null) return;

        Vector3 playerWorldPos = player.position;
        Quaternion playerWorldRot = player.rotation;

        transform.Rotate(0f, 0f, angle);

        player.position = playerWorldPos;
        player.rotation = playerWorldRot;
    }
}
