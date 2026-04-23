using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour
{
    public float distanceFromEnemy = 1.5f;

    [Header("Health")]
    [Tooltip("Hit points the shield can absorb before breaking. Projectile damage is subtracted per hit.")]
    public int maxHitPoints = 2;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;

    private int currentHP;
    private Transform target;
    private Transform enemy;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        currentHP = maxHitPoints;
    }

    private void Start()
    {
        // Parent is the enemy
        enemy = transform.parent;

        // Auto-find player if not assigned

        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // Called by Projectile.OnTriggerEnter2D. Returns true if the shield broke on this hit.
    public bool TakeHit(int amount)
    {
        int dmg = Mathf.Max(1, amount);
        DamagePopup.Create(transform.position, dmg);

        currentHP -= dmg;
        if (currentHP <= 0)
        {
            Destroy(gameObject);
            return true;
        }

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
        return false;
    }

    IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
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
