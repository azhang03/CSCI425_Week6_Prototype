using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth  = camHeight * Camera.main.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;
        transform.localScale = new Vector3(camWidth / spriteSize.x, camHeight / spriteSize.y, 1f);

        Vector3 cam = Camera.main.transform.position;
        transform.position = new Vector3(cam.x, cam.y, 0f);
    }
}
