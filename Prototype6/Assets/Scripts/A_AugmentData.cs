using UnityEngine;

public enum AugmentType
{
    NewWeapon,
    ModifyWeapon
}

[CreateAssetMenu(fileName = "NewAugment", menuName = "Game/AugmentData")]
public class A_AugmentData : ScriptableObject
{
    public string augmentName = "Augment";
    [TextArea(2, 4)]
    public string description = "";
    public AugmentType type = AugmentType.NewWeapon;

    [Header("NewWeapon")]
    public A_WeaponData weaponToAdd;

    [Header("ModifyWeapon")]
    public string weaponToModify;
    public float chanceDelta;

    public bool isUnique = true;
}
