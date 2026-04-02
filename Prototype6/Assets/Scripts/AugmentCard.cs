using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class A_AugmentCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public Image backgroundImage;
    public Outline outline;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Colors")]
    public Color defaultOutlineColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color hoverOutlineColor = new Color(1f, 0.85f, 0.3f, 1f);

    private A_AugmentData augmentData;
    private A_AugmentUI parentUI;

    public void Setup(A_AugmentData data, A_AugmentUI parent)
    {
        augmentData = data;
        parentUI = parent;

        if (titleText != null)
            titleText.text = data.augmentName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (outline != null)
        {
            outline.effectColor = defaultOutlineColor;
            outline.effectDistance = new Vector2(3, 3);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null)
            outline.effectColor = hoverOutlineColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null)
            outline.effectColor = defaultOutlineColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentUI != null && augmentData != null)
            parentUI.OnCardSelected(augmentData);
    }
}
