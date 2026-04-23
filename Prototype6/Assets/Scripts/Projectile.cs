using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public int damage = 1;
    public float lifetime = 5f;

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.92f, 0.15f, 0.5f);
    public float glowScale = 2.2f;

    // Set by Shooting.FireProjectile() at spawn — same pattern as damage
    [HideInInspector] public bool  isMagnetic     = false;
    [HideInInspector] public float magnetRadius   = 5f;
    [HideInInspector] public float magnetStrength = 5f;

    private Transform   committedTarget;
    private Rigidbody2D rb;

    private string obstacleTag = "Obstacle";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        AddGlow();
    }

    void Update()
    {
        if (!isMagnetic || rb == null) return;

        if (committedTarget == null)
            committedTarget = FindBestTarget();

        if (committedTarget == null) return;

        Vector2 toTarget = ((Vector2)committedTarget.position - rb.position).normalized;
        float   speed    = rb.linearVelocity.magnitude;
        Vector2 newDir   = Vector2.Lerp(rb.linearVelocity.normalized, toTarget,
                                        magnetStrength * Time.deltaTime).normalized;
        rb.linearVelocity = newDir * speed;
    }

    Transform FindBestTarget()
    {
        if (PlayerHealth.Instance == null) return null;

        Vector2 playerPos = PlayerHealth.Instance.transform.position;
        Vector2 bulletPos = rb.position;
        Vector2 bulletDir = rb.linearVelocity.normalized;

        Collider2D[] hits = Physics2D.OverlapCircleAll(bulletPos, magnetRadius);

        Transform bestTarget = null;
        float     bestDistSq = float.MaxValue;
        float     bestDot    = -2f;

        const float tiebreakThreshold = 0.1f;

        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponent<Enemy>() == null) continue;

            Vector2 enemyPos       = (Vector2)hit.transform.position;
            float   distSqToPlayer = (enemyPos - playerPos).sqrMagnitude;

            if (bestTarget == null)
            {
                bestTarget = hit.transform;
                bestDistSq = distSqToPlayer;
                bestDot    = Vector2.Dot(bulletDir, (enemyPos - bulletPos).normalized);
                continue;
            }

            float distDiff = Mathf.Abs(Mathf.Sqrt(distSqToPlayer) - Mathf.Sqrt(bestDistSq));

            if (distDiff > tiebreakThreshold)
            {
                if (distSqToPlayer < bestDistSq)
                {
                    bestTarget = hit.transform;
                    bestDistSq = distSqToPlayer;
                    bestDot    = Vector2.Dot(bulletDir, (enemyPos - bulletPos).normalized);
                }
            }
            else
            {
                // Tiebreaker: prefer enemy most "ahead" in current bullet path
                float dot = Vector2.Dot(bulletDir, (enemyPos - bulletPos).normalized);
                if (dot > bestDot)
                {
                    bestTarget = hit.transform;
                    bestDistSq = distSqToPlayer;
                    bestDot    = dot;
                }
            }
        }

        return bestTarget;
    }

    void AddGlow()
    {
        SpriteRenderer parentSr = GetComponent<SpriteRenderer>();

        var glow = new GameObject("Glow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale    = Vector3.one * glowScale;

        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite           = CreateGlowSprite(32);
        sr.color            = glowColor;
        sr.sortingLayerName = parentSr != null ? parentSr.sortingLayerName : "Entities";
        sr.sortingOrder     = (parentSr != null ? parentSr.sortingOrder : 0) - 1;
    }

    static Sprite CreateGlowSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        float center = res * 0.5f;
        var pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - center + 0.5f;
            float dy = y - center + 0.5f;
            float t  = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / center);
            pixels[y * res + x] = new Color(1f, 1f, 1f, 1f - t);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag(obstacleTag))
        {
            Shield shield = other.GetComponent<Shield>();
            if (shield != null)
                shield.TakeHit(damage);
            Destroy(gameObject);
        }
    }
}
