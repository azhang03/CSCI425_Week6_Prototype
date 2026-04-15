using UnityEngine;

public class Snowball : MonoBehaviour
{
    [Header("Growth")]
    [Tooltip("Scale increase per world unit traveled.")]
    public float growthPerUnit = 0.15f;

    [Header("On-Hit Speed Boost")]
    public float speedBoostOnHit = 1.5f;

    [Header("Auto-Destroy")]
    public float maxDistance = 25f;

    private const string WEAPON_NAME = "Snowball";

    private Vector2 spawnPosition;
    private Rigidbody2D rb;
    private Vector2 direction;
    private float currentSpeed;
    private float baseDamage = 1f;

    void Start()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        // Reset scale — Shooting.cs may have applied bonusRadius as a scale multiplier,
        // but Snowball manages its own scale entirely from spawn position.
        transform.localScale = Vector3.one;

        // Capture direction and speed that Shooting.cs set via rb.linearVelocity
        direction = rb.linearVelocity.normalized;
        currentSpeed = rb.linearVelocity.magnitude;

        // Read base damage and bonus growth rate from WeaponManager
        if (WeaponManager.Instance != null)
        {
            foreach (var entry in WeaponManager.Instance.GetActiveWeapons())
            {
                if (entry.data.weaponName == WEAPON_NAME)
                {
                    baseDamage = entry.data.damage + entry.bonusDamage;
                    growthPerUnit += entry.bonusRadius; // "Heavy Snow" augment adds here
                    break;
                }
            }
        }

        // Block re-firing until this snowball leaves the scene (same as Moat pattern)
        WeaponManager.Instance?.SetCooldown(WEAPON_NAME, true);
    }

    void Update()
    {
        float distance = Vector2.Distance((Vector2)transform.position, spawnPosition);

        // Scale grows linearly with distance traveled
        float scale = 1f + distance * growthPerUnit;
        transform.localScale = Vector3.one * scale;

        // Auto-destroy when off screen
        if (distance >= maxDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Damage scales with current size, minimum 1
                int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * transform.localScale.x));
                enemy.TakeDamage(damage);

                // Speed up instead of disappearing
                currentSpeed += speedBoostOnHit;
                rb.linearVelocity = direction * currentSpeed;
            }
        }
        else if (other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Clear cooldown so Shooting.cs can fire the next snowball
        WeaponManager.Instance?.SetCooldown(WEAPON_NAME, false);
    }
}
