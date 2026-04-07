using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStage", menuName = "Game/StageData")]
public class StageData : ScriptableObject
{
    [Header("Identity")]
    public string stageName;
    public int stageNumber;

    [Header("Wave Structure")]
    public float[] spawnIntervals;
    public float[] enemiesPerWave;

    public int waveCount => spawnIntervals != null ? spawnIntervals.Length : 0;

    [Header("Enemy Roster")]
    public List<EnemySpawner.EnemyType> enemyRoster = new List<EnemySpawner.EnemyType>();

    [Header("Difficulty")]
    public int enemyHPBonus = 0;
    public int bonusHPPerCycle = 2;
    public int bonusEnemiesPerWave = 2;
    public float spawnRandomness = 0.5f;
}
