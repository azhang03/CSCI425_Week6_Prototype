using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner Instance { get; private set; }

    [Header("References")]
    public Transform rotateParent;

    [Header("Obstacle Settings")]
    public GameObject obstaclePrefab;

    [Header("Options")]
    public bool clearExistingOnSpawn = true;

    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Configure(StageData stageData)
    {
        if (stageData == null) return;
        SpawnObstacles(stageData.obstacleLayout);
    }

    public void SpawnObstacles(List<ObstaclePlacement> layout)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner: No obstacle prefab assigned.");
            return;
        }

        if (clearExistingOnSpawn)
            ClearObstacles();

        if (rotateParent == null)
            rotateParent = transform;

        if (layout == null)
            return;

        foreach (ObstaclePlacement data in layout)
        {
            GameObject obj = Instantiate(obstaclePrefab, rotateParent);
            obj.transform.localPosition = data.localPosition;
            obj.transform.localRotation = Quaternion.Euler(0f, 0f, data.localRotationZ);
            obj.transform.localScale = data.localScale;
            spawnedObstacles.Add(obj);
        }
    }

    public void ClearObstacles()
    {
        for (int i = spawnedObstacles.Count - 1; i >= 0; i--)
        {
            if (spawnedObstacles[i] != null)
                Destroy(spawnedObstacles[i]);
        }

        spawnedObstacles.Clear();
    }
}



