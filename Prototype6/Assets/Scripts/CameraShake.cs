using System.Collections;
using UnityEngine;

// Self-bootstrapping camera shake. Call CameraShake.Shake(duration, magnitude) from anywhere;
// on first use it attaches itself to Camera.main and caches that camera's baseline localPosition.
// Subsequent calls restart the shake coroutine so rapid hits don't stack or drift the baseline.
public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    private Vector3 baselineLocalPos;
    private Coroutine shakeRoutine;

    public static void Shake(float duration, float magnitude)
    {
        if (instance == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            instance = cam.gameObject.AddComponent<CameraShake>();
            instance.baselineLocalPos = cam.transform.localPosition;
        }

        if (instance.shakeRoutine != null)
            instance.StopCoroutine(instance.shakeRoutine);

        instance.shakeRoutine = instance.StartCoroutine(instance.ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float remaining = 1f - (elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * (magnitude * remaining);
            transform.localPosition = baselineLocalPos + (Vector3)offset;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = baselineLocalPos;
        shakeRoutine = null;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
