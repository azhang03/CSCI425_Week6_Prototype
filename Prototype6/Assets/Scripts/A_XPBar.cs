using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class A_XPBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;
    public TextMeshProUGUI label;

    void OnEnable()
    {
        if (A_XPManager.Instance != null)
            A_XPManager.Instance.OnXPChanged += UpdateBar;
    }

    void OnDisable()
    {
        if (A_XPManager.Instance != null)
            A_XPManager.Instance.OnXPChanged -= UpdateBar;
    }

    void Start()
    {
        if (A_XPManager.Instance != null)
        {
            A_XPManager.Instance.OnXPChanged += UpdateBar;
            UpdateBar(A_XPManager.Instance.CurrentXP, A_XPManager.Instance.XPToNextLevel);
        }
        SetVisible(A_PauseMenu.AugmentsEnabled);
    }

    void Update()
    {
        bool shouldShow = A_PauseMenu.AugmentsEnabled;
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
