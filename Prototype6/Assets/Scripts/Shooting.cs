using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    [Header("Fire Timing")]
    public float fireInterval = 1.5f;
    public float staggerDelay = 0.15f;

    [Header("Projectile Telegraph")]
    public GameObject projectileIndicatorPrefab;   // Child sprite prefab
    public float projectileTelegraphTime = 0.25f;  // Delay before firing
    public float spawnOffset = 0.5f;               // Edge distance from shooter

    private float fireTimer;

    public AudioManager audioManager;

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
                yield return StartCoroutine(FireProjectileWithTelegraph(entry));
            else
                FireWeaponInstant(entry);

            if (i < toFire.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    void FireWeaponInstant(WeaponManager.WeaponEntry entry)
    {
        switch (entry.data.weaponType)
        {
            case WeaponType.Area:
                FireArea(entry);
                break;

            case WeaponType.Line:
                FireLine(entry);
                break;
        }
    }

    IEnumerator FireProjectileWithTelegraph(WeaponManager.WeaponEntry entry)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null)
            yield break;

        Vector2 direction = GetRandomDirection();

        GameObject indicator = CreateProjectileIndicator(direction);

        if (projectileTelegraphTime > 0f)
            yield return new WaitForSeconds(projectileTelegraphTime);

        if (indicator != null)
            Destroy(indicator);

        FireProjectile(entry, direction);
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
        // indicator.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
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
            proj.canBreakShield = entry.bonusBreakShield;
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
        }

        WeaponManager.Instance.SetCooldown(weapon.weaponName, true);
        WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, -1f);
    }

    void FireLine(WeaponManager.WeaponEntry entry)
    {
        WeaponData weapon = entry.data;
        if (weapon.projectilePrefab == null) return;

        Vector2 direction = GetRandomDirection();

        GameObject laserObj = Instantiate(
            weapon.projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        Laser laser = laserObj.GetComponent<Laser>();
        if (laser != null)
        {
            laser.damage = weapon.damage + entry.bonusDamage;
            laser.bonusWidth = entry.bonusWidth;
            laser.direction = direction;
            laser.Setup();

            float laserDuration = laser.blinkCount * (laser.blinkOnTime + laser.blinkOffTime) + laser.fadeOutDuration;
            WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, laserDuration);
        }
    }

    Vector2 GetRandomDirection()
    {
        // Use the pre-generated queue when available so BulletDirectionUI can
        // show upcoming directions.  Falls back to inline random if no queue.
        if (BulletDirectionQueue.Instance != null)
            return BulletDirectionQueue.Instance.Dequeue();

        switch (Random.Range(0, 4))
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            default: return Vector2.right;
        }
    }
}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Shooting : MonoBehaviour
//{
//    public float fireInterval = 1.5f;
//    public float spawnOffset = 0.5f;
//    public float staggerDelay = 0.15f;

//    private float fireTimer;

//    public AudioManager audioManager;


//    void Update()
//    {
//        fireTimer -= Time.deltaTime;

//        if (fireTimer <= 0f)
//        {
//            fireTimer = fireInterval;
//            if(audioManager != null ) 
//                audioManager.PlayBullet();
//            StartCoroutine(FireWeapons());
//        }
//    }

//    IEnumerator FireWeapons()
//    {
//        if (WeaponManager.Instance == null) yield break;

//        var weapons = WeaponManager.Instance.GetActiveWeapons();
//        List<WeaponManager.WeaponEntry> toFire = new List<WeaponManager.WeaponEntry>();

//        foreach (var entry in weapons)
//        {
//            if (entry.isOnCooldown) continue;

//            if (!PauseMenu.AugmentsEnabled)
//            {
//                if (entry.data.weaponType == WeaponType.Projectile)
//                    toFire.Add(entry);
//                continue;
//            }

//            if (Random.value < entry.currentChance)
//                toFire.Add(entry);
//        }

//        for (int i = 0; i < toFire.Count; i++)
//        {
//            FireWeapon(toFire[i]);
//            if (i < toFire.Count - 1)
//                yield return new WaitForSeconds(staggerDelay);
//        }
//    }

//    void FireWeapon(WeaponManager.WeaponEntry entry)
//    {
//        switch (entry.data.weaponType)
//        {
//            case WeaponType.Projectile:
//                FireProjectile(entry);
//                //audioManager.PlayBullet();
//                break;
//            case WeaponType.Area:
//                //audioManager.PlayAreaWeapon();
//                FireArea(entry);
//                break;
//            case WeaponType.Line:
//               // audioManager.PlayLaser();
//                FireLine(entry);
//                break;
//        }
//    }

//    void FireProjectile(WeaponManager.WeaponEntry entry)
//    {
//        WeaponData weapon = entry.data;
//        if (weapon.projectilePrefab == null) return;

//        Vector2 direction = GetRandomDirection();

//        Vector3 spawnPos = new Vector3(
//            transform.position.x + spawnOffset * direction.x,
//            transform.position.y + spawnOffset * direction.y,
//            transform.position.z
//        );

//        GameObject projectile = Instantiate(
//            weapon.projectilePrefab,
//            spawnPos,
//            Quaternion.identity
//        );

//        Projectile proj = projectile.GetComponent<Projectile>();
//        if (proj != null)
//            proj.damage = weapon.damage + entry.bonusDamage;

//        if (entry.bonusRadius > 0f)
//            projectile.transform.localScale *= (1f + entry.bonusRadius);

//        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
//        if (rb != null)
//            rb.linearVelocity = direction * weapon.projectileSpeed;

//        WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, 0.3f);
//    }

//    void FireArea(WeaponManager.WeaponEntry entry)
//    {
//        WeaponData weapon = entry.data;
//        if (weapon.projectilePrefab == null) return;

//        GameObject moatObj = Instantiate(
//            weapon.projectilePrefab,
//            transform.position,
//            Quaternion.identity
//        );

//        Moat moat = moatObj.GetComponent<Moat>();
//        if (moat != null)
//        {
//            moat.damage = weapon.damage + entry.bonusDamage;
//            moat.duration = weapon.duration + entry.bonusDuration;
//            moat.radius = weapon.radius + entry.bonusRadius;
//            moat.weaponName = weapon.weaponName;
//        }

//        WeaponManager.Instance.SetCooldown(weapon.weaponName, true);
//        WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, -1f);
//    }

//    void FireLine(WeaponManager.WeaponEntry entry)
//    {
//        WeaponData weapon = entry.data;
//        if (weapon.projectilePrefab == null) return;

//        Vector2 direction = GetRandomDirection();

//        GameObject laserObj = Instantiate(
//            weapon.projectilePrefab,
//            transform.position,
//            Quaternion.identity
//        );

//        Laser laser = laserObj.GetComponent<Laser>();
//        if (laser != null)
//        {
//            laser.damage = weapon.damage + entry.bonusDamage;
//            laser.bonusWidth = entry.bonusWidth;
//            laser.direction = direction;
//            laser.Setup();

//            float laserDuration = laser.blinkCount * (laser.blinkOnTime + laser.blinkOffTime) + laser.fadeOutDuration;
//            WeaponManager.Instance.NotifyWeaponFired(weapon.weaponName, laserDuration);
//        }
//    }

//    Vector2 GetRandomDirection()
//    {
//        int random = Random.Range(0, 4);

//        switch (random)
//        {
//            case 0: return Vector2.up;
//            case 1: return Vector2.down;
//            case 2: return Vector2.left;
//            default: return Vector2.right;
//        }
//    }
//}
