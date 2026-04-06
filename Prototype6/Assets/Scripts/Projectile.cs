using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 1;
    public float lifetime = 5f;

    private string obstacleTag = "Obstacle";


    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("test collision");
        if (other.gameObject.CompareTag(obstacleTag))
        {
            Destroy(gameObject);

            // add sound effect 

        }

    }
}
