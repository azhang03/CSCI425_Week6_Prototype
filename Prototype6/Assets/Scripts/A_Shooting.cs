using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A_Shooting : MonoBehaviour
{
    public float fireInterval = 1.5f;
    public float spawnOffset = 0.5f;
    public float staggerDelay = 0.15f;

    private float fireTimer;

    void Update()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            StartCoroutine(FireWeapons());
        }
    }

    IEnumerator FireWeapons()
    {
        if (A_WeaponManager.Instance == null) yield break;

        var weapons = A_WeaponManager.Instance.GetActiveWeapons();
        List<A_WeaponManager.WeaponEntry> toFire = new List<A_WeaponManager.WeaponEntry>();

        foreach (var entry in weapons)
        {
            if (Random.value < entry.currentChance)
                toFire.Add(entry);
        }

        for (int i = 0; i < toFire.Count; i++)
        {
            FireProjectile(toFire[i]);

            if (i < toFire.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    void FireProjectile(A_WeaponManager.WeaponEntry entry)
    {
        A_WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        Vector2 direction = GetRandomDirection();

        Vector3 spawnPos = new Vector3(
            transform.position.x + spawnOffset * direction.x,
            transform.position.y + spawnOffset * direction.y,
            transform.position.z
        );

        GameObject projectile = Instantiate(
            weapon.projectilePrefab,
            spawnPos,
            Quaternion.identity
        );

        projectile.transform.localScale = Vector3.one * weapon.projectileScale;

        SpriteRenderer sr = projectile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = weapon.projectileColor;

        A_Projectile proj = projectile.GetComponent<A_Projectile>();
        if (proj != null)
            proj.damage = weapon.damage;

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * weapon.projectileSpeed;
    }

    Vector2 GetRandomDirection()
    {
        int random = Random.Range(0, 4);

        switch (random)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            default: return Vector2.right;
        }
    }
}
