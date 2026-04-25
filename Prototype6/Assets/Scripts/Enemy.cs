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

    [Header("Hit Reaction")]
    public float knockbackDistance = 0.4f;
    public float knockbackDuration = 0.08f;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.25f;

    [Header("Death Camera Shake")]
    public float deathShakeDuration  = 0.18f;
    public float deathShakeMagnitude = 0.35f;

    [Header("Breathing")]
    [Tooltip("Fractional scale amplitude of the breathing sine wave. 0.1 = ±10%.")]
    public float breathAmount = 0.1f;
    [Tooltip("Angular speed of the breathing sine wave (radians/sec). 10π ≈ 31.416 = 0.2s per cycle.")]
    public float breathSpeed = 10f * Mathf.PI;
    [Tooltip("When damaged, sprite dims to this fraction of original brightness on each breath peak.")]
    [Range(0f, 1f)]
    public float damagedDimFactor = 0.6f;

    private int currentHP;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;
    private Coroutine reactionRoutine;
    private bool deathRegistered;

    private Vector3 baseScale;
    private float breathPhaseOffset;

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

        baseScale = transform.localScale;
        // Random phase so a wave of enemies doesn't breathe in lockstep.
        breathPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float sineWave = Mathf.Sin(Time.time * breathSpeed + breathPhaseOffset);

        float s = 1f + sineWave * breathAmount;
        transform.localScale = baseScale * s;

        // Damaged enemies pulse darker in sync with the breath. Skip while a hit-flash is
        // playing so we don't fight that coroutine for control of the sprite color.
        if (spriteRenderer != null && flashRoutine == null)
        {
            if (currentHP < maxHitPoints && currentHP > 0)
            {
                // Map sine [-1, +1] -> brightness [1.0, damagedDimFactor]: darkest at peak inhale.
                float halfRange = (1f - damagedDimFactor) * 0.5f;
                float midpoint  = 1f - halfRange;
                float brightness = midpoint - halfRange * sineWave;
                spriteRenderer.color = new Color(
                    originalColor.r * brightness,
                    originalColor.g * brightness,
                    originalColor.b * brightness,
                    originalColor.a);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
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

            if (spriteRenderer != null)
                DeathParticles.Spawn(spriteRenderer);

            CameraShake.Shake(deathShakeDuration, deathShakeMagnitude);

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

        if (reactionRoutine != null)
            StopCoroutine(reactionRoutine);

        reactionRoutine = StartCoroutine(HitReaction());
    }

    IEnumerator HitReaction()
    {
        // Direction away from the player (the stage center).
        Vector2 awayDir = PlayerHealth.Instance != null
            ? ((Vector2)transform.position - (Vector2)PlayerHealth.Instance.transform.position).normalized
            : Vector2.up;

        // Phase 1: knockback — permanent backward displacement, applied incrementally so it
        // composes additively with whatever the movement script is doing that frame.
        float elapsed = 0f;
        float lastApplied = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = elapsed / knockbackDuration;
            float target = Mathf.SmoothStep(0f, knockbackDistance, t);
            float delta = target - lastApplied;
            transform.position += (Vector3)(awayDir * delta);
            lastApplied = target;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: shake — visual jitter with net-zero residual displacement. Each frame we
        // subtract the previous shake offset and add a new one; amplitude tapers to 0. Using
        // a normalized random direction (not insideUnitCircle) gives a consistent full-strength
        // radius, and flipping sign each frame produces a readable back-and-forth wiggle.
        elapsed = 0f;
        Vector2 prevShake = Vector2.zero;
        int sign = 1;
        while (elapsed < shakeDuration)
        {
            float remaining = 1f - (elapsed / shakeDuration);
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            Vector2 newShake = dir * (shakeMagnitude * remaining) * sign;
            transform.position += (Vector3)(newShake - prevShake);
            prevShake = newShake;
            sign = -sign;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position -= (Vector3)prevShake;
        reactionRoutine = null;
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

