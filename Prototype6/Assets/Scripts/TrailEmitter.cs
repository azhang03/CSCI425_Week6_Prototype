using UnityEngine;

// Drop-in trail emitter. Attach to any actor with a SpriteRenderer (enemy, bullet,
// projectile, etc.) and it will periodically drop sprite-clone particles at the actor's
// world position. Each particle smoothly shrinks to zero scale and fades out, then
// self-destructs. Particles spawn at world root so they don't follow the actor.
public class TrailEmitter : MonoBehaviour
{
    [Header("Emission")]
    [Tooltip("Seconds between trail particle spawns.")]
    public float emitInterval = 0.05f;

    [Header("Particle Size (fraction of source actor's scale)")]
    [Range(0f, 1f)] public float minSizeFrac = 0.1f;
    [Range(0f, 1f)] public float maxSizeFrac = 0.65f;

    [Header("Particle Lifetime (seconds, randomized per particle)")]
    public float minLifetime = 0.5f;
    public float maxLifetime = 2f;

    private SpriteRenderer sourceSr;
    private float emitTimer;

    void Awake()
    {
        sourceSr = GetComponent<SpriteRenderer>();
        if (sourceSr == null)
            sourceSr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (sourceSr == null || sourceSr.sprite == null) return;

        emitTimer -= Time.deltaTime;
        if (emitTimer > 0f) return;
        emitTimer = emitInterval;

        SpawnParticle();
    }

    void SpawnParticle()
    {
        var go = new GameObject("TrailParticle");
        go.transform.position = transform.position;
        go.transform.rotation = sourceSr.transform.rotation;

        // lossyScale.x captures any parent scaling (e.g. Snowball's growth multiplier).
        float scaleBase = Mathf.Abs(sourceSr.transform.lossyScale.x);
        if (scaleBase <= 0.0001f) scaleBase = 1f;

        float frac = Random.Range(minSizeFrac, maxSizeFrac);
        Vector3 startScale = Vector3.one * (scaleBase * frac);
        go.transform.localScale = startScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sourceSr.sprite;
        sr.color            = sourceSr.color;
        sr.sortingLayerName = sourceSr.sortingLayerName;
        // Render behind the source so the actor stays on top of its own trail.
        sr.sortingOrder     = sourceSr.sortingOrder - 1;

        var p = go.AddComponent<TrailParticle>();
        p.lifetime   = Random.Range(minLifetime, maxLifetime);
        p.startScale = startScale;
    }
}

// Per-particle ticker. Smoothly shrinks scale to zero and fades alpha over its
// randomized lifetime, then destroys itself.
public class TrailParticle : MonoBehaviour
{
    public float   lifetime = 1f;
    public Vector3 startScale = Vector3.one;

    SpriteRenderer sr;
    Color          baseColor;
    float          age;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01(age / lifetime);
        // SmoothStep eases the shrink — slow start, faster mid, gentle finish at zero.
        float scaleMul = 1f - Mathf.SmoothStep(0f, 1f, t);
        transform.localScale = startScale * scaleMul;

        if (sr != null)
        {
            // Alpha fade tracks the same curve so particles don't pop at the end.
            var c = baseColor;
            c.a = baseColor.a * scaleMul;
            sr.color = c;
        }
    }
}
