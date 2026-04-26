using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    [Header("Fire Timing")]
    public float fireInterval = 0.7f;
    public float staggerDelay = 0.15f;

    [Header("Projectile Telegraph")]
    public GameObject projectileIndicatorPrefab;   // Child sprite prefab
    public float projectileTelegraphTime = 0.25f;  // Delay before firing
    public float spawnOffset = 0.5f;               // Edge distance from shooter

    [Header("Fireball")]
    [Tooltip("Half-angle of the fireball cone in degrees. 3 bullets at -angle, 0, +angle around a random cardinal.")]
    public float fireballConeHalfAngleDeg = 10f;

    private float fireTimer;
    private float baseFireInterval;

    public AudioManager audioManager;

    [Header("Juice")]
    [Tooltip("Optional. Arcade-style scale pop triggered whenever a weapon fires. Auto-resolved from this GameObject if left empty.")]
    public ShootPunch shootPunch;

    private const float DIAG = 0.7071068f;

    private static readonly Vector2[] Cardinals = {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
    };

    // Four diagonal beams sharing the player origin form an "X" across the arena.
    private static readonly Vector2[] LaserXDirs = {
        new Vector2( DIAG,  DIAG), new Vector2( DIAG, -DIAG),
        new Vector2(-DIAG,  DIAG), new Vector2(-DIAG, -DIAG),
    };

    void Awake()
    {
        if (shootPunch == null)
            shootPunch = GetComponent<ShootPunch>();

        if (shootPunch == null)
            shootPunch = gameObject.AddComponent<ShootPunch>();

        baseFireInterval = fireInterval;
    }

    void TriggerShootPunch()
    {
        if (shootPunch != null)
            shootPunch.Punch();
    }

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
        if (WeaponManager.Instance == null)
            yield break;

        var weapons = WeaponManager.Instance.GetActiveWeapons();
        List<WeaponManager.WeaponEntry> toFire = new List<WeaponManager.WeaponEntry>();

        foreach (var entry in weapons)
        {
            if (entry.isOnCooldown)
                continue;

            if (!PauseMenu.AugmentsEnabled)
            {
                if (entry.data.weaponType == WeaponType.Projectile)
                    toFire.Add(entry);

                continue;
            }

            if (Random.value < entry.currentChance)
                toFire.Add(entry);
        }

        for (int i = 0; i < toFire.Count; i++)
        {
            WeaponManager.WeaponEntry entry = toFire[i];

            if (entry.data.weaponType == WeaponType.Projectile)
            {
                Vector2[] dirs = GetProjectileDirections(entry);
                yield return StartCoroutine(FireProjectileBurst(entry, dirs));
            }
            else
            {
                FireWeaponInstant(entry);
            }

            if (i < toFire.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    Vector2[] GetProjectileDirections(WeaponManager.WeaponEntry entry)
    {
        WeaponData weapon = entry.data;
        string name = weapon.weaponName;

        if (name == "Bullet")
            return Cardinals;

        if (name == "Fireball")
        {
            Vector2 center = Cardinals[Random.Range(0, 4)];
            float halfAngle = fireballConeHalfAngleDeg;
            return new Vector2[]
            {
                RotateDeg(center, -halfAngle),
                center,
                RotateDeg(center,  halfAngle),
            };
        }

        if (weapon.randomize360Direction)
            return new Vector2[] { GetRandom360Direction() };

        return new Vector2[] { GetRandomCardinalDirection() };
    }

    void FireWeaponInstant(WeaponManager.WeaponEntry entry)
    {
        switch (entry.data.weaponType)
        {
            case WeaponType.Area:
                FireArea(entry);
                TriggerShootPunch();
                break;

            case WeaponType.Line:
                FireLine(entry);
                TriggerShootPunch();
                break;
        }
    }

    IEnumerator FireProjectileBurst(WeaponManager.WeaponEntry entry, Vector2[] directions)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null || directions == null || directions.Length == 0)
            yield break;

        List<GameObject> indicators = new List<GameObject>(directions.Length);
        foreach (var dir in directions)
        {
            GameObject ind = CreateProjectileIndicator(dir);
            if (ind != null)
                indicators.Add(ind);
        }

        if (projectileTelegraphTime > 0f)
            yield return new WaitForSeconds(projectileTelegraphTime);

        foreach (var ind in indicators)
        {
            if (ind != null)
                Destroy(ind);
        }

        TriggerShootPunch();

        foreach (var dir in directions)
            FireProjectile(entry, dir);
    }

    GameObject CreateProjectileIndicator(Vector2 direction)
    {
        if (projectileIndicatorPrefab == null)
            return null;

        GameObject indicator = Instantiate(projectileIndicatorPrefab, transform);
        indicator.transform.localPosition = new Vector3(
            direction.x * spawnOffset,
            direction.y * spawnOffset,
            0f
        );
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        indicator.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        return indicator;
    }

    void FireProjectile(WeaponManager.WeaponEntry entry, Vector2 direction)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null)
            return;

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

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage         = weapon.damage + entry.bonusDamage;
            proj.isMagnetic     = weapon.isMagnetic;
            proj.magnetRadius   = weapon.magnetRadius;
            proj.magnetStrength = weapon.magnetStrength;
        }

        if (entry.bonusRadius > 0f)
            projectile.transform.localScale *= (1f + entry.bonusRadius);

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * weapon.projectileSpeed;

        if (audioManager != null)
            audioManager.PlayBullet();

        WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, 0.3f);
    }

    void FireArea(WeaponManager.WeaponEntry entry)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        GameObject moatObj = Instantiate(
            weapon.projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        Moat moat = moatObj.GetComponent<Moat>();
        if (moat != null)
        {
            moat.damage = weapon.damage + entry.bonusDamage;
            moat.duration = weapon.duration + entry.bonusDuration;
            moat.radius = weapon.radius + entry.bonusRadius;
            moat.weaponName = weapon.weaponName;

            if (baseFireInterval > 0f)
                moat.tickInterval *= fireInterval / baseFireInterval;
        }

        WeaponManager.Instance.SetCooldown(weapon.weaponName, true);
        WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, -1f);
    }

    void FireLine(WeaponManager.WeaponEntry entry)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        float laserDuration = 0f;
        bool durationComputed = false;

        foreach (var dir in LaserXDirs)
            SpawnOneLaser(entry, dir, ref laserDuration, ref durationComputed);

        if (durationComputed)
        {
            // Block re-firing until all 4 beams have finished blinking + fading.
            // Mirrors the Moat/Snowball cooldown pattern, but gated on a timer here
            // because we spawn multiple Laser instances per fire.
            WeaponManager.Instance.SetCooldown(weapon.weaponName, true);
            WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, laserDuration);
            StartCoroutine(ClearCooldownAfter(weapon.weaponName, laserDuration));
        }
    }

    IEnumerator ClearCooldownAfter(string weaponName, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (WeaponManager.Instance != null)
            WeaponManager.Instance.SetCooldown(weaponName, false);
    }

    void SpawnOneLaser(WeaponManager.WeaponEntry entry, Vector2 direction, ref float durationOut, ref bool durationComputed)
    {
        WeaponData weapon = entry.data;
        GameObject laserObj = Instantiate(
            weapon.projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        Laser laser = laserObj.GetComponent<Laser>();
        if (laser == null)
            return;

        laser.damage = weapon.damage + entry.bonusDamage;
        laser.bonusWidth = entry.bonusWidth;
        laser.direction = direction;
        laser.Setup();

        if (!durationComputed)
        {
            durationOut = laser.blinkCount * (laser.blinkOnTime + laser.blinkOffTime) + laser.fadeOutDuration;
            durationComputed = true;
        }
    }

    static Vector2 GetRandomCardinalDirection()
    {
        switch (Random.Range(0, 4))
        {
            case 0:  return Vector2.up;
            case 1:  return Vector2.down;
            case 2:  return Vector2.left;
            default: return Vector2.right;
        }
    }

    static Vector2 GetRandom360Direction()
    {
        float angleDeg = Random.Range(0f, 360f);
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    static Vector2 RotateDeg(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
