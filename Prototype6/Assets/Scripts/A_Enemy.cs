using UnityEngine;
using System.Collections;

public class A_Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHitPoints = 2;
    public float moveSpeed = 2f;
    public int xpValue = 1;
    public int scoreValue = 1;
    public int spawnCycle = 0;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;

    private int currentHP;
    private Transform target;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        currentHP = maxHitPoints;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;
    }

    void Update()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        A_Projectile proj = other.GetComponent<A_Projectile>();
        if (proj != null)
        {
            int dmg = proj.damage;
            Destroy(other.gameObject);
            TakeDamage(dmg);
            return;
        }

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null)
        {
            Destroy(other.gameObject);
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        A_DamagePopup.Create(transform.position, damage);

        if (currentHP <= 0)
        {
            if (A_XPManager.Instance != null)
                A_XPManager.Instance.AddXP(xpValue);
            if (A_ScoreManager.Instance != null)
                A_ScoreManager.Instance.AddScore(scoreValue);
            if (A_EnemySpawner.Instance != null)
                A_EnemySpawner.Instance.RegisterKill(spawnCycle);
            Destroy(gameObject);
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
