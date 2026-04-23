using System.Collections.Generic;
using UnityEngine;

public class AugmentPool : MonoBehaviour
{
    public static AugmentPool Instance { get; private set; }

    [Header("All Augments")]
    public List<AugmentData> allAugments = new List<AugmentData>();

    private List<AugmentData> availablePool = new List<AugmentData>();
    private bool firstLevelUp = true;

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
        availablePool = new List<AugmentData>(allAugments);

        if (SceneFlowManager.Instance != null)
        {
            foreach (var aug in SceneFlowManager.Instance.GetShopPurchases())
                AddToPool(aug);
        }
    }

    public void AddToPool(AugmentData augment)
    {
        if (augment != null && !availablePool.Contains(augment))
            availablePool.Add(augment);
    }

    public List<AugmentData> GetCards(int count)
    {
        List<AugmentData> result = new List<AugmentData>();

        if (firstLevelUp)
        {
            firstLevelUp = false;

            // Collect all available NewWeapon augments then shuffle,
            // so shop-purchased weapons (e.g. Snowball) have an equal chance to appear.
            List<AugmentData> newWeapons = new List<AugmentData>();
            foreach (var aug in availablePool)
                if (aug.type == AugmentType.NewWeapon)
                    newWeapons.Add(aug);

            // Fisher-Yates shuffle
            for (int i = newWeapons.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                AugmentData tmp = newWeapons[i];
                newWeapons[i] = newWeapons[j];
                newWeapons[j] = tmp;
            }

            for (int i = 0; i < Mathf.Min(count, newWeapons.Count); i++)
                result.Add(newWeapons[i]);

            if (result.Count > 0) return result;
        }

        List<AugmentData> eligible = new List<AugmentData>();
        foreach (var aug in availablePool)
        {
            if (IsEligible(aug))
                eligible.Add(aug);
        }

        if (eligible.Count == 0)
            return result;

        List<AugmentData> tempPool = new List<AugmentData>(eligible);

        while (result.Count < count && tempPool.Count > 0)
        {
            int idx = Random.Range(0, tempPool.Count);
            AugmentData picked = tempPool[idx];
            tempPool.RemoveAt(idx);

            if (result.Contains(picked))
                continue;

            result.Add(picked);
        }

        while (result.Count < count && result.Count > 0)
        {
            result.Add(result[Random.Range(0, result.Count)]);
        }

        return result;
    }

    bool IsEligible(AugmentData augment)
    {
        if (WeaponManager.Instance == null) return true;

        switch (augment.type)
        {
            case AugmentType.ModifyWeaponStat:
                return WeaponManager.Instance.HasWeapon(augment.statWeaponName);

            case AugmentType.Tradeoff:
                return WeaponManager.Instance.GetWeaponCount() >= 2;

            case AugmentType.ModifyHealth:
                if (augment.healthDelta < 0 && PlayerHealth.Instance != null)
                    return PlayerHealth.Instance.CurrentHearts > Mathf.Abs(augment.healthDelta);
                return true;

            default:
                return true;
        }
    }

    public void ApplyAugment(AugmentData augment)
    {
        if (WeaponManager.Instance == null) return;

        switch (augment.type)
        {
            case AugmentType.NewWeapon:
                if (augment.weaponToAdd != null)
                    WeaponManager.Instance.AddWeapon(augment.weaponToAdd);
                break;

            case AugmentType.ModifyWeapon:
                WeaponManager.Instance.ModifyChance(augment.weaponToModify, augment.chanceDelta);
                break;

            case AugmentType.Tradeoff:
                ApplyTradeoff(augment);
                break;

            case AugmentType.ModifyAllWeapons:
                WeaponManager.Instance.ModifyAllChances(augment.allChanceDelta);
                break;

            case AugmentType.ModifyHealth:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.AddMaxHearts(augment.healthDelta);
                if (augment.allChanceDelta != 0f)
                    WeaponManager.Instance.ModifyAllChances(augment.allChanceDelta);
                break;

            case AugmentType.ModifyFireInterval:
                Shooting shooter = FindAnyObjectByType<Shooting>();
                if (shooter != null)
                    shooter.fireInterval = Mathf.Max(0.5f, shooter.fireInterval + augment.intervalDelta);
                break;

            case AugmentType.ModifyWeaponStat:
                WeaponManager.Instance.ModifyWeaponStat(
                    augment.statWeaponName, augment.statName, augment.statDelta);
                break;
        }

        if (augment.isUnique)
            availablePool.Remove(augment);
    }

    void ApplyTradeoff(AugmentData augment)
    {
        var boost = augment.boostTarget == TargetMode.Highest
            ? WeaponManager.Instance.GetHighestChanceEntry()
            : WeaponManager.Instance.GetLowestChanceEntry();

        var nerf = augment.nerfTarget == TargetMode.Highest
            ? WeaponManager.Instance.GetHighestChanceEntry()
            : WeaponManager.Instance.GetLowestChanceEntry();

        if (boost != null)
            WeaponManager.Instance.ModifyChance(boost.data.weaponName, augment.boostDelta);
        if (nerf != null)
            WeaponManager.Instance.ModifyChance(nerf.data.weaponName, augment.nerfDelta);
    }
}
