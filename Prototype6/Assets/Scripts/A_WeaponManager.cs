using System;
using System.Collections.Generic;
using UnityEngine;

public class A_WeaponManager : MonoBehaviour
{
    public static A_WeaponManager Instance { get; private set; }

    [Header("Starting Weapon")]
    public A_WeaponData startingWeapon;

    public event Action OnInventoryChanged;
    public event Action<string, float> OnWeaponFired;

    public void NotifyWeaponFired(string weaponName, float duration)
    {
        OnWeaponFired?.Invoke(weaponName, duration);
    }

    [Serializable]
    public class WeaponEntry
    {
        public A_WeaponData data;
        public float currentChance;
        public bool isOnCooldown;
        public float bonusDuration;
        public float bonusRadius;
        public float bonusWidth;
        public int bonusDamage;
    }

    private List<WeaponEntry> weapons = new List<WeaponEntry>();

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
        if (startingWeapon != null)
        {
            weapons.Add(new WeaponEntry
            {
                data = startingWeapon,
                currentChance = startingWeapon.baseFireChance
            });
            OnInventoryChanged?.Invoke();
        }
    }

    public void AddWeapon(A_WeaponData data)
    {
        weapons.Add(new WeaponEntry
        {
            data = data,
            currentChance = data.baseFireChance
        });
        OnInventoryChanged?.Invoke();
    }

    public void ModifyChance(string weaponName, float delta)
    {
        foreach (var entry in weapons)
        {
            if (entry.data.weaponName == weaponName)
            {
                entry.currentChance = Mathf.Clamp01(entry.currentChance + delta);
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }

    public void ModifyAllChances(float delta)
    {
        foreach (var entry in weapons)
            entry.currentChance = Mathf.Clamp01(entry.currentChance + delta);
        OnInventoryChanged?.Invoke();
    }

    public void SetCooldown(string weaponName, bool value)
    {
        foreach (var entry in weapons)
        {
            if (entry.data.weaponName == weaponName)
            {
                entry.isOnCooldown = value;
                return;
            }
        }
    }

    public WeaponEntry GetHighestChanceEntry()
    {
        if (weapons.Count == 0) return null;
        WeaponEntry best = weapons[0];
        for (int i = 1; i < weapons.Count; i++)
        {
            if (weapons[i].currentChance > best.currentChance)
                best = weapons[i];
        }
        return best;
    }

    public WeaponEntry GetLowestChanceEntry()
    {
        if (weapons.Count == 0) return null;
        WeaponEntry worst = weapons[0];
        for (int i = 1; i < weapons.Count; i++)
        {
            if (weapons[i].currentChance < worst.currentChance)
                worst = weapons[i];
        }
        return worst;
    }

    public void ModifyWeaponStat(string weaponName, string stat, float delta)
    {
        foreach (var entry in weapons)
        {
            if (entry.data.weaponName == weaponName)
            {
                switch (stat)
                {
                    case "duration":
                        entry.bonusDuration += delta;
                        break;
                    case "radius":
                        entry.bonusRadius += delta;
                        break;
                    case "width":
                        entry.bonusWidth += delta;
                        break;
                    case "damage":
                        entry.bonusDamage += Mathf.RoundToInt(delta);
                        break;
                }
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }

    public List<WeaponEntry> GetActiveWeapons()
    {
        return weapons;
    }

    public bool HasWeapon(string weaponName)
    {
        foreach (var entry in weapons)
        {
            if (entry.data.weaponName == weaponName)
                return true;
        }
        return false;
    }

    public int GetWeaponCount()
    {
        return weapons.Count;
    }
}
