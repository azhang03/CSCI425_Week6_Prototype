using System;
using System.Collections.Generic;
using UnityEngine;

public class A_WeaponManager : MonoBehaviour
{
    public static A_WeaponManager Instance { get; private set; }

    [Header("Starting Weapon")]
    public A_WeaponData startingWeapon;

    public event Action OnInventoryChanged;

    [Serializable]
    public class WeaponEntry
    {
        public A_WeaponData data;
        public float currentChance;
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

    public List<WeaponEntry> GetActiveWeapons()
    {
        return weapons;
    }
}
