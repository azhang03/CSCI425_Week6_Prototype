using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Self-bootstrapping red-edge damage vignette. Call DamageVignette.Flash(duration) from anywhere;
// on first use it finds the active Canvas, creates a full-screen Image child with a runtime-
// generated radial gradient (transparent center, opaque red at edges), and fades alpha 0->peak->0.
public class DamageVignette : MonoBehaviour
{
    private static DamageVignette instance;

    public float peakAlpha = 0.6f;
    public Color edgeColor = new Color(1f, 0.1f, 0.1f, 1f);

    private Image image;
    private Coroutine flashRoutine;

    public static void Flash(float duration)
    {
        if (instance == null)
        {
            // Must attach to a screen-space canvas. FindAnyObjectByType<Canvas> is
            // nondeterministic and can return a WorldSpace canvas (e.g. a DamagePopup
            // spawned the same frame), which makes the vignette render as a tiny
            // red rectangle at the enemy instead of a full-screen overlay.
            Canvas canvas = null;
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                    c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null) return;

            GameObject holder = new GameObject("DamageVignette", typeof(RectTransform));
            holder.transform.SetParent(canvas.transform, false);

            RectTransform rt = holder.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            instance = holder.AddComponent<DamageVignette>();
            instance.image = holder.AddComponent<Image>();
            instance.image.raycastTarget = false;
            instance.image.sprite = BuildVignetteSprite(128);
            instance.image.color = new Color(instance.edgeColor.r, instance.edgeColor.g, instance.edgeColor.b, 0f);

            // Push to the back of siblings so it sits BEHIND other canvas children (AugmentPanel,
            // ResultsScreen, etc.) — they should always be able to overlay the vignette.
            rt.SetAsFirstSibling();
        }

        if (instance.flashRoutine != null)
            instance.StopCoroutine(instance.flashRoutine);

        instance.flashRoutine = instance.StartCoroutine(instance.FadeRoutine(duration));
    }

    IEnumerator FadeRoutine(float duration)
    {
        // Fast pop-in (~20%), slow fade-out (~80%). Reads as a hit-shock that lingers.
        float fadeInEnd = duration * 0.2f;
        float elapsed = 0f;
        Color c = image.color;

        while (elapsed < duration)
        {
            float a;
            if (elapsed < fadeInEnd)
                a = Mathf.Lerp(0f, peakAlpha, elapsed / fadeInEnd);
            else
                a = Mathf.Lerp(peakAlpha, 0f, (elapsed - fadeInEnd) / (duration - fadeInEnd));

            c.a = a;
            image.color = c;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        c.a = 0f;
        image.color = c;
        flashRoutine = null;
    }

    static Sprite BuildVignetteSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        float center = res * 0.5f;
        Color[] pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - center + 0.5f;
            float dy = y - center + 0.5f;
            float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / center);
            // Inverse of Projectile's glow — transparent center, opaque edge.
            float a = Mathf.SmoothStep(0f, 1f, t);
            pixels[y * res + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
