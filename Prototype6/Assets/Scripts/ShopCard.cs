using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Attached to each affordable shop card at runtime by ShopUI.BuildCard().
// Brightens the outline on hover to signal the card is clickable.
public class ShopCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ShopItem item;
    public ShopUI owner;
    public Outline outline;
    public Color normalColor;
    public Color hoverColor;

    public void OnPointerEnter(PointerEventData _)
    {
        if (outline != null)
            outline.effectColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (outline != null)
            outline.effectColor = normalColor;
    }

    public void OnClick() => owner.TryPurchase(item);
}
