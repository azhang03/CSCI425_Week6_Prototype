using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class S_DefaultMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Transform target;
    public float moveSpeed = 2f;


    void Start()
    {

        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;

    }

    // Update is called once per frame
    void Update()
    {

        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);

    }
}
