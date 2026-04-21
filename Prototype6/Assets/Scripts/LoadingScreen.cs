using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("Timing")]
    public float fadeDuration        = 0.5f;
    public float blackScreenDuration = 1.0f;

    [Header("Animation")]
    public Sprite[] frames;
    public float    animationFPS  = 8f;
    public float    animationSize = 150f;

    private CanvasGroup canvasGroup;
    private Image       animationImage;
    private bool        transitioning;
    private bool        animationRunning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
    }

    // ── Public entry point ────────────────────────────────────────────────

    public void Transition(string sceneName)
    {
        if (transitioning) return;
        transitioning = true;
        StartCoroutine(DoTransition(sceneName));
    }

    // ── Core coroutine ────────────────────────────────────────────────────

    IEnumerator DoTransition(string sceneName)
    {
        // Pause menu may have set timeScale = 0; reset so unscaled coroutines work cleanly
        Time.timeScale = 1f;

        //LobbyAudioManager.Instance.PlayLoadLevel();
        //Not really long enough for this audio

        // Show canvas (starts transparent) and begin animation
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        animationImage.gameObject.SetActive(true);
        StartCoroutine(AnimateFrames());

        // Phase 1: Fade to black — animation plays throughout
        yield return Fade(0f, 1f);

        // Phase 2: Start async load; hold activation until timing conditions met
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < blackScreenDuration || load.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Phase 3: Allow scene to activate while animation is still covering the screen.
        // Wait for isDone so Unity finishes running Awake/Start on the new scene before
        // we reveal anything — this prevents the brief dead-black gap between animation
        // hiding and the new scene being ready to render.
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;

        // Scene is fully initialized and rendering behind the canvas — now hide the
        // animation and fade out simultaneously.
        animationRunning = false;
        animationImage.gameObject.SetActive(false);
        yield return Fade(1f, 0f);

        canvasGroup.gameObject.SetActive(false);
        animationImage.gameObject.SetActive(true); // restore for next transition
        transitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    IEnumerator AnimateFrames()
    {
        if (frames == null || frames.Length == 0) yield break;
        animationRunning = true;
        int   index    = 0;
        float interval = 1f / Mathf.Max(animationFPS, 0.01f);
        while (animationRunning)
        {
            animationImage.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    // ── Canvas built in code (no prefab needed) ───────────────────────────

    void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("LoadingCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas        = canvasObj.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 100; // always on top of everything
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup                  = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts   = true;
        canvasGroup.interactable     = false;

        // Full-screen black background
        Image bg           = canvasObj.AddComponent<Image>();
        bg.color           = Color.black;
        RectTransform bgRt = canvasObj.GetComponent<RectTransform>();
        bgRt.anchorMin     = Vector2.zero;
        bgRt.anchorMax     = Vector2.one;
        bgRt.offsetMin     = bgRt.offsetMax = Vector2.zero;

        // Centred animation icon
        GameObject    animObj = new GameObject("AnimIcon");
        animObj.transform.SetParent(canvasObj.transform, false);
        RectTransform animRt  = animObj.AddComponent<RectTransform>();
        animRt.anchorMin      = animRt.anchorMax = new Vector2(0.5f, 0.5f);
        animRt.pivot          = new Vector2(0.5f, 0.5f);
        animRt.sizeDelta      = new Vector2(animationSize, animationSize);

        animationImage                = animObj.AddComponent<Image>();
        animationImage.preserveAspect = true;
        if (frames != null && frames.Length > 0)
            animationImage.sprite = frames[0];

        canvasObj.SetActive(false);
    }
}
