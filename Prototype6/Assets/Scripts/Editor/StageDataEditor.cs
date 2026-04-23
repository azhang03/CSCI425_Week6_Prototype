using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty stageName = serializedObject.FindProperty("stageName");
        SerializedProperty stageNumber = serializedObject.FindProperty("stageNumber");

        SerializedProperty intervals = serializedObject.FindProperty("spawnIntervals");
        SerializedProperty enemies = serializedObject.FindProperty("enemiesPerWave");
        SerializedProperty speedMultipliers = serializedObject.FindProperty("enemySpeedMultipliers");
        SerializedProperty bursts = serializedObject.FindProperty("burstSizes");

        // Identity
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageName);
        EditorGUILayout.PropertyField(stageNumber);

        EditorGUILayout.Space(10);

        // Waves
        if (intervals != null && enemies != null && speedMultipliers != null && bursts != null)
        {
            int waveCount = Mathf.Max(
                intervals.arraySize,
                enemies.arraySize,
                speedMultipliers.arraySize,
                bursts.arraySize);

            EnsureArraySizeFloat(intervals, waveCount, 2f);
            EnsureArraySizeFloat(enemies, waveCount, 5f);
            EnsureArraySizeFloat(speedMultipliers, waveCount, 1f);
            EnsureArraySizeInt(bursts, waveCount, 1);

            EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Wave Count: {waveCount}", EditorStyles.miniLabel);

            for (int i = 0; i < waveCount; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    intervals.DeleteArrayElementAtIndex(i);
                    enemies.DeleteArrayElementAtIndex(i);
                    speedMultipliers.DeleteArrayElementAtIndex(i);
                    bursts.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                intervals.GetArrayElementAtIndex(i).floatValue =
                    EditorGUILayout.FloatField("Spawn Interval (sec)",
                    intervals.GetArrayElementAtIndex(i).floatValue);

                enemies.GetArrayElementAtIndex(i).floatValue =
                    EditorGUILayout.FloatField("Enemy Count",
                    enemies.GetArrayElementAtIndex(i).floatValue);

                speedMultipliers.GetArrayElementAtIndex(i).floatValue =
                    EditorGUILayout.FloatField("Enemy Speed Multiplier",
                    speedMultipliers.GetArrayElementAtIndex(i).floatValue);

                int burstVal = Mathf.Max(1, EditorGUILayout.IntField(
                    "Burst Size (enemies per spawn tick)",
                    bursts.GetArrayElementAtIndex(i).intValue));
                bursts.GetArrayElementAtIndex(i).intValue = burstVal;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Wave"))
            {
                AddWave(intervals, enemies, speedMultipliers, bursts);
            }
        }

        EditorGUILayout.Space(10);

        // Enemy Roster
        EditorGUILayout.LabelField("Enemy Roster", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyRoster"), true);

        EditorGUILayout.Space(10);

        // Obstacles
        EditorGUILayout.LabelField("Obstacles", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayout"), true);

        EditorGUILayout.Space(10);

        // Difficulty
        EditorGUILayout.LabelField("Difficulty", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyHPBonus"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusHPPerWave"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusEnemiesPerWave"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnRandomness"));

        EditorGUILayout.Space(10);

        // Rewards
        EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("coinReward"));

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsureArraySizeFloat(SerializedProperty array, int size, float defaultValue)
    {
        while (array.arraySize < size)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).floatValue = defaultValue;
        }

        while (array.arraySize > size)
        {
            array.DeleteArrayElementAtIndex(array.arraySize - 1);
        }
    }

    private void EnsureArraySizeInt(SerializedProperty array, int size, int defaultValue)
    {
        while (array.arraySize < size)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).intValue = defaultValue;
        }

        while (array.arraySize > size)
        {
            array.DeleteArrayElementAtIndex(array.arraySize - 1);
        }
    }

    private void AddWave(
        SerializedProperty intervals,
        SerializedProperty enemies,
        SerializedProperty speedMultipliers,
        SerializedProperty bursts)
    {
        intervals.InsertArrayElementAtIndex(intervals.arraySize);
        enemies.InsertArrayElementAtIndex(enemies.arraySize);
        speedMultipliers.InsertArrayElementAtIndex(speedMultipliers.arraySize);
        bursts.InsertArrayElementAtIndex(bursts.arraySize);

        intervals.GetArrayElementAtIndex(intervals.arraySize - 1).floatValue = 2f;
        enemies.GetArrayElementAtIndex(enemies.arraySize - 1).floatValue = 5f;
        speedMultipliers.GetArrayElementAtIndex(speedMultipliers.arraySize - 1).floatValue = 1f;
        bursts.GetArrayElementAtIndex(bursts.arraySize - 1).intValue = 1;
    }
}
