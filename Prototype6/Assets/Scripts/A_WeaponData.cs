using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/WeaponData")]
public class A_WeaponData : ScriptableObject
{
    public string weaponName = "Weapon";
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public int damage = 1;

    [Range(0f, 1f)]
    public float baseFireChance = 1f;

    public float projectileScale = 0.25f;
    public Color projectileColor = Color.white;
}
