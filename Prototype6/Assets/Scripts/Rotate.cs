


    using UnityEngine;
using UnityEngine.InputSystem;

public class WorldRotator2D : MonoBehaviour
{
    [Header("Assign the player transform here")]
    public Transform player;

    [Header("Rotation Settings")]
    public float rotationStep = 90f;

    private bool isRotating = false;

    void Update()
    {
        if (!isRotating)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                RotateWorld(rotationStep);
                Debug.Log("q pressed");

            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RotateWorld(rotationStep);
                Debug.Log("e pressed");


            }
        }
    }

    public void RotateWorld(float angle)
    {
        if (player == null) return;

        isRotating = true;

        // Store player's world position and rotation
        Vector3 playerWorldPos = player.position;
        Quaternion playerWorldRot = player.rotation;

        // Rotate the entire world around Z axis (2D axis)
        transform.Rotate(0f, 0f, angle);

        // Restore player's world position and rotation
        player.position = playerWorldPos;
        player.rotation = playerWorldRot;

        isRotating = false;
    }
}
