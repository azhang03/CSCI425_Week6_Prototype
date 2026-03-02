using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 3;

    private int currentHearts;
    private bool isDead = false;

    public string enemyTag = "Enemy";

    public bool actuallyDie = false; //for testing

    void Start()
    {
        currentHearts = maxHearts;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag(enemyTag))
        {
            Destroy(collision.gameObject);
            TakeDamage(1);
        }
    }

    void TakeDamage(int amount)
    {
        currentHearts -= amount;

        Debug.Log("Player Hearts: " + currentHearts);

        if (currentHearts <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("Player Died");

        if (actuallyDie)
        {
            Destroy(this);
        }

    }
}