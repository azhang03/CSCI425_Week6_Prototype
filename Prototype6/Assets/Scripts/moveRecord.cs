using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class MoveOnInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Optional (auto-found if null)")]
    [SerializeField] private StageSelectUI owner;
    [SerializeField] private RectTransform moveTarget;

    [Header("Motion")]
    public float moveAmount = 60f;
    public float duration = 0.2f;

    [Header("Behavior")]
    [SerializeField] private bool requireCenterStageUnlocked = true;
    [SerializeField] private bool clickInvokesPlay = true;
    [SerializeField] private float clickInvokeDelay = 0.05f;

    private RectTransform rectTransform;
    private Vector2 originalPos;

    private bool isHovered = false;
    private bool isClicked = false;
    private Coroutine moveRoutine;
    private Coroutine clickRoutine;

    void Awake()
    {
        rectTransform = moveTarget != null ? moveTarget : GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;
    }

    void OnEnable()
    {
        ResetState();
    }

    void OnDisable()
    {
        ResetState();
    }

    void Update()
    {
        // If the center stage becomes locked while we're hovered (e.g. navigation happens under the cursor),
        // snap back to the original position and stop accepting input.
        if ((isHovered || isClicked) && !CanInteract())
            ResetState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            ResetState();
            return;
        }

        isHovered = true;
        isClicked = false;
        UpdatePosition();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isClicked = false;
        UpdatePosition();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            ResetState();
            return;
        }

        isClicked = true;
        UpdatePosition();

        if (clickInvokesPlay)
        {
            if (clickRoutine != null) StopCoroutine(clickRoutine);
            clickRoutine = StartCoroutine(InvokePlayAfterDelay());
        }
    }

    void UpdatePosition()
    {
        float offset = 0f;

        if (isHovered) offset += moveAmount;
        if (isClicked) offset += moveAmount;

        Vector2 target = originalPos + new Vector2(offset, 0);

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(SmoothMove(target));
    }

    private void ResetState()
    {
        isHovered = false;
        isClicked = false;

        if (clickRoutine != null) { StopCoroutine(clickRoutine); clickRoutine = null; }
        if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }

        if (rectTransform == null)
            rectTransform = moveTarget != null ? moveTarget : GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.anchoredPosition = originalPos;
    }

    private bool CanInteract()
    {
        if (!requireCenterStageUnlocked) return true;

        if (owner == null) owner = FindAnyObjectByType<StageSelectUI>();
        if (owner == null) return false;

        // In this lobby, StageSelectUI hides the Play button when the center stage is locked.
        if (owner.playButton != null)
            return owner.playButton.gameObject.activeInHierarchy;

        // Fallback if Play button isn't wired for some reason.
        if (owner.centerLockOverlay != null)
            return !owner.centerLockOverlay.enabled;

        return true;
    }

    private IEnumerator InvokePlayAfterDelay()
    {
        if (owner == null) owner = FindAnyObjectByType<StageSelectUI>();
        if (owner == null || owner.playButton == null) yield break;

        // Let the slide animation visibly "commit" before changing scenes.
        if (clickInvokeDelay > 0f)
            yield return new WaitForSeconds(clickInvokeDelay);

        if (owner.playButton.gameObject.activeInHierarchy && owner.playButton.interactable)
            owner.playButton.onClick.Invoke();
    }

    IEnumerator SmoothMove(Vector2 target)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        rectTransform.anchoredPosition = target;
    }
}