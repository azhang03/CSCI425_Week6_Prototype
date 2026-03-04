using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using static A_EnemySpawner;

public class S_PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 direction;

    public Tilemap stageTilemap;
    public float radius;
    public Transform centerPoint;   
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (stageTilemap != null)
        {
            BoundsInt cellBounds = stageTilemap.cellBounds;
            Vector3 min = stageTilemap.CellToWorld(cellBounds.min);
            Vector3 max = stageTilemap.CellToWorld(cellBounds.max);
            float width = (max.x - min.x)*0.5f;
            float height = (max.y - min.y)*0.5f;
            radius = Mathf.Max(width, height) + 0.5f;

          
        }
    }

    void Update()
    {
        direction = Vector2.zero;
        if (Keyboard.current.wKey.isPressed)
        {
            direction.y += 1;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            direction.y -= 1;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            direction.x += 1;

        }
        if (Keyboard.current.aKey.isPressed)
        {
            direction.x -= 1;
        }


        direction = direction.normalized;

       // rb.linearVelocity = moveSpeed * direction;
    }

    void FixedUpdate()
    {
        Vector2 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;

        // Check distance from center
        Vector2 directionFromCenter = newPosition - (Vector2)centerPoint.position;

        if (directionFromCenter.magnitude > radius)
        {
            Debug.Log("Border");
            directionFromCenter = directionFromCenter.normalized * radius;
            newPosition = (Vector2)centerPoint.position + directionFromCenter;
        }

        rb.MovePosition(newPosition);
    }
}