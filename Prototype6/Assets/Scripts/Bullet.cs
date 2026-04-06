using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 5f;

    //private string obstacleTag = "Obstacle";
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        if (collision.gameObject.CompareTag(obstacleTag))
//        {
           
//                Destroy(gameObject); 
//        }
//    }
}