using UnityEngine;

public class DefaultMove : MonoBehaviour, ISpeedMultiplierReceiver
{
    private Transform target;

    public float moveSpeed = 2f;
    private float baseMoveSpeed;

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        moveSpeed = baseMoveSpeed * multiplier;
        //Debug.Log("Speed: " + moveSpeed);
    }

    void Update()
    {
        if (target == null) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }
}



