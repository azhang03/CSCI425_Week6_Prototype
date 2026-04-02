using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 1;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
