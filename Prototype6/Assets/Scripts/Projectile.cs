using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public int damage = 1;
    public float lifetime = 5f;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("test collision");
        if (other.gameObject.CompareTag(obstacleTag))
        {
            Destroy(gameObject);
            // add sound effect
        }
    }
}
