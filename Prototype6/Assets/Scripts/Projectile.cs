using UnityEngine;

public class A_Projectile : MonoBehaviour
{
    [HideInInspector]
    public int damage = 1;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
