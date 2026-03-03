using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class A_EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public Tilemap stageTilemap;
    public Transform rotateParent;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;

    private float spawnTimer;
    private float spawnRadius;

    public int numStages = 4;
    public float spawnRandmoness = 0.5f;
    public float[] stageIntervals = { 5, 3, 2, 1 };
    public float[] enemiesPerStage = { 2, 5, 8, 15 };

    public int stageCounter = 0;
    public int spawnCounter = 0;


    [System.Serializable]
    public class EnemyType
    {
        public string name;
        public GameObject prefab;
        public float spawnWeight = 1f;
    }

    [Header("Enemy Types")]
    public List<EnemyType> enemyTypes = new List<EnemyType>();
    public float totalWeight = 0;


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

            stageCounter = 0;
            spawnCounter = 0;
            spawnInterval = stageIntervals[stageCounter];


            foreach (var enemy in enemyTypes)
            {
                totalWeight += enemy.spawnWeight;
            }

        }
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = Random.Range(spawnInterval - spawnRandmoness, spawnInterval - spawnRandmoness); 
            spawnCounter++;
            UpdateStage();
        }
    }


    void UpdateStage()
    {
        if(stageCounter < numStages && spawnCounter >= enemiesPerStage[stageCounter]) 
        {
            spawnCounter = 0;
            stageCounter++;



            if (stageCounter >= numStages)
            {
                Debug.Log("game over or infinite run");
            }
            else
            {
                spawnInterval = stageIntervals[stageCounter];

            }

            //Debug.Log("Change stage: " + stageCounter + " interval:" + spawnInterval);

        }

    }

    void SpawnEnemy()
    {
        if (enemyTypes.Count > 0)
        {
            GameObject enemyPrefab = GetRandomEnemyByWeight();
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


    GameObject GetRandomEnemyByWeight()
    {

        
        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var enemy in enemyTypes)
        {
            cumulativeWeight += enemy.spawnWeight;

            if (randomValue <= cumulativeWeight)
            {
                Debug.Log("Enemy anem :" + enemy.name);
                return enemy.prefab;
            }
        }

        return null;
    }

}
