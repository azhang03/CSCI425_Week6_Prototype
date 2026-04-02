using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;
    public TextMeshProUGUI label;

    void OnEnable()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnXPChanged += UpdateBar;
    }

    void OnDisable()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnXPChanged -= UpdateBar;
    }

    void Start()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += UpdateBar;
            UpdateBar(XPManager.Instance.CurrentXP, XPManager.Instance.XPToNextLevel);
        }
        SetVisible(PauseMenu.AugmentsEnabled);
    }

    void Update()
    {
        bool shouldShow = PauseMenu.AugmentsEnabled;
        if (fillImage != null && fillImage.gameObject.activeSelf != shouldShow)
            SetVisible(shouldShow);
    }

    void SetVisible(bool visible)
    {
        if (fillImage != null) fillImage.gameObject.SetActive(visible);
        if (label != null) label.gameObject.SetActive(visible);
    }

    void UpdateBar(int currentXP, int xpToNextLevel)
    {
        if (fillImage != null)
            fillImage.fillAmount = (float)currentXP / xpToNextLevel;

        if (label != null)
            label.text = $"{currentXP}/{xpToNextLevel} XP";
    }
}
