using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public AudioManager audioManager;


    [Header("Health Settings")]
    public int maxHearts = 3;

    [Header("Invincibility")]
    public float iFrameDuration = 1f;
    public float flashInterval = 0.1f;

    public int CurrentHearts { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    private bool isInvincible;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CurrentHearts = maxHearts;
        spriteRenderer = GetComponent<SpriteRenderer>();
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsDead || isInvincible) return;


        Enemy sEnemy = collision.GetComponent<Enemy>();
        if (sEnemy != null)
        {
            Destroy(collision.gameObject);
            TakeDamage(1);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || isInvincible) return;

        audioManager.PlayPlayerHurt();

        CurrentHearts = Mathf.Max(0, CurrentHearts - amount);
        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);

        if (CurrentHearts <= 0)
        {
            audioManager.PlayPlayerDie();
            Die();
            return;
        }

        StartCoroutine(IFrames());
    }

    IEnumerator IFrames()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < iFrameDuration)
        {
            visible = !visible;
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        isInvincible = false;
    }

    public void AddMaxHearts(int amount)
    {
        maxHearts += amount;
        if (amount > 0)
            CurrentHearts = Mathf.Min(CurrentHearts + amount, maxHearts);
        else
            CurrentHearts = Mathf.Min(CurrentHearts, maxHearts);

        if (maxHearts < 1) maxHearts = 1;
        if (CurrentHearts < 1 && !IsDead)
        {
            CurrentHearts = 0;
            OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
            Die();
            return;
        }

        OnHealthChanged?.Invoke(CurrentHearts, maxHearts);
    }

    void Die()
    {
        IsDead = true;
        OnPlayerDied?.Invoke();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
