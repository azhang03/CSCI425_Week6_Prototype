using UnityEngine;
using UnityEngine.UI;

// UI-space sibling of DeathParticles. Bursts a cluster of tiny colored squares
// at a screen point on a Canvas — used when the player loses a heart so the
// lost heart visually shatters.
public static class HeartParticles
{
    const float MinSpeedPx     = 200f;
    const float MaxSpeedPx     = 480f;
    const float MinLifetime    = 0.55f;
    const float MaxLifetime    = 0.95f;
    const float MinAngularVel  = -720f;
    const float MaxAngularVel  =  720f;
    const float GravityPx      = 1100f;
    const float MinSizeFrac    = 0.20f;
    const float MaxSizeFrac    = 0.55f;

    public static void Spawn(
        Canvas canvas,
        Vector3 worldPos,
        Color color,
        float referenceSizePx = 28f,
        int count = 18)
    {
        if (canvas == null) return;

        Transform parent = canvas.transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("HeartParticle", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            float sideFrac = Random.Range(MinSizeFrac, MaxSizeFrac);
            float side     = referenceSizePx * sideFrac;

            rt.position = worldPos;
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            rt.localScale = Vector3.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(side, side);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(MinSpeedPx, MaxSpeedPx);

            var p = go.AddComponent<HeartParticle>();
            p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            p.angularVelocity = Random.Range(MinAngularVel, MaxAngularVel);
            p.lifetime = Random.Range(MinLifetime, MaxLifetime);
            p.gravity = GravityPx;
        }
    }
}

// Per-particle ticker for UI hearts. Mirrors DeathParticle but operates in
// UI/screen space (pixel-magnitude velocities, RectTransform position).
public class HeartParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float   angularVelocity;
    public float   lifetime  = 0.8f;
    public float   gravity   = 1100f;
    public float   fadeStart = 0.55f;

    Graphic graphic;
    Color   baseColor;
    float   age;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        if (graphic != null) baseColor = graphic.color;
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

        if (graphic == null) return;

        float t = age / lifetime;
        float alpha = t < fadeStart
            ? 1f
            : 1f - (t - fadeStart) / Mathf.Max(0.0001f, 1f - fadeStart);

        var c = baseColor;
        c.a = baseColor.a * alpha;
        graphic.color = c;
    }
}
