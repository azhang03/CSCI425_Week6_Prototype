using UnityEngine;
using System.Collections;

public class S_Enemy : MonoBehaviour
{
    public S_AudioManager audioManager;

    [Header("Stats")]
    public int maxHitPoints = 2;
    public int xpValue = 1;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;

    private int currentHP;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        currentHP = maxHitPoints;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        audioManager = FindAnyObjectByType<S_AudioManager>();

    }

    void Update()
    {

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

        audioManager.PlayEnemyHurt();
        if (currentHP <= 0)
        {
            audioManager.PlayEnemyDie();

            if (A_XPManager.Instance != null)
                A_XPManager.Instance.AddXP(xpValue);
            if (A_ScoreManager.Instance != null)
                A_ScoreManager.Instance.AddKill();
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
