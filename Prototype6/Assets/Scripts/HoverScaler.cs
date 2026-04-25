using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverMultiplier = 1.1f;
    public float animationDuration = 0.2f;
    public AnimationCurve easingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private float baseScale = 1f;
    private bool isHovered;
    private Coroutine activeTween;

    void OnEnable()
    {
        CheckInitialHover();
    }

    void Start()
    {
        CheckInitialHover();
    }

    void CheckInitialHover()
    {
        if (EventSystem.current == null) return;
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = mousePos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        bool over = false;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject == gameObject ||
                results[i].gameObject.transform.IsChildOf(transform))
            {
                over = true;
                break;
            }
        }

        if (over && !isHovered)
        {
            isHovered = true;
            StartTween();
        }
    }

    public void SetBaseScale(float scale)
    {
        baseScale = scale;
        if (activeTween == null)
        {
            float s = isHovered ? baseScale * hoverMultiplier : baseScale;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        StartTween();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        StartTween();
    }

    void OnDisable()
    {
        if (activeTween != null)
        {
            StopCoroutine(activeTween);
            activeTween = null;
        }
        isHovered = false;
        float s = baseScale;
        transform.localScale = new Vector3(s, s, 1f);
    }

    void StartTween()
    {
        if (!gameObject.activeInHierarchy) return;
        if (activeTween != null) StopCoroutine(activeTween);
        activeTween = StartCoroutine(TweenScale());
    }

    System.Collections.IEnumerator TweenScale()
    {
        float targetScale = isHovered ? baseScale * hoverMultiplier : baseScale;
        float startScale = transform.localScale.x;

        if (animationDuration <= 0f)
        {
            transform.localScale = new Vector3(targetScale, targetScale, 1f);
            activeTween = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float eased = easingCurve.Evaluate(t);
            float s = Mathf.LerpUnclamped(startScale, targetScale, eased);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        transform.localScale = new Vector3(targetScale, targetScale, 1f);
        activeTween = null;
    }
}
