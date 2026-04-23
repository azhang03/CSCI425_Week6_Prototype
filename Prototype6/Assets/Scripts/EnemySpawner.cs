using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public AudioManager audioManager;

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
    public float[] enemySpeedMultipliers = { 1f, 1f, 1f, 1f };

    public int stageCounter = 0;
    public int spawnCounter = 0;
    public int cycleCount = 0;
    public float bonusHPPerWave = 0f;
    public int bonusEnemiesPerStage = 2;

    public int baseHPBonus = 0;
    public bool finiteMode = false;
    public bool IsComplete { get; private set; }
    public int activeEnemyCount { get; private set; }
    private bool doneSpawning;
    public event System.Action OnAllWavesComplete;
    public event System.Action OnEnemyKilled;

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
            spawnTimer = spawnInterval;

            totalWeight = 0f;
            foreach (var enemy in enemyTypes)
            {
                totalWeight += enemy.spawnWeight;
            }
        }
    }

    public void RegisterEnemyDeath()
    {
        activeEnemyCount--;
        OnEnemyKilled?.Invoke();
        if (finiteMode && doneSpawning && activeEnemyCount <= 0)
        {
            IsComplete = true;
            OnAllWavesComplete?.Invoke();
        }
    }

    public void Configure(StageData data)
    {
        numStages = data.waveCount;
        stageIntervals = data.spawnIntervals;
        enemiesPerStage = data.enemiesPerWave;
        enemySpeedMultipliers = data.enemySpeedMultipliers;
        enemyTypes = new List<EnemyType>(data.enemyRoster);
        bonusHPPerWave = data.bonusHPPerWave;
        bonusEnemiesPerStage = data.bonusEnemiesPerWave;
        spawnRandmoness = data.spawnRandomness;
        baseHPBonus = data.enemyHPBonus;
        finiteMode = true;
        IsComplete = false;
        doneSpawning = false;
        activeEnemyCount = 0;

        totalWeight = 0f;
        foreach (var enemy in enemyTypes)
            totalWeight += enemy.spawnWeight;

        stageCounter = 0;
        spawnCounter = 0;
        cycleCount = 0;

        if (stageIntervals != null && stageIntervals.Length > 0)
        {
            spawnInterval = stageIntervals[0];
            // First enemy of a stage arrives after at most 1s so the player isn't staring at
            // an empty arena. If wave 1's interval is already shorter, keep that.
            spawnTimer = Mathf.Min(1f, spawnInterval);
        }
    }

    void Update()
    {
        if (IsComplete || doneSpawning) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = Random.Range(spawnInterval - spawnRandmoness, spawnInterval + spawnRandmoness);
            spawnCounter++;
            UpdateStage();
        }
    }

    int GetEffectiveEnemiesForStage(int stage)
    {
        return (int)enemiesPerStage[stage] + cycleCount * bonusEnemiesPerStage;
    }

    float GetSpeedMultiplierForStage(int stage)
    {
        if (enemySpeedMultipliers == null || stage < 0 || stage >= enemySpeedMultipliers.Length)
            return 1f;

        return enemySpeedMultipliers[stage];
    }

    void UpdateStage()
    {
        if (stageCounter < numStages && spawnCounter >= GetEffectiveEnemiesForStage(stageCounter))
        {
            spawnCounter = 0;
            stageCounter++;

            if (stageCounter >= numStages)
            {
                if (finiteMode)
                {
                    doneSpawning = true;
                    if (activeEnemyCount <= 0)
                    {
                        IsComplete = true;
                        OnAllWavesComplete?.Invoke();
                    }
                    return;
                }

                cycleCount++;
                stageCounter = 0;
                spawnCounter = 0;
                spawnInterval = stageIntervals[0];
            }
            else
            {
                spawnInterval = stageIntervals[stageCounter];
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyTypes.Count <= 0)
            return;

        GameObject enemyPrefab = GetRandomEnemyByWeight();
        if (enemyPrefab == null || stageTilemap == null)
            return;

        Vector3 center = stageTilemap.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnPos = center + new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            Mathf.Sin(angle) * spawnRadius,
            0f
        );

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemyCount++;

        float speedMultiplier = GetSpeedMultiplierForStage(stageCounter);

        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.maxHitPoints += baseHPBonus + Mathf.FloorToInt(stageCounter * bonusHPPerWave);

           enemyComponent.SetSpeedMultiplier(speedMultiplier);
        }

        if (rotateParent != null)
            enemy.transform.SetParent(rotateParent);

        if (audioManager != null)
            audioManager.PlayEnemySpawn();
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
                return enemy.prefab;
            }
        }

        return null;
    }
}


