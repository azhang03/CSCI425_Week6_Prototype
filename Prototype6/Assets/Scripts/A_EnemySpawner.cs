using UnityEngine;
using UnityEngine.Tilemaps;

public class A_EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;
    public Tilemap stageTilemap;
    public Transform rotateParent;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;

    private float spawnTimer;
    private float spawnRadius;

    void Start()
    {
        if (stageTilemap != null)
        {
            BoundsInt cellBounds = stageTilemap.cellBounds;
            Vector3 min = stageTilemap.CellToWorld(cellBounds.min);
            Vector3 max = stageTilemap.CellToWorld(cellBounds.max);
            float halfWidth = (max.x - min.x) * 0.5f;
            float halfHeight = (max.y - min.y) * 0.5f;
            spawnRadius = Mathf.Min(halfWidth, halfHeight);
        }
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || stageTilemap == null) return;

        Vector3 center = stageTilemap.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnPos = center + new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            Mathf.Sin(angle) * spawnRadius,
            0f
        );

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        if (rotateParent != null)
            enemy.transform.SetParent(rotateParent);
    }
}
