using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Identity
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stageName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stageNumber"));

        EditorGUILayout.Space(10);

        // Waves
        SerializedProperty intervals = serializedObject.FindProperty("spawnIntervals");
        SerializedProperty enemies = serializedObject.FindProperty("enemiesPerWave");

        if (intervals != null && enemies != null)
        {
            int waveCount = Mathf.Min(intervals.arraySize, enemies.arraySize);

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
                    break;
                }

                EditorGUILayout.EndHorizontal();

                intervals.GetArrayElementAtIndex(i).floatValue =
                    EditorGUILayout.FloatField("Spawn Interval (sec)",
                    intervals.GetArrayElementAtIndex(i).floatValue);

                enemies.GetArrayElementAtIndex(i).floatValue =
                    EditorGUILayout.FloatField("Enemy Count",
                    enemies.GetArrayElementAtIndex(i).floatValue);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Wave"))
            {
                intervals.InsertArrayElementAtIndex(intervals.arraySize);
                enemies.InsertArrayElementAtIndex(enemies.arraySize);

                intervals.GetArrayElementAtIndex(intervals.arraySize - 1).floatValue = 2f;
                enemies.GetArrayElementAtIndex(enemies.arraySize - 1).floatValue = 5f;
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
}


