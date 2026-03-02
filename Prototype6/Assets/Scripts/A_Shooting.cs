using UnityEngine;

public class A_Shooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public float fireInterval = 1.5f;

    public float spawnOffset = 0.5f;

    private float fireTimer;

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireInterval;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector2 direction = GetRandomDirection();

        Vector3 bulletPosition = new Vector3(
            transform.position.x + spawnOffset * direction.x,
            transform.position.y + spawnOffset * direction.y,
            transform.position.z
        );

        GameObject projectile = Instantiate(
            projectilePrefab,
            bulletPosition,
            Quaternion.identity
        );

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
    }

    Vector2 GetRandomDirection()
    {
        int random = Random.Range(0, 4);

        switch (random)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            default: return Vector2.right;
        }
    }
}
