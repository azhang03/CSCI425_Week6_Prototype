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
            if (entry.isOnCooldown) continue;
            if (Random.value < entry.currentChance)
                toFire.Add(entry);
        }

        for (int i = 0; i < toFire.Count; i++)
        {
            FireWeapon(toFire[i]);

            if (i < toFire.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    void FireWeapon(A_WeaponManager.WeaponEntry entry)
    {
        switch (entry.data.weaponType)
        {
            case WeaponType.Projectile:
                FireProjectile(entry);
                break;
            case WeaponType.Area:
                FireArea(entry);
                break;
            case WeaponType.Line:
                FireLine(entry);
                break;
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

        A_Projectile proj = projectile.GetComponent<A_Projectile>();
        if (proj != null)
            proj.damage = weapon.damage + entry.bonusDamage;

        if (entry.bonusRadius > 0f)
            projectile.transform.localScale *= (1f + entry.bonusRadius);

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * weapon.projectileSpeed;
    }

    void FireArea(A_WeaponManager.WeaponEntry entry)
    {
        A_WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        GameObject moatObj = Instantiate(
            weapon.projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        A_Moat moat = moatObj.GetComponent<A_Moat>();
        if (moat != null)
        {
            moat.damage = weapon.damage + entry.bonusDamage;
            moat.duration = weapon.duration + entry.bonusDuration;
            moat.radius = weapon.radius + entry.bonusRadius;
            moat.weaponName = weapon.weaponName;
        }

        A_WeaponManager.Instance.SetCooldown(weapon.weaponName, true);
    }

    void FireLine(A_WeaponManager.WeaponEntry entry)
    {
        A_WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        Vector2 direction = GetRandomDirection();

        GameObject laserObj = Instantiate(
            weapon.projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        A_Laser laser = laserObj.GetComponent<A_Laser>();
        if (laser != null)
        {
            laser.damage = weapon.damage + entry.bonusDamage;
            laser.bonusWidth = entry.bonusWidth;
            laser.direction = direction;
            laser.Setup();
        }
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
