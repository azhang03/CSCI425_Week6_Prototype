using System.Collections.Generic;
using UnityEngine;

public class A_AugmentPool : MonoBehaviour
{
    public static A_AugmentPool Instance { get; private set; }

    [Header("All Augments")]
    public List<A_AugmentData> allAugments = new List<A_AugmentData>();

    private List<A_AugmentData> availablePool = new List<A_AugmentData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        availablePool = new List<A_AugmentData>(allAugments);
    }

    public List<A_AugmentData> GetCards(int count)
    {
        List<A_AugmentData> result = new List<A_AugmentData>();

        if (availablePool.Count == 0)
            return result;

        List<A_AugmentData> tempPool = new List<A_AugmentData>(availablePool);

        while (result.Count < count && tempPool.Count > 0)
        {
            int idx = Random.Range(0, tempPool.Count);
            result.Add(tempPool[idx]);
            tempPool.RemoveAt(idx);
        }

        while (result.Count < count)
        {
            result.Add(result[Random.Range(0, result.Count)]);
        }

        return result;
    }

    public void ApplyAugment(A_AugmentData augment)
    {
        if (A_WeaponManager.Instance == null) return;

        switch (augment.type)
        {
            case AugmentType.NewWeapon:
                if (augment.weaponToAdd != null)
                    A_WeaponManager.Instance.AddWeapon(augment.weaponToAdd);
                break;

            case AugmentType.ModifyWeapon:
                A_WeaponManager.Instance.ModifyChance(augment.weaponToModify, augment.chanceDelta);
                break;

            case AugmentType.Tradeoff:
                ApplyTradeoff(augment);
                break;

            case AugmentType.ModifyAllWeapons:
                A_WeaponManager.Instance.ModifyAllChances(augment.allChanceDelta);
                break;

            case AugmentType.ModifyHealth:
                if (A_PlayerHealth.Instance != null)
                    A_PlayerHealth.Instance.AddMaxHearts(augment.healthDelta);
                if (augment.allChanceDelta != 0f)
                    A_WeaponManager.Instance.ModifyAllChances(augment.allChanceDelta);
                break;

            case AugmentType.ModifyFireInterval:
                A_Shooting shooter = FindAnyObjectByType<A_Shooting>();
                if (shooter != null)
                    shooter.fireInterval = Mathf.Max(0.5f, shooter.fireInterval + augment.intervalDelta);
                break;

            case AugmentType.ModifyWeaponStat:
                A_WeaponManager.Instance.ModifyWeaponStat(
                    augment.statWeaponName, augment.statName, augment.statDelta);
                break;
        }

        if (augment.isUnique)
            availablePool.Remove(augment);
    }

    void ApplyTradeoff(A_AugmentData augment)
    {
        var boost = augment.boostTarget == TargetMode.Highest
            ? A_WeaponManager.Instance.GetHighestChanceEntry()
            : A_WeaponManager.Instance.GetLowestChanceEntry();

        var nerf = augment.nerfTarget == TargetMode.Highest
            ? A_WeaponManager.Instance.GetHighestChanceEntry()
            : A_WeaponManager.Instance.GetLowestChanceEntry();

        if (boost != null)
            A_WeaponManager.Instance.ModifyChance(boost.data.weaponName, augment.boostDelta);
        if (nerf != null)
            A_WeaponManager.Instance.ModifyChance(nerf.data.weaponName, augment.nerfDelta);
    }
}
