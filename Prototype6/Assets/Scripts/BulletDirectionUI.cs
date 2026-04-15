using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds and manages the "Next Shots" HUD panel entirely in code.
///
/// Setup:
///   1. Add this component to any empty child GameObject inside the Canvas.
///   2. (Optional) assign a Texture2D to Arrow Texture.
///      The texture does NOT need Read/Write Enabled — no pixel data is read.
///   BulletDirectionQueue is created automatically if missing.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BulletDirectionUI : MonoBehaviour
{
    [Header("Arrow Texture (blank = procedural)")]
    [SerializeField] Texture2D arrowTexture;

    [Header("Layout")]
    [SerializeField][Min(1)]  int   arrowCount = 6;
    [SerializeField][Min(16)] float arrowSize  = 52f;
    [Tooltip("Vertical offset from the right-center anchor (positive = up).")]
    [SerializeField] float yOffset = 0f;

    // ── Layout constants ─────────────────────────────────────────────────────
    private const float PanelWidth = 76f;
    private const float Padding    = 8f;
    private const float Spacing    = 5f;
    private const float HeaderH    = 18f;
    private const float TimerBarH  = 5f;
    private const float SepH       = 1f;

    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color BgColor      = new Color(0.07f, 0.07f, 0.13f, 0.85f);
    private static readonly Color TimerBgCol   = new Color(0.20f, 0.20f, 0.30f, 1.00f);
    private static readonly Color TimerFillCol = new Color(0.30f, 0.90f, 1.00f, 1.00f);
    private static readonly Color HeaderCol    = new Color(0.75f, 0.80f, 0.95f, 1.00f);
    private static readonly Color ColNext      = new Color(1.00f, 0.92f, 0.20f, 1.00f);
    private static readonly Color ColSecond    = new Color(1.00f, 1.00f, 1.00f, 0.80f);
    private static readonly Color ColFaded     = new Color(1.00f, 1.00f, 1.00f, 0.30f);

    // ── Runtime ──────────────────────────────────────────────────────────────
    // Each element is the inner Image child (rotatable without layout interference)
    private Image[] _arrowImgs;
    private Image   _timerFill;
    private Sprite  _arrowSprite;

    private bool     _subscribed;
    private float    _fireInterval = 1.5f;
    private float    _countdown;
    private Shooting _shooting;

    // =========================================================================
    void Start()
    {
        try
        {
            EnsureQueue();
            _arrowSprite = BuildArrowSprite();
            BuildUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BulletDirectionUI] Build failed: {e}");
        }

        TrySubscribe();
    }

    void Update()
    {
        TrySubscribe();
        TickTimer();
    }

    void OnDestroy()
    {
        if (_subscribed && BulletDirectionQueue.Instance != null)
            BulletDirectionQueue.Instance.OnQueueChanged -= RefreshArrows;
    }

    // =========================================================================
    // Queue
    // =========================================================================
    static void EnsureQueue()
    {
        if (BulletDirectionQueue.Instance != null) return;
        new GameObject("BulletDirectionQueue").AddComponent<BulletDirectionQueue>();
    }

    // =========================================================================
    // Single arrow sprite — no pixel reads, works with any texture import settings
    // =========================================================================
    Sprite BuildArrowSprite()
    {
        if (arrowTexture != null)
            // Use the user texture directly — no GetPixels call
            return Sprite.Create(arrowTexture,
                new Rect(0f, 0f, arrowTexture.width, arrowTexture.height),
                new Vector2(0.5f, 0.5f));

        return MakeProceduralArrowSprite(Mathf.Max(16, Mathf.RoundToInt(arrowSize)));
    }

    // =========================================================================
    // UI construction
    // =========================================================================
    void BuildUI()
    {
        float contentW = PanelWidth - Padding * 2f;
        float totalH   = Padding
                       + HeaderH   + Spacing
                       + TimerBarH + Spacing
                       + SepH      + Spacing
                       + arrowCount * arrowSize
                       + (arrowCount - 1) * Spacing
                       + Padding;

        // ── Root rect: right-center of Canvas ────────────────────────────────
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-14f, yOffset);
        rt.sizeDelta        = new Vector2(PanelWidth, totalH);

        // ── Background (full-stretch child so Image.Awake can't reset it) ────
        CreateStretchChild("Bg", transform, BgColor);

        float y = -Padding;

        // ── Header ────────────────────────────────────────────────────────────
        {
            var go = CreateRect("Header", transform, Padding, y, contentW, HeaderH);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text            = "NEXT";
            tmp.fontSize        = 11f;
            tmp.fontStyle       = FontStyles.Bold;
            tmp.alignment       = TextAlignmentOptions.Center;
            tmp.color           = HeaderCol;
            tmp.raycastTarget   = false;
            y -= HeaderH + Spacing;
        }

        // ── Timer bar ─────────────────────────────────────────────────────────
        {
            var bar = CreateRect("TimerBar", transform, Padding, y, contentW, TimerBarH);
            var barImg = bar.AddComponent<Image>();
            barImg.color = TimerBgCol;
            barImg.raycastTarget = false;

            var fill = CreateStretchChild("Fill", bar.transform, TimerFillCol);
            _timerFill            = fill.GetComponent<Image>();
            _timerFill.type       = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillOrigin = 0;
            _timerFill.fillAmount = 1f;
            _timerFill.raycastTarget = false;

            y -= TimerBarH + Spacing;
        }

        // ── Separator ─────────────────────────────────────────────────────────
        {
            var go = CreateRect("Sep", transform, Padding, y, contentW, SepH);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;
            y -= SepH + Spacing;
        }

        // ── Arrow slots ───────────────────────────────────────────────────────
        // Each slot = outer container (positioned, no Image) +
        //             inner Image child (centred, ROTATED here without layout fight)
        _arrowImgs = new Image[arrowCount];
        float arrowX = (PanelWidth - arrowSize) * 0.5f;

        for (int i = 0; i < arrowCount; i++)
        {
            // Outer container — this is what gets manually positioned
            var slot = CreateRect($"Slot{i}", transform, arrowX, y, arrowSize, arrowSize);

            // Inner image — child centred inside slot, ROTATION applied here
            var inner = new GameObject($"Arrow{i}", typeof(RectTransform));
            inner.transform.SetParent(slot.transform, false);
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin        = new Vector2(0.5f, 0.5f);
            innerRt.anchorMax        = new Vector2(0.5f, 0.5f);
            innerRt.pivot            = new Vector2(0.5f, 0.5f);
            innerRt.anchoredPosition = Vector2.zero;
            innerRt.sizeDelta        = new Vector2(arrowSize, arrowSize);

            var img = inner.AddComponent<Image>();
            img.sprite         = _arrowSprite;
            img.color          = Color.white;
            img.preserveAspect = true;
            img.raycastTarget  = false;

            _arrowImgs[i] = img;
            y -= arrowSize + (i < arrowCount - 1 ? Spacing : 0f);
        }
    }

    // =========================================================================
    // Subscription & update
    // =========================================================================
    void TrySubscribe()
    {
        if (_subscribed) return;
        if (BulletDirectionQueue.Instance == null) return;

        BulletDirectionQueue.Instance.OnQueueChanged += RefreshArrows;
        _subscribed = true;

        if (_shooting == null) _shooting = FindAnyObjectByType<Shooting>();
        if (_shooting != null) _fireInterval = _shooting.fireInterval;

        _countdown = _fireInterval;
        RefreshArrows();
    }

    void TickTimer()
    {
        if (!_subscribed || _timerFill == null) return;
        _countdown -= Time.deltaTime;
        _timerFill.fillAmount = Mathf.Clamp01(_countdown / _fireInterval);
    }

    void RefreshArrows()
    {
        if (_arrowImgs == null) return;
        if (_shooting != null) _fireInterval = _shooting.fireInterval;
        _countdown = _fireInterval;

        if (BulletDirectionQueue.Instance == null) return;
        Vector2[] dirs = BulletDirectionQueue.Instance.GetPreview(_arrowImgs.Length);

        for (int i = 0; i < _arrowImgs.Length; i++)
        {
            if (_arrowImgs[i] == null) continue;

            if (i >= dirs.Length) { _arrowImgs[i].enabled = false; continue; }

            _arrowImgs[i].enabled = true;

            // Rotate the inner Image child — slot container is untouched
            _arrowImgs[i].rectTransform.localRotation = DirToRotation(dirs[i]);

            if (i == 0)
                _arrowImgs[i].color = ColNext;
            else if (i == 1)
                _arrowImgs[i].color = ColSecond;
            else
            {
                float t = (float)(i - 1) / Mathf.Max(1, _arrowImgs.Length - 2);
                _arrowImgs[i].color = Color.Lerp(ColSecond, ColFaded, t);
            }
        }
    }

    static Quaternion DirToRotation(Vector2 dir)
    {
        if (dir == Vector2.up)   return Quaternion.Euler(0f, 0f,   0f);
        if (dir == Vector2.down) return Quaternion.Euler(0f, 0f, 180f);
        if (dir == Vector2.left) return Quaternion.Euler(0f, 0f,  90f);
        /* right */              return Quaternion.Euler(0f, 0f, -90f);
    }

    // =========================================================================
    // RectTransform helpers
    // =========================================================================

    /// Creates a child with top-left anchoring at (x, y) from the parent's top-left.
    static GameObject CreateRect(string name, Transform parent,
                                 float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    /// Creates a child Image that stretches to fill its parent.
    static GameObject CreateStretchChild(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(0);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return go;
    }

    // =========================================================================
    // Procedural arrow texture (used when no texture is assigned)
    // =========================================================================
    static Sprite MakeProceduralArrowSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        int mid      = size / 2;
        int headBase = Mathf.RoundToInt(size * 0.40f);
        int stemHalf = Mathf.Max(2, size / 10);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (y >= headBase)
            {
                float t    = (float)(y - headBase) / Mathf.Max(1, size - 1 - headBase);
                int   half = Mathf.RoundToInt((1f - t) * (mid - 2));
                if (Mathf.Abs(x - mid) <= half) px[y * size + x] = Color.white;
            }
            else if (Mathf.Abs(x - mid) < stemHalf)
                px[y * size + x] = Color.white;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size),
                             new Vector2(0.5f, 0.5f), size);
    }
}
