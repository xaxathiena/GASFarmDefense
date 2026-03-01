using UnityEngine;
using UnityEditor;

namespace Abel.TranHuongDao.Core.Editor
{
    /// <summary>
    /// Custom Inspector for the WavesConfig ScriptableObject.
    /// Provides a cleaner, tiered UI for mapping out maps, waves, and spawn groups
    /// so game designers don't have to navigate deeply nested default Unity lists.
    /// </summary>
    [CustomEditor(typeof(WavesConfig))]
    public class WavesConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _mapProfilesProp;

        private void OnEnable()
        {
            // Link directly to the serialization system for built-in Undo/Redo and dirty flagging
            _mapProfilesProp = serializedObject.FindProperty("mapProfiles");
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object before making modifications
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Waves Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Iterate over each map profile
            for (int i = 0; i < _mapProfilesProp.arraySize; i++)
            {
                DrawMapProfile(_mapProfilesProp.GetArrayElementAtIndex(i), i);
                EditorGUILayout.Space();
            }

            // Button to append a new MapWaveProfile
            if (GUILayout.Button("Add New Map", GUILayout.Height(30)))
            {
                _mapProfilesProp.arraySize++;
                SerializedProperty newProfile = _mapProfilesProp.GetArrayElementAtIndex(_mapProfilesProp.arraySize - 1);

                // Initialize defaults for the newly added map
                newProfile.FindPropertyRelative("MapID").stringValue = $"Map_{_mapProfilesProp.arraySize}";
                newProfile.FindPropertyRelative("waves").ClearArray();
            }

            // Apply modifications to the target object
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMapProfile(SerializedProperty mapProfileProp, int index)
        {
            SerializedProperty mapIDProp = mapProfileProp.FindPropertyRelative("MapID");
            SerializedProperty wavesProp = mapProfileProp.FindPropertyRelative("waves");

            // Wrap the map content inside a styled vertical box
            EditorGUILayout.BeginVertical("box");

            // Header for the Map Profile
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Map: {mapIDProp.stringValue}", EditorStyles.boldLabel, GUILayout.Width(150));
            mapIDProp.stringValue = EditorGUILayout.TextField("", mapIDProp.stringValue);

            // Delete button for Map Profile
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Map", GUILayout.Width(100)))
            {
                _mapProfilesProp.DeleteArrayElementAtIndex(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Render all configured waves for this map
            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                DrawWaveConfig(wavesProp, i);
            }

            // Button to append a new Wave to this map
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Add Wave to Map", GUILayout.Height(25)))
            {
                wavesProp.arraySize++;
                SerializedProperty newWave = wavesProp.GetArrayElementAtIndex(wavesProp.arraySize - 1);

                // Initialize defaults for the newly added wave
                newWave.FindPropertyRelative("waveName").stringValue = $"Wave {wavesProp.arraySize}";
                newWave.FindPropertyRelative("preparationTime").floatValue = 5f;
                newWave.FindPropertyRelative("startDelay").floatValue = 2f;
                newWave.FindPropertyRelative("spawnEntries").ClearArray();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawWaveConfig(SerializedProperty wavesProp, int index)
        {
            SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(index);
            SerializedProperty waveNameProp = waveProp.FindPropertyRelative("waveName");
            SerializedProperty prepTimeProp = waveProp.FindPropertyRelative("preparationTime");
            SerializedProperty startDelayProp = waveProp.FindPropertyRelative("startDelay");
            SerializedProperty spawnsProp = waveProp.FindPropertyRelative("spawnEntries");

            // Use HelpBox style to clearly distinguish waves
            GUIStyle waveBoxStyle = new GUIStyle("HelpBox");
            EditorGUILayout.BeginVertical(waveBoxStyle);

            // Wave Header with Delete Button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Wave: {waveNameProp.stringValue}", EditorStyles.boldLabel, GUILayout.Width(150));

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                wavesProp.DeleteArrayElementAtIndex(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Basic Wave Configuration
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(45));
            waveNameProp.stringValue = EditorGUILayout.TextField(waveNameProp.stringValue);
            GUILayout.Label("Prep Time:", GUILayout.Width(70));
            prepTimeProp.floatValue = EditorGUILayout.FloatField(prepTimeProp.floatValue, GUILayout.Width(40));
            GUILayout.Label("Start Delay:", GUILayout.Width(70));
            startDelayProp.floatValue = EditorGUILayout.FloatField(startDelayProp.floatValue, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Spawn Entries:", EditorStyles.miniBoldLabel);

            // Draw header columns for Spawn Entries
            if (spawnsProp.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enemy ID", EditorStyles.miniLabel, GUILayout.MinWidth(80));
                EditorGUILayout.LabelField("Count", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Interval", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Path", EditorStyles.miniLabel, GUILayout.Width(40));
                EditorGUILayout.LabelField("", GUILayout.Width(25)); // Spacing for delete button
                EditorGUILayout.EndHorizontal();
            }

            // Iterate through every spawn entry inside this wave
            for (int i = 0; i < spawnsProp.arraySize; i++)
            {
                DrawSpawnEntry(spawnsProp, i);
            }

            // Add Spawn Button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Spawn", GUILayout.Width(100)))
            {
                spawnsProp.arraySize++;
                SerializedProperty newSpawn = spawnsProp.GetArrayElementAtIndex(spawnsProp.arraySize - 1);

                // Set default spawn data
                newSpawn.FindPropertyRelative("enemyID").stringValue = "enemy_basic";
                newSpawn.FindPropertyRelative("count").intValue = 1;
                newSpawn.FindPropertyRelative("intervalBetweenSpawns").floatValue = 1f;
                newSpawn.FindPropertyRelative("pathIndex").intValue = 0;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawSpawnEntry(SerializedProperty spawnsProp, int index)
        {
            SerializedProperty spawnProp = spawnsProp.GetArrayElementAtIndex(index);
            SerializedProperty enemyIDProp = spawnProp.FindPropertyRelative("enemyID");
            SerializedProperty countProp = spawnProp.FindPropertyRelative("count");
            SerializedProperty intervalProp = spawnProp.FindPropertyRelative("intervalBetweenSpawns");
            SerializedProperty pathProp = spawnProp.FindPropertyRelative("pathIndex");

            EditorGUILayout.BeginHorizontal();

            // Render properties inline horizontally
            enemyIDProp.stringValue = EditorGUILayout.TextField(enemyIDProp.stringValue, GUILayout.MinWidth(80));
            countProp.intValue = EditorGUILayout.IntField(countProp.intValue, GUILayout.Width(60));
            intervalProp.floatValue = EditorGUILayout.FloatField(intervalProp.floatValue, GUILayout.Width(60));
            pathProp.intValue = EditorGUILayout.IntField(pathProp.intValue, GUILayout.Width(40));

            // Delete button for this specific spawn row
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                spawnsProp.DeleteArrayElementAtIndex(index);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }
}
