using UnityEngine;

// Hand-rolled particle burst. Each particle is its own GameObject with a
// SpriteRenderer + DeathParticle ticker — no Unity ParticleSystem involved.
// Used for enemy death and shield shatter effects.
public static class DeathParticles
{
    const float MinSpeed       = 2.5f;
    const float MaxSpeed       = 6.0f;
    const float MinLifetime    = 0.85f;
    const float MaxLifetime    = 1.25f;
    const float MinAngularVel  = -540f;
    const float MaxAngularVel  =  540f;
    const float Gravity        = 9.81f;

    public static void Spawn(
        SpriteRenderer source,
        int count          = 30,
        float minSizeFrac  = 0.05f,
        float maxSizeFrac  = 0.35f,
        bool forceSquare   = false)
    {
        if (source == null || source.sprite == null) return;

        Sprite spriteToUse = source.sprite;

        if (forceSquare)
        {
            // Build a one-off sprite that maps to the entire source texture so each
            // particle quad reads as a plain textured square, regardless of the
            // shape carved out of the original sprite's rect.
            Texture2D tex = spriteToUse.texture;
            if (tex != null)
            {
                spriteToUse = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(1f, spriteToUse.pixelsPerUnit));
            }
        }

        Vector3 spawnPos  = source.transform.position;
        float   scaleBase = Mathf.Abs(source.transform.lossyScale.x);
        if (scaleBase <= 0.0001f) scaleBase = 1f;

        string layerName = source.sortingLayerName;
        int    order     = source.sortingOrder + 1;
        Color  tint      = source.color;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("DeathParticle");
            go.transform.position = spawnPos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float frac = Random.Range(minSizeFrac, maxSizeFrac);
            go.transform.localScale = Vector3.one * (scaleBase * frac);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = spriteToUse;
            sr.color            = tint;
            sr.sortingLayerName = layerName;
            sr.sortingOrder     = order;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(MinSpeed, MaxSpeed);

            var p = go.AddComponent<DeathParticle>();
            p.velocity        = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            p.angularVelocity = Random.Range(MinAngularVel, MaxAngularVel);
            p.lifetime        = Random.Range(MinLifetime, MaxLifetime);
            p.gravity         = Gravity;
        }
    }
}

// Per-particle ticker. Integrates velocity under gravity, spins, fades alpha,
// and self-destructs at end of life.
public class DeathParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float   angularVelocity;
    public float   lifetime  = 1f;
    public float   gravity   = 9.81f;
    public float   fadeStart = 0.65f;

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
        float dt = Time.deltaTime;

        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, angularVelocity * dt);

        age += dt;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (sr == null) return;

        float t = age / lifetime;
        float alpha = t < fadeStart
            ? 1f
            : 1f - (t - fadeStart) / Mathf.Max(0.0001f, 1f - fadeStart);

        var c = baseColor;
        c.a = baseColor.a * alpha;
        sr.color = c;
    }
}
