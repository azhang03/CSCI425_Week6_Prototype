using System.Collections.Generic;
using UnityEngine;

public class A_Moat : MonoBehaviour
{
    [HideInInspector] public int damage = 1;
    [HideInInspector] public float duration = 4f;
    [HideInInspector] public float radius = 2f;
    [HideInInspector] public string weaponName = "Moat";

    private float lifetime;
    private Dictionary<int, float> tickTimers = new Dictionary<int, float>();

    void Start()
    {
        lifetime = duration;

        float visualScale = radius * 2f;
        transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = CreateCircleSprite(64);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
            col.radius = 0.5f;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        List<int> keys = new List<int>(tickTimers.Keys);
        foreach (int id in keys)
            tickTimers[id] -= Time.deltaTime;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        int id = other.gameObject.GetInstanceID();

        if (tickTimers.ContainsKey(id) && tickTimers[id] > 0f)
            return;

        tickTimers[id] = 1f;

        A_Enemy aEnemy = other.GetComponent<A_Enemy>();
        if (aEnemy != null)
        {
            aEnemy.TakeDamage(damage);
            return;
        }

        S_Enemy sEnemy = other.GetComponent<S_Enemy>();
        if (sEnemy != null)
            sEnemy.TakeDamage(damage);
    }

    void OnDestroy()
    {
        if (A_WeaponManager.Instance != null)
            A_WeaponManager.Instance.SetCooldown(weaponName, false);
    }

    static Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = resolution * 0.5f;
        float radiusSq = center * center;

        Color[] pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distSq = dx * dx + dy * dy;
                pixels[y * resolution + x] = distSq <= radiusSq ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
    }
}
