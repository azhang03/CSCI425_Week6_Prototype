using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class A_WeaponInventoryUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI inventoryText;

    private bool subscribed;
    private Dictionary<string, float> highlightTimers = new Dictionary<string, float>();

    void Update()
    {
        if (!subscribed && A_WeaponManager.Instance != null)
        {
            A_WeaponManager.Instance.OnInventoryChanged += UpdateDisplay;
            A_WeaponManager.Instance.OnWeaponFired += OnWeaponFired;
            subscribed = true;
            UpdateDisplay();
        }

        if (subscribed)
            TickHighlights();
    }

    void OnDestroy()
    {
        if (subscribed && A_WeaponManager.Instance != null)
        {
            A_WeaponManager.Instance.OnInventoryChanged -= UpdateDisplay;
            A_WeaponManager.Instance.OnWeaponFired -= OnWeaponFired;
        }
    }

    void OnWeaponFired(string weaponName, float duration)
    {
        if (duration < 0f)
            return;

        highlightTimers[weaponName] = duration;
        UpdateDisplay();
    }

    void TickHighlights()
    {
        bool anyExpired = false;
        var keys = new List<string>(highlightTimers.Keys);

        foreach (var key in keys)
        {
            highlightTimers[key] -= Time.unscaledDeltaTime;
            if (highlightTimers[key] <= 0f)
            {
                highlightTimers.Remove(key);
                anyExpired = true;
            }
        }

        if (anyExpired)
            UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (inventoryText == null || A_WeaponManager.Instance == null) return;

        var weapons = A_WeaponManager.Instance.GetActiveWeapons();

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Weapons</b>");

        foreach (var entry in weapons)
        {
            int pct = Mathf.RoundToInt(entry.currentChance * 100f);
            string name = entry.data.weaponName;

            bool isActive = entry.isOnCooldown ||
                            (highlightTimers.ContainsKey(name) && highlightTimers[name] > 0f);

            if (isActive)
                sb.AppendLine($"<color=#00FF00>{name} - {pct}%</color>");
            else
                sb.AppendLine($"{name} - {pct}%");
        }

        inventoryText.text = sb.ToString();
    }
}
