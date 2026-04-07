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

        int waveCount = intervals.arraySize;

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
                EditorGUILayout.FloatField("Spawn Interval (sec)", intervals.GetArrayElementAtIndex(i).floatValue);
            enemies.GetArrayElementAtIndex(i).floatValue =
                EditorGUILayout.FloatField("Enemy Count", enemies.GetArrayElementAtIndex(i).floatValue);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ Add Wave"))
        {
            intervals.InsertArrayElementAtIndex(waveCount);
            enemies.InsertArrayElementAtIndex(waveCount);
            // Default values for new wave
            intervals.GetArrayElementAtIndex(waveCount).floatValue = 2f;
            enemies.GetArrayElementAtIndex(waveCount).floatValue = 5f;
        }

        EditorGUILayout.Space(10);

        // Enemy Roster
        EditorGUILayout.LabelField("Enemy Roster", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyRoster"), true);

        EditorGUILayout.Space(10);

        // Difficulty
        EditorGUILayout.LabelField("Difficulty", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyHPBonus"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnRandomness"));

        serializedObject.ApplyModifiedProperties();
    }
}
