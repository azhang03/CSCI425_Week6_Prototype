using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverDimmer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image target;
    private static readonly Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target != null) target.color = dimColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target != null) target.color = Color.white;
    }
}
