using UnityEngine;
using TMPro;

public class A_WeaponInventoryUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI inventoryText;

    void OnEnable()
    {
        if (A_WeaponManager.Instance != null)
            A_WeaponManager.Instance.OnInventoryChanged += UpdateDisplay;
    }

    void OnDisable()
    {
        if (A_WeaponManager.Instance != null)
            A_WeaponManager.Instance.OnInventoryChanged -= UpdateDisplay;
    }

    void Start()
    {
        if (A_WeaponManager.Instance != null)
        {
            A_WeaponManager.Instance.OnInventoryChanged += UpdateDisplay;
            UpdateDisplay();
        }
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
            sb.AppendLine($"{entry.data.weaponName} - {pct}%");
        }

        inventoryText.text = sb.ToString();
    }
}
