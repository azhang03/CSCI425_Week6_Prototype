using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public AudioManager audioManager;

    [Header("Stats")]
    public int maxHitPoints = 2;
    public int xpValue = 1;

    [Tooltip("How much this enemy reacts to the wave speed multiplier. " +
             "1 = full effect (default). 0.5 = half-reactive (a 2x wave becomes 1.5x). " +
             "0 = immune (always prefab base speed). Deviations from 1.0 are scaled; " +
             "a 1.0 wave is always neutral regardless of this value.")]
    [Range(0f, 2f)]
    public float speedMultiplierInfluence = 1f;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;

    private int currentHP;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;
    private bool deathRegistered;

    private MonoBehaviour[] attachedBehaviours;

    void Awake()
    {
        attachedBehaviours = GetComponents<MonoBehaviour>();
    }

    void Start()
    {
        currentHP = maxHitPoints;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        audioManager = FindAnyObjectByType<AudioManager>();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (attachedBehaviours == null)
            attachedBehaviours = GetComponents<MonoBehaviour>();

        // Attenuate the wave multiplier per-enemy. 1.0 stays 1.0 for any influence value;
        // only deviations from 1.0 are scaled.
        float effective = 1f + (multiplier - 1f) * speedMultiplierInfluence;

        foreach (MonoBehaviour behaviour in attachedBehaviours)
        {
            if (behaviour == null || behaviour == this)
                continue;

            if (behaviour is ISpeedMultiplierReceiver receiver)
            {
                receiver.SetSpeedMultiplier(effective);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Projectile proj = other.GetComponent<Projectile>();
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
        DamagePopup.Create(transform.position, damage);

        audioManager.PlayEnemyHurt();
        if (currentHP <= 0)
        {
            audioManager.PlayEnemyDie();

            RegisterDeath();

            if (XPManager.Instance != null)
                XPManager.Instance.AddXP(xpValue);

            if (A_ScoreManager.Instance != null)
                A_ScoreManager.Instance.AddKill();

            Destroy(gameObject);
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlash());
    }

    void RegisterDeath()
    {
        if (deathRegistered) return;
        deathRegistered = true;

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.RegisterEnemyDeath();
    }

    void OnDestroy()
    {
        RegisterDeath();
    }

    IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}

