using System.Collections;
using UnityEngine;

public class A_Laser : MonoBehaviour
{
    [HideInInspector] public int damage = 2;
    [HideInInspector] public Vector2 direction = Vector2.up;

    [Header("Telegraph")]
    public int blinkCount = 3;
    public float blinkOnTime = 0.15f;
    public float blinkOffTime = 0.15f;
    public float telegraphAlpha = 0.35f;

    [Header("Fire")]
    public float fadeOutDuration = 0.3f;

    [Header("Box Size")]
    public float boxWidth = 0.3f;
    public float boxLength = 24f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        transform.localScale = new Vector3(boxWidth, boxLength, 1f);

        Vector2 center = (Vector2)transform.position + direction * (boxLength * 0.5f);
        transform.position = new Vector3(center.x, center.y, 0f);

        StartCoroutine(LaserSequence());
    }

    IEnumerator LaserSequence()
    {
        Color c = spriteRenderer.color;

        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.color = new Color(c.r, c.g, c.b, telegraphAlpha);
            yield return new WaitForSeconds(blinkOnTime);
            spriteRenderer.color = new Color(c.r, c.g, c.b, 0f);
            yield return new WaitForSeconds(blinkOffTime);
        }

        spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
        DealDamage();

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    void DealDamage()
    {
        Vector2 center = transform.position;
        Vector2 size = new Vector2(boxWidth, boxLength);
        float angle = transform.eulerAngles.z;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

        foreach (var hit in hits)
        {
            A_Enemy aEnemy = hit.GetComponent<A_Enemy>();
            if (aEnemy != null)
            {
                aEnemy.TakeDamage(damage);
                continue;
            }

            S_Enemy sEnemy = hit.GetComponent<S_Enemy>();
            if (sEnemy != null)
                sEnemy.TakeDamage(damage);
        }
    }
}
