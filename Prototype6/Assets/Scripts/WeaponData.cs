using UnityEngine;

public enum WeaponType { Projectile, Area, Line }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Weapon";
    public WeaponType weaponType = WeaponType.Projectile;
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public int damage = 1;

    [Range(0f, 1f)]
    public float baseFireChance = 1f;

    [Header("Area Weapon (Moat)")]
    public float duration = 0f;
    public float radius = 0f;

    [Header("Magnetic Bullet")]
    public bool isMagnetic = false;

    [Range(0f, 20f)]
    public float magnetRadius = 5f;

    [Range(0f, 20f)]
    public float magnetStrength = 5f;
}
