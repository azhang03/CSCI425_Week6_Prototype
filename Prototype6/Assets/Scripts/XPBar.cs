using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Segmented XP bar with DMC-style punch+fill animation per segment.
//
// Usage (drop-in replacement for the old continuous bar):
//   1. Put this script on the GameObject that used to be your single yellow bar.
//   2. Drag the OLD continuous Image into "Legacy Fill Image" (it gets hidden).
//   3. Drag the OLD "x/y XP" text into "Legacy Label" (it gets hidden).
//   4. Press play — 5 segment Images will be generated automatically under
//      the rect, evenly spaced, with the fill colour you choose.
//   5. (Optional) tweak Tilt Degrees / Tilt Root for the slanted look.
//
// You can also wire your own segments by hand: assign Images to the
// "Segment Fills" array and turn off "Auto Generate".
public class XPBar : MonoBehaviour
{
    [Header("Segments (left \u2192 right)")]
    [Tooltip("Custom segment graphics. If empty and Auto Generate is on, these are created at runtime.")]
    public XPBarSegmentGraphic[] segmentFills;

    [Header("Auto Setup")]
    [Tooltip("If true and Segment Fills is empty, build the segments at runtime under Segments Container.")]
    public bool autoGenerate = true;
    [Tooltip("Where generated segments are parented. If null, this transform is used.")]
    public RectTransform segmentsContainer;
    [Tooltip("If true, every pre-existing child of the segments container (the old bar, the old text, etc.) is set inactive on Awake before generating the new segments.")]
    public bool hideExistingChildren = true;
    [Range(2, 50)]
    [Tooltip("Used as the segment count when Match Segments To Max XP is off, otherwise overwritten at runtime.")]
    public int segmentCount = 5;
    [Tooltip("If true, segment count tracks XPManager.XPToNextLevel — one block per XP point — and rebuilds when the cap changes.")]
    public bool matchSegmentsToMaxXP = true;
    [Tooltip("Pixels of empty space between adjacent segments.")]
    public float segmentSpacing = 4f;
    [Tooltip("Fill colour of each segment when full.")]
    public Color segmentFillColor = new Color(1f, 0.78f, 0.16f, 1f);
    [Tooltip("Empty/unlit colour of each segment.")]
    public Color segmentBackgroundColor = new Color(0.55f, 0.57f, 0.62f, 1f);
    [Tooltip("Optional sprite for both the background and fill (e.g. a rounded rect). Leave null for plain rectangles.")]
    public Sprite segmentSprite;

    [Header("Height Taper (left \u2192 right)")]
    [Tooltip("If true, segments form a continuous slanted top from leftSegmentHeight on the left to rightSegmentHeight on the right (smooth, not staircase).")]
    public bool taperHeights = true;
    [Tooltip("Pixel height of the leftmost edge of the bar (max).")]
    public float leftSegmentHeight = 28f;
    [Tooltip("Pixel height of the rightmost edge of the bar (min). Default keeps max ~1.4x of min.")]
    public float rightSegmentHeight = 20f;

    [Header("Legacy / Old UI to Hide (optional, also hidden by Hide Existing Children)")]
    public Image legacyFillImage;
    public TMP_Text legacyLabel;
    public GameObject[] additionalHideOnAwake;

    [Header("Tilt (applied on Awake)")]
    [Tooltip("Optional rect to rotate. Place hearts + bar under this rect to tilt them together. If null, this object is rotated.")]
    public RectTransform tiltRoot;
    [Tooltip("Z rotation in degrees. Positive = right side rotates up. 0 = level.")]
    public float tiltDegrees = 6f;

    [Header("Punch Animation")]
    public float punchScale = 1.45f;
    public float punchInTime = 0.06f;
    public float punchOutTime = 0.18f;
    public float fillTime = 0.14f;

    [Header("Flash")]
    public bool flashOnFill = true;
    public Color flashColor = Color.white;

    [Header("Shake")]
    [Tooltip("Tiny shake of the whole bar each time a segment is filled.")]
    public bool shakeOnFill = true;
    public float fillShakeMagnitude = 4f;
    public float fillShakeDuration  = 0.18f;
    [Tooltip("Bigger shake on level-up, when the bar shatters and resets.")]
    public float levelUpShakeMagnitude = 14f;
    public float levelUpShakeDuration  = 0.45f;

    [Header("Level-Up Shatter")]
    [Tooltip("Spawn particles outward from every segment when the player levels up.")]
    public bool shatterOnLevelUp = true;
    [Tooltip("How many particles to burst from each segment.")]
    public int   shatterParticlesPerSegment = 14;
    [Tooltip("Color used for empty segments' shatter chunks.")]
    public Color emptyShatterColor = new Color(0.7f, 0.72f, 0.78f, 1f);

    Coroutine[] runningAnims;
    Color[]     baseColors;
    float[]     currentFill;
    int         lastFilledSegments;
    int         lastXP = -1;
    bool        eventSubscribed;

    GameObject     generatedRoot;
    RectTransform  shakeTarget;
    Vector2        shakeBasePos;
    Coroutine      shakeCo;

    void Awake()
    {
        HideLegacy();

        // If matching the XP cap, defer the build until we know XPToNextLevel.
        // Otherwise build immediately with the inspector segmentCount.
        if (autoGenerate && (segmentFills == null || segmentFills.Length == 0)
            && !matchSegmentsToMaxXP)
        {
            BuildSegments();
        }

        InitSegmentState();
        ApplyTilt();
    }

    void HideLegacy()
    {
        // Hide everything currently sitting in the segments container so it
        // doesn't compete with the generated segments for layout space.
        if (hideExistingChildren)
        {
            var container = segmentsContainer != null
                ? segmentsContainer
                : transform as RectTransform;
            if (container != null)
            {
                for (int i = 0; i < container.childCount; i++)
                {
                    var child = container.GetChild(i);
                    if (child != null) child.gameObject.SetActive(false);
                }
            }
        }

        if (legacyFillImage != null) legacyFillImage.gameObject.SetActive(false);
        if (legacyLabel     != null) legacyLabel.gameObject.SetActive(false);
        if (additionalHideOnAwake != null)
            for (int i = 0; i < additionalHideOnAwake.Length; i++)
                if (additionalHideOnAwake[i] != null)
                    additionalHideOnAwake[i].SetActive(false);
    }

    void BuildSegments()
    {
        var container = segmentsContainer != null
            ? segmentsContainer
            : transform as RectTransform;
        if (container == null) return;

        // Tear down a previous build (e.g. on rebuild after XP cap change).
        if (generatedRoot != null)
        {
            if (runningAnims != null)
                for (int i = 0; i < runningAnims.Length; i++)
                    if (runningAnims[i] != null) StopCoroutine(runningAnims[i]);
            if (Application.isPlaying) Destroy(generatedRoot);
            else                       DestroyImmediate(generatedRoot);
            generatedRoot = null;
        }

        // Create a dedicated child rect that stretches to fill the bar and
        // hosts the HorizontalLayoutGroup. This way our segments never share
        // layout slots with anything else still parented to `container`.
        var rootGO = new GameObject("GeneratedSegments", typeof(RectTransform));
        generatedRoot = rootGO;
        var root   = (RectTransform)rootGO.transform;
        root.SetParent(container, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot     = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        shakeTarget  = root;
        shakeBasePos = root.anchoredPosition;
        if (shakeCo != null) { StopCoroutine(shakeCo); shakeCo = null; }

        var hlg = rootGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = segmentSpacing;
        // The custom XPBarSegmentGraphic draws its own slanted top inside its
        // RectTransform, so we always let HLG control width + height; the
        // segment rects are uniform-size, and the slope is in the mesh.
        hlg.childAlignment         = TextAnchor.LowerCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        float maxH = Mathf.Max(leftSegmentHeight, rightSegmentHeight);

        var generated = new List<XPBarSegmentGraphic>(segmentCount);
        for (int i = 0; i < segmentCount; i++)
        {
            var seg = new GameObject($"Segment_{i}", typeof(RectTransform));
            seg.transform.SetParent(root, false);

            // Per-segment trapezoid heights so adjacent segments share the
            // same slope (right edge of segment i == left edge of i+1).
            float tL = segmentCount <= 1 ? 0f : (float)i       / segmentCount;
            float tR = segmentCount <= 1 ? 1f : (float)(i + 1) / segmentCount;
            float hL = taperHeights ? Mathf.Lerp(leftSegmentHeight, rightSegmentHeight, tL) : maxH;
            float hR = taperHeights ? Mathf.Lerp(leftSegmentHeight, rightSegmentHeight, tR) : maxH;

            // The rect must be tall enough to contain the trapezoid; its
            // bottom is the bar's baseline so the wedge sits flush.
            var le = seg.AddComponent<LayoutElement>();
            le.preferredHeight = maxH;
            le.minHeight       = maxH;
            le.flexibleHeight  = 0f;

            var graphic = seg.AddComponent<XPBarSegmentGraphic>();
            graphic.BackgroundColor = segmentBackgroundColor;
            graphic.FillColor       = segmentFillColor;
            graphic.SetHeights(hL, hR);
            graphic.FillAmount      = 0f;
            graphic.raycastTarget   = false;

            generated.Add(graphic);
        }

        segmentFills = generated.ToArray();
    }

    void InitSegmentState()
    {
        int n = segmentFills != null ? segmentFills.Length : 0;
        runningAnims = new Coroutine[n];
        baseColors   = new Color[n];
        currentFill  = new float[n];

        for (int i = 0; i < n; i++)
        {
            var g = segmentFills[i];
            if (g == null) continue;
            g.FillAmount = 0f;
            baseColors[i] = g.FillColor;
            g.rectTransform.localScale = Vector3.one;
        }
    }

    void ApplyTilt()
    {
        var t = tiltRoot != null ? tiltRoot : transform as RectTransform;
        if (t != null)
            t.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees);
    }

    void OnEnable()  { Subscribe(); }
    void OnDisable() { Unsubscribe(); }

    void Start()
    {
        Subscribe();
        if (XPManager.Instance != null)
            UpdateBar(XPManager.Instance.CurrentXP, XPManager.Instance.XPToNextLevel);
        SetVisible(PauseMenu.AugmentsEnabled);
    }

    void Subscribe()
    {
        if (eventSubscribed || XPManager.Instance == null) return;
        XPManager.Instance.OnXPChanged += UpdateBar;
        eventSubscribed = true;
    }

    void Unsubscribe()
    {
        if (!eventSubscribed || XPManager.Instance == null) return;
        XPManager.Instance.OnXPChanged -= UpdateBar;
        eventSubscribed = false;
    }

    void Update()
    {
        if (!eventSubscribed) Subscribe();

        bool show = PauseMenu.AugmentsEnabled;
        if (segmentFills != null && segmentFills.Length > 0
            && segmentFills[0] != null
            && segmentFills[0].gameObject.activeSelf != show)
            SetVisible(show);
    }

    void SetVisible(bool visible)
    {
        if (segmentFills == null) return;
        for (int i = 0; i < segmentFills.Length; i++)
        {
            if (segmentFills[i] == null) continue;
            segmentFills[i].gameObject.SetActive(visible);
        }
    }

    void UpdateBar(int currentXP, int xpToNextLevel)
    {
        // Detect a level-up empty (XP wrapped down) BEFORE any teardown so
        // we can spawn the shatter particles from the still-full bar.
        bool isLevelUpReset = lastXP > 0 && currentXP < lastXP;

        if (isLevelUpReset)
            SpawnShatter();

        // (Re)build segments if the cap has changed and we're tracking it.
        if (matchSegmentsToMaxXP && autoGenerate)
        {
            int desired = Mathf.Max(2, xpToNextLevel);
            if (segmentFills == null || segmentFills.Length != desired)
            {
                segmentCount = desired;
                BuildSegments();
                InitSegmentState();
                lastFilledSegments = 0;
            }
        }

        if (segmentFills == null || segmentFills.Length == 0) return;

        int n = segmentFills.Length;
        float xpPerSeg = xpToNextLevel / (float)n;
        if (xpPerSeg <= 0f) xpPerSeg = 1f;

        if (isLevelUpReset)
        {
            for (int i = 0; i < n; i++) SnapSegment(i, 0f);
            lastFilledSegments = 0;
            PlayShake(levelUpShakeMagnitude, levelUpShakeDuration);
        }
        lastXP = currentXP;

        int filledSegs = Mathf.Min(n, Mathf.FloorToInt(currentXP / xpPerSeg));
        float partial  = (filledSegs < n)
            ? Mathf.Clamp01((currentXP - filledSegs * xpPerSeg) / xpPerSeg)
            : 1f;

        for (int i = lastFilledSegments; i < filledSegs; i++)
            PlayPunchFill(i, 1f);

        if (filledSegs < n && partial > currentFill[filledSegs] + 0.001f)
            PlayPunchFill(filledSegs, partial);

        for (int i = Mathf.Max(filledSegs + 1, lastFilledSegments + 1); i < n; i++)
            SnapSegment(i, 0f);

        lastFilledSegments = filledSegs;
    }

    void SnapSegment(int idx, float fill)
    {
        if (idx < 0 || idx >= segmentFills.Length) return;
        var g = segmentFills[idx];
        if (g == null) return;
        if (runningAnims[idx] != null)
        {
            StopCoroutine(runningAnims[idx]);
            runningAnims[idx] = null;
        }
        g.FillAmount = fill;
        g.rectTransform.localScale = Vector3.one;
        g.FillColor = baseColors[idx];
        currentFill[idx] = fill;
    }

    void PlayPunchFill(int idx, float target)
    {
        if (idx < 0 || idx >= segmentFills.Length) return;
        if (segmentFills[idx] == null) return;
        if (runningAnims[idx] != null) StopCoroutine(runningAnims[idx]);
        runningAnims[idx] = StartCoroutine(PunchFillCoroutine(idx, target));

        if (shakeOnFill)
            PlayShake(fillShakeMagnitude, fillShakeDuration);
    }

    public void PlayShake(float magnitude, float duration)
    {
        if (shakeTarget == null || duration <= 0f) return;
        if (shakeCo != null) StopCoroutine(shakeCo);
        shakeCo = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            Vector2 off = Random.insideUnitCircle * (magnitude * k);
            if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBasePos + off;
            yield return null;
        }
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBasePos;
        shakeCo = null;
    }

    void SpawnShatter()
    {
        if (!shatterOnLevelUp) return;
        if (segmentFills == null || segmentFills.Length == 0) return;

        Canvas canvas = null;
        for (int i = 0; i < segmentFills.Length && canvas == null; i++)
            if (segmentFills[i] != null) canvas = segmentFills[i].canvas;
        if (canvas == null) return;

        for (int i = 0; i < segmentFills.Length; i++)
        {
            var g = segmentFills[i];
            if (g == null) continue;
            var segRT = (RectTransform)g.transform;

            Vector3 worldCenter = segRT.TransformPoint(segRT.rect.center);

            Color color = g.FillAmount > 0.05f
                ? segmentFillColor
                : emptyShatterColor;

            float visualHeight = Mathf.Max(g.LeftHeight, g.RightHeight);
            float refSize = Mathf.Max(visualHeight * 0.9f, segRT.rect.width * 0.45f);

            HeartParticles.Spawn(canvas, worldCenter, color, refSize, shatterParticlesPerSegment);
        }
    }

    IEnumerator PunchFillCoroutine(int idx, float target)
    {
        var g  = segmentFills[idx];
        var rt = g.rectTransform;

        float startFill = g.FillAmount;
        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = Vector3.one * punchScale;

        float t = 0f;
        while (t < punchInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchInTime);
            k = 1f - (1f - k) * (1f - k);
            rt.localScale = Vector3.LerpUnclamped(baseScale, peakScale, k);
            if (flashOnFill) g.FillColor = Color.Lerp(baseColors[idx], flashColor, k);
            yield return null;
        }
        rt.localScale = peakScale;

        float duration = Mathf.Max(punchOutTime, fillTime);
        t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float kFill  = fillTime > 0f     ? Mathf.Clamp01(t / fillTime)     : 1f;
            float kScale = punchOutTime > 0f ? Mathf.Clamp01(t / punchOutTime) : 1f;
            float ef = 1f - Mathf.Pow(1f - kFill, 3f);
            float es = 1f - Mathf.Pow(1f - kScale, 2f);
            g.FillAmount  = Mathf.Lerp(startFill, target, ef);
            rt.localScale = Vector3.LerpUnclamped(peakScale, baseScale, es);
            if (flashOnFill) g.FillColor = Color.Lerp(flashColor, baseColors[idx], es);
            yield return null;
        }

        g.FillAmount      = target;
        rt.localScale     = baseScale;
        g.FillColor       = baseColors[idx];
        currentFill[idx]  = target;
        runningAnims[idx] = null;
    }
}
