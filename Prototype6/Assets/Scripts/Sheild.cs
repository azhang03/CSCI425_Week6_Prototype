using UnityEngine;

public class Shield : MonoBehaviour
{
    public float distanceFromEnemy = 1.5f;

    private Transform target;
    private Transform enemy;

    private void Start()
    {
        // Parent is the enemy
        enemy = transform.parent;

        // Auto-find player if not assigned

        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
        if (target == null || enemy == null) return;

        Vector2 direction = (target.position - enemy.position).normalized;

        transform.position = (Vector2)enemy.position + direction * distanceFromEnemy;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}