using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that pre-generates upcoming bullet directions.
/// Shooting.cs calls Dequeue() for each projectile direction so that
/// BulletDirectionUI can show what's coming next.
/// </summary>
public class BulletDirectionQueue : MonoBehaviour
{
    public static BulletDirectionQueue Instance { get; private set; }

    // How many directions to keep buffered ahead of the current shot
    private const int BufferSize = 12;

    private readonly Queue<Vector2> _queue = new Queue<Vector2>();

    // Fired whenever a direction is consumed so the UI can refresh
    public event System.Action OnQueueChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Refill();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by Shooting.cs instead of its own random roll.
    /// Pops the front direction, refills the buffer, and notifies UI.
    /// </summary>
    public Vector2 Dequeue()
    {
        if (_queue.Count == 0)
            Refill();

        Vector2 dir = _queue.Dequeue();
        Refill();
        OnQueueChanged?.Invoke();
        return dir;
    }

    /// <summary>
    /// Returns the next <paramref name="count"/> upcoming directions (peek, not consume).
    /// </summary>
    public Vector2[] GetPreview(int count = 6)
    {
        Vector2[] all = _queue.ToArray();
        int len = Mathf.Min(count, all.Length);
        Vector2[] result = new Vector2[len];
        System.Array.Copy(all, result, len);
        return result;
    }

    // -------------------------------------------------------------------------

    void Refill()
    {
        while (_queue.Count < BufferSize)
            _queue.Enqueue(RandomCardinal());
    }

    static Vector2 RandomCardinal()
    {
        switch (Random.Range(0, 4))
        {
            case 0:  return Vector2.up;
            case 1:  return Vector2.down;
            case 2:  return Vector2.left;
            default: return Vector2.right;
        }
    }
}
