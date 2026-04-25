using UnityEngine;
using UnityEngine.UI;

// Custom UI graphic for one XP-bar segment.
//
// Draws a trapezoid (sloped top) plus a left-aligned filled trapezoid on top
// of it, in a single mesh. Used by XPBar to make adjacent segments share the
// same slope so the bar reads as a smooth wedge instead of a staircase.
//
// Heights are pixel offsets up from the bottom edge of the RectTransform.
// FillAmount goes 0..1 horizontally and the fill's right edge height is
// linearly interpolated between LeftHeight and RightHeight, so the fill's
// top stays on the same slope as the background.
[RequireComponent(typeof(CanvasRenderer))]
public class XPBarSegmentGraphic : MaskableGraphic
{
    [SerializeField] Color backgroundColor = new Color(0.55f, 0.57f, 0.62f, 1f);
    [SerializeField] Color fillColor       = new Color(1f, 0.78f, 0.16f, 1f);

    [SerializeField, Range(0f, 1f)] float fillAmount = 0f;
    [SerializeField] float leftHeight  = 50f;
    [SerializeField] float rightHeight = 40f;

    public Color BackgroundColor
    {
        get => backgroundColor;
        set { if (backgroundColor == value) return; backgroundColor = value; SetVerticesDirty(); }
    }

    public Color FillColor
    {
        get => fillColor;
        set { if (fillColor == value) return; fillColor = value; SetVerticesDirty(); }
    }

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, fillAmount)) return;
            fillAmount = v;
            SetVerticesDirty();
        }
    }

    public float LeftHeight
    {
        get => leftHeight;
        set { if (Mathf.Approximately(leftHeight, value)) return; leftHeight = value; SetVerticesDirty(); }
    }

    public float RightHeight
    {
        get => rightHeight;
        set { if (Mathf.Approximately(rightHeight, value)) return; rightHeight = value; SetVerticesDirty(); }
    }

    public void SetHeights(float left, float right)
    {
        if (Mathf.Approximately(leftHeight, left) && Mathf.Approximately(rightHeight, right)) return;
        leftHeight  = left;
        rightHeight = right;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var r  = rectTransform.rect;
        float xL = r.xMin;
        float xR = r.xMax;
        float yB = r.yMin;
        float w  = r.width;

        // Background trapezoid: bottom is flat, top edge slopes from
        // (xL, yB+leftHeight) → (xR, yB+rightHeight).
        AddQuad(vh,
            new Vector2(xL, yB),
            new Vector2(xL, yB + leftHeight),
            new Vector2(xR, yB + rightHeight),
            new Vector2(xR, yB),
            backgroundColor);

        if (fillAmount > 0f && w > 0f)
        {
            float midX = xL + w * fillAmount;
            float midH = Mathf.Lerp(leftHeight, rightHeight, fillAmount);
            AddQuad(vh,
                new Vector2(xL,   yB),
                new Vector2(xL,   yB + leftHeight),
                new Vector2(midX, yB + midH),
                new Vector2(midX, yB),
                fillColor);
        }
    }

    static void AddQuad(VertexHelper vh, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, Color c)
    {
        int idx = vh.currentVertCount;
        vh.AddVert(bl, c, Vector2.zero);
        vh.AddVert(tl, c, Vector2.zero);
        vh.AddVert(tr, c, Vector2.zero);
        vh.AddVert(br, c, Vector2.zero);
        vh.AddTriangle(idx,     idx + 1, idx + 2);
        vh.AddTriangle(idx,     idx + 2, idx + 3);
    }
}
