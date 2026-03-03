using UnityEngine;

public class S_SpiralMove : MonoBehaviour
{
    [Header("Movement Settings")]
    private Transform target;

    public float moveSpeed = 3f;          // Forward speed toward player
    public float spiralStrength = 15f;     // How wide the spiral is
    public float spiralTightness = 1.5f;  // How fast spiral tightens near player

    private float spiralDirection;


    void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;


        spiralDirection = Random.value < 0.5f ? -1f : 1f;
    }

    void Update()
    {
        if (target == null) return;

        Vector2 toPlayer = target.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance < 0.1f) return;

        Vector2 directionToPlayer = toPlayer.normalized;

        // Perpendicular direction (creates circular motion)
        Vector2 perpendicular = spiralDirection * new Vector2(-directionToPlayer.y, directionToPlayer.x);
        // Spiral gets tighter as it gets closer
        float spiralAmount = spiralStrength / Mathf.Max(distance * spiralTightness, 0.5f);

        Vector2 finalDirection = directionToPlayer + perpendicular * spiralAmount;

        transform.position += (Vector3)(finalDirection.normalized * moveSpeed * Time.deltaTime);
    }
}