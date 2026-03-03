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
public class A_AugmentData : ScriptableObject
{
    public string augmentName = "Augment";
    [TextArea(2, 4)]
    public string description = "";
    public AugmentType type = AugmentType.NewWeapon;

    [Header("NewWeapon")]
    public A_WeaponData weaponToAdd;

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
}
