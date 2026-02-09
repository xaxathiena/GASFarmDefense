using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FD.Character;
using FD.Tests;
using System.Linq;

namespace FD.Editor
{
    /// <summary>
    /// Helper script to setup Performance Test scene
    /// </summary>
    public static class PerformanceTestSceneSetup
    {
        [MenuItem("FD/Setup Performance Test Scene")]
        public static void SetupPerformanceTestScene()
        {
            // Find the managers
            var perfManager = Object.FindObjectOfType<PerformanceTestManager>();
            var waveController = Object.FindObjectOfType<FDEnemyWaveController>();
            
            if (perfManager == null)
            {
                Debug.LogError("PerformanceTestManager not found in scene!");
                return;
            }
            
            if (waveController == null)
            {
                Debug.LogError("FDEnemyWaveController not found in scene!");
                return;
            }

            // Setup PerformanceTestManager
            SetupPerformanceManager(perfManager);
            
            // Setup Wave Controller
            SetupWaveController(waveController);
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            Debug.Log("Performance Test Scene setup complete!");
        }

        private static void SetupPerformanceManager(PerformanceTestManager manager)
        {
            SerializedObject so = new SerializedObject(manager);
            
            // Load tower prefabs
            string[] towerGuids = new string[] 
            {
                "a0f76716302b1499fb19793047956f0b", // BasicTower
                "fcfde47532eb341c7826112f224abb76", // SniperTower
                "fa93fd3dd38e0486aa2256396dea8066"  // AOETower
            };
            
            SerializedProperty towerPrefabsProp = so.FindProperty("towerPrefabs");
            towerPrefabsProp.ClearArray();
            
            foreach (var guid in towerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<TowerBase>(path);
                    if (prefab != null)
                    {
                        towerPrefabsProp.InsertArrayElementAtIndex(towerPrefabsProp.arraySize);
                        towerPrefabsProp.GetArrayElementAtIndex(towerPrefabsProp.arraySize - 1).objectReferenceValue = prefab;
                    }
                }
            }
            
            // Set number of towers
            so.FindProperty("numberOfTowers").intValue = 15;
            so.FindProperty("spawnTowersOnStart").boolValue = true;
            so.FindProperty("randomizeTowerTypes").boolValue = true;
            so.FindProperty("showPerformanceStats").boolValue = true;
            
            // Set path points
            var pathParent = GameObject.Find("PathPoints");
            if (pathParent != null)
            {
                var pathTransforms = pathParent.GetComponentsInChildren<Transform>()
                    .Where(t => t != pathParent.transform)
                    .OrderBy(t => t.name)
                    .ToArray();
                
                SerializedProperty pathProp = so.FindProperty("pathPoints");
                pathProp.ClearArray();
                
                foreach (var t in pathTransforms)
                {
                    pathProp.InsertArrayElementAtIndex(pathProp.arraySize);
                    pathProp.GetArrayElementAtIndex(pathProp.arraySize - 1).objectReferenceValue = t;
                }
            }
            
            so.FindProperty("offsetFromPath").floatValue = 3f;
            
            so.ApplyModifiedProperties();
            Debug.Log("PerformanceTestManager configured with towers and path");
        }

        private static void SetupWaveController(FDEnemyWaveController controller)
        {
            SerializedObject so = new SerializedObject(controller);
            
            // Set spawn point
            var spawnPoint = GameObject.Find("SpawnPoint");
            if (spawnPoint != null)
            {
                so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            }
            
            // Set path points
            var pathParent = GameObject.Find("PathPoints");
            if (pathParent != null)
            {
                var pathTransforms = pathParent.GetComponentsInChildren<Transform>()
                    .Where(t => t != pathParent.transform)
                    .OrderBy(t => t.name)
                    .ToArray();
                
                SerializedProperty pathProp = so.FindProperty("pathPoints");
                pathProp.ClearArray();
                
                foreach (var t in pathTransforms)
                {
                    pathProp.InsertArrayElementAtIndex(pathProp.arraySize);
                    pathProp.GetArrayElementAtIndex(pathProp.arraySize - 1).objectReferenceValue = t;
                }
            }
            
            // Load enemy prefabs
            string[] enemyGuids = new string[] 
            {
                "46a53775df32b5e4c81d5b3635ec7590", // Enemy01
                "5dc5161daf9bf412aa51a306bf543b5b", // FastEnemy
                "eacbc9ad84cc84b02b642d647d3cb8ae", // TankEnemy
                "ddeecd804e2c2437d9f3cec10705187e"  // FlyingEnemy
            };
            
            // Create waves
            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.ClearArray();
            
            // Wave 1: Mixed enemies
            wavesProp.InsertArrayElementAtIndex(0);
            SerializedProperty wave1 = wavesProp.GetArrayElementAtIndex(0);
            wave1.FindPropertyRelative("waveName").stringValue = "Mixed Wave";
            wave1.FindPropertyRelative("delayBeforeWave").floatValue = 1f;
            
            SerializedProperty enemies1 = wave1.FindPropertyRelative("enemies");
            enemies1.ClearArray();
            
            for (int i = 0; i < enemyGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(enemyGuids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<FDEnemyBase>(path);
                    if (prefab != null)
                    {
                        enemies1.InsertArrayElementAtIndex(enemies1.arraySize);
                        var entry = enemies1.GetArrayElementAtIndex(enemies1.arraySize - 1);
                        entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = prefab;
                        entry.FindPropertyRelative("count").intValue = 5;
                        entry.FindPropertyRelative("spawnInterval").floatValue = 0.3f;
                    }
                }
            }
            
            so.FindProperty("autoStartOnPlay").boolValue = true;
            so.FindProperty("timeBetweenWaves").floatValue = 3f;
            
            so.ApplyModifiedProperties();
            Debug.Log("FDEnemyWaveController configured with enemies and waves");
        }
    }
}
