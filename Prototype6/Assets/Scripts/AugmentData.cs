using UnityEngine;

public enum AugmentType
{
    NewWeapon,
    ModifyWeapon,
    Tradeoff,
    ModifyAllWeapons,
    ModifyHealth,
    ModifyFireInterval,
    ModifyWeaponStat
}

public enum TargetMode { Highest, Lowest }

[CreateAssetMenu(fileName = "NewAugment", menuName = "Game/AugmentData")]
public class AugmentData : ScriptableObject
{
    public string augmentName = "Augment";
    [TextArea(2, 4)]
    public string description = "";
    public AugmentType type = AugmentType.NewWeapon;

    [Header("NewWeapon")]
    public WeaponData weaponToAdd;

    [Header("ModifyWeapon (by name)")]
    public string weaponToModify;
    public float chanceDelta;

    [Header("Tradeoff")]
    public TargetMode boostTarget;
    public float boostDelta;
    public TargetMode nerfTarget;
    public float nerfDelta;

    [Header("ModifyAllWeapons")]
    public float allChanceDelta;

    [Header("ModifyHealth")]
    public int healthDelta;

    [Header("ModifyFireInterval")]
    public float intervalDelta;

    [Header("ModifyWeaponStat")]
    public string statWeaponName;
    public string statName;
    public float statDelta;

    public bool isUnique = true;

    [Header("Tier")]
    [Tooltip("Top-strength tier. Shop and level-up cards render with a chromatic pearl/pink border when true.")]
    public bool isPrismatic = false;
}
