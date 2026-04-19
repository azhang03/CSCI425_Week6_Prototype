using UnityEngine;
using UnityEngine.EventSystems;

public class StageCarouselItem : MonoBehaviour, IPointerEnterHandler
{
    public enum Slot
    {
        Left,
        Center,
        Right
    }

    [SerializeField] private StageSelectUI owner;
    [SerializeField] private Slot slot = Slot.Center;

    public void Set(StageSelectUI stageSelectUI, Slot which)
    {
        owner = stageSelectUI;
        slot = which;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner == null) owner = FindAnyObjectByType<StageSelectUI>();
        if (owner == null) return;

        switch (slot)
        {
            case Slot.Left:
                owner.HoverLeftRecord();
                break;
            case Slot.Right:
                owner.HoverRightRecord();
                break;
        }
    }
}

