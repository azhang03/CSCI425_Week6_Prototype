using UnityEngine;
using System.Collections;


public class S_TeleportMove : MonoBehaviour
{
    [Header("Teleport Settings")]
    public float teleportInterval = 2f;
    public float shrinkFactor = 0.75f;           // How much closer each teleport gets (0.5–0.9 works well)

    private Transform target;
    private float timer;


    [Header("Fade Settings")]
    public float fadeDuration = 0.4f;
    private SpriteRenderer spriteRenderer;
    private bool isTeleporting = false;

    public S_AudioManager audioManager;


    void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();

        audioManager = Object.FindAnyObjectByType<S_AudioManager>(); 


    }

    void Update()
    {

        if (target == null || isTeleporting) return;

        timer += Time.deltaTime;

        if (timer >= teleportInterval)
        {
            S_AudioManager.Instance?.PlayEnemyTp();
            StartCoroutine(TeleportRoutine());
            timer = 0f;
        }
    }





    IEnumerator TeleportRoutine()
    {
        isTeleporting = true;

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));

        // Teleport
        TeleportCloser();

        // Fade back in
        yield return StartCoroutine(Fade(0f, 1f));

        isTeleporting = false;
    }

   

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        spriteRenderer.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    void TeleportCloser()
    {
        Vector2 playerPos = target.position;
        Vector2 currentPos = transform.position;

        float currentDistance = Vector2.Distance(currentPos, playerPos);

        // Calculate new maximum radius (closer than before)
        float newRadius = currentDistance * shrinkFactor;


        // Pick random direction
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // Pick random distance within allowed radius

        Vector2 newPosition = playerPos + randomDirection * newRadius;

        transform.position = newPosition;
    }
}