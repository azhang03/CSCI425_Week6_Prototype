using System.Collections;
using UnityEngine;

// Arcade-style "scale pop" feedback. Whenever Punch() is called the
// transform scales up sharply then eases back to its original scale.
// Designed to be slapped on the Player's transform (or any visual root)
// and triggered by Shooting whenever a weapon actually fires.
public class ShootPunch : MonoBehaviour
{
    [Header("Punch Shape")]
    [Tooltip("Multiplier applied to base scale at the peak of the punch.")]
    public float punchScale = 1.35f;

    [Tooltip("Time (seconds) to scale up to the peak.")]
    public float scaleUpTime = 0.05f;

    [Tooltip("Time (seconds) to ease back down to base scale.")]
    public float scaleDownTime = 0.18f;

    [Tooltip("If true, a new Punch() while one is active restarts the effect from the current scale.")]
    public bool retriggerable = true;

    private Vector3 baseScale;
    private Coroutine punchRoutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Punch()
    {
        Punch(punchScale);
    }

    public void Punch(float overrideScale)
    {
        if (!isActiveAndEnabled)
            return;

        if (punchRoutine != null)
        {
            if (!retriggerable)
                return;

            StopCoroutine(punchRoutine);
        }

        punchRoutine = StartCoroutine(PunchRoutine(overrideScale));
    }

    IEnumerator PunchRoutine(float peakMultiplier)
    {
        Vector3 startScale = transform.localScale;
        Vector3 peakScale  = baseScale * peakMultiplier;

        float t = 0f;
        float up = Mathf.Max(0.0001f, scaleUpTime);
        while (t < up)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / up);
            transform.localScale = Vector3.LerpUnclamped(startScale, peakScale, k);
            yield return null;
        }
        transform.localScale = peakScale;

        t = 0f;
        float down = Mathf.Max(0.0001f, scaleDownTime);
        while (t < down)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / down);
            transform.localScale = Vector3.LerpUnclamped(peakScale, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        punchRoutine = null;
    }

    void OnDisable()
    {
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
            punchRoutine = null;
        }
        transform.localScale = baseScale;
    }
}
