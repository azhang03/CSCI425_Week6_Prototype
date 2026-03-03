using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class A_EnemySpawner : MonoBehaviour
{
    public static A_EnemySpawner Instance { get; private set; }

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
    public int cycleCount = 0;
    public int bonusHPPerCycle = 2;
    public int bonusEnemiesPerStage = 2;

    private Dictionary<int, int> cycleKillCounts = new Dictionary<int, int>();
    public event System.Action OnCycleProgressChanged;

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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

            cycleKillCounts[0] = 0;

            foreach (var enemy in enemyTypes)
            {
                totalWeight += enemy.spawnWeight;
            }
        }
    }

    public void RegisterKill(int cycle)
    {
        if (!cycleKillCounts.ContainsKey(cycle))
            cycleKillCounts[cycle] = 0;
        cycleKillCounts[cycle]++;
        OnCycleProgressChanged?.Invoke();
    }

    public int GetCycleKills(int cycle)
    {
        return cycleKillCounts.ContainsKey(cycle) ? cycleKillCounts[cycle] : 0;
    }

    public int GetTotalEnemiesInCycle(int cycle)
    {
        int total = 0;
        for (int i = 0; i < numStages; i++)
            total += (int)enemiesPerStage[i] + cycle * bonusEnemiesPerStage;
        return total;
    }

    public Dictionary<int, int> GetCycleKillCounts()
    {
        return cycleKillCounts;
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

    int GetEffectiveEnemiesForStage(int stage)
    {
        return (int)enemiesPerStage[stage] + cycleCount * bonusEnemiesPerStage;
    }

    void UpdateStage()
    {
        if (stageCounter < numStages && spawnCounter >= GetEffectiveEnemiesForStage(stageCounter))
        {
            spawnCounter = 0;
            stageCounter++;

            if (stageCounter >= numStages)
            {
                cycleCount++;
                stageCounter = 0;
                spawnCounter = 0;
                spawnInterval = stageIntervals[0];
                cycleKillCounts[cycleCount] = 0;
                OnCycleProgressChanged?.Invoke();
                Debug.Log("Cycle " + cycleCount + " — enemies now have +" + (cycleCount * bonusHPPerCycle) + " HP, worth " + (1 + cycleCount * 2) + " pts");
            }
            else
            {
                spawnInterval = stageIntervals[stageCounter];
            }
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

            A_Enemy aEnemy = enemy.GetComponent<A_Enemy>();
            if (aEnemy != null)
            {
                aEnemy.maxHitPoints += cycleCount * bonusHPPerCycle;
                aEnemy.scoreValue = 1 + cycleCount * 2;
                aEnemy.spawnCycle = cycleCount;
            }
            else
            {
                S_Enemy sEnemy = enemy.GetComponent<S_Enemy>();
                if (sEnemy != null)
                    sEnemy.maxHitPoints += cycleCount * bonusHPPerCycle;
            }

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
