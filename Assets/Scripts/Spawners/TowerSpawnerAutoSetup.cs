using UnityEngine;
using System.Collections.Generic;
using FD.Data;

namespace FD.Spawners
{
    /// <summary>
    /// Helper script để auto-assign references cho TowerSpawner trong test scene
    /// </summary>
    [RequireComponent(typeof(TowerSpawner))]
    public class TowerSpawnerAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        [Header("Reference Names (Optional)")]
        [SerializeField] private string waypoint1Name = "Waypoint1";
        [SerializeField] private string waypoint2Name = "Waypoint2";
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupReferences();
            }
        }
        
        [ContextMenu("Setup References")]
        public void SetupReferences()
        {
            var spawner = GetComponent<TowerSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[TowerSpawnerAutoSetup] No TowerSpawner component found!");
                return;
            }
            
            // Find waypoints in scene for "near path" placement
            var waypoint1 = GameObject.Find(waypoint1Name);
            var waypoint2 = GameObject.Find(waypoint2Name);
            
            if (waypoint1 == null || waypoint2 == null)
            {
                Debug.LogWarning("[TowerSpawnerAutoSetup] Waypoints not found - will use area spawn mode");
            }
            
            // Use reflection to set private serialized fields
            var spawnerType = spawner.GetType();
            
            if (waypoint1 != null && waypoint2 != null)
            {
                var pathPointsField = spawnerType.GetField("pathPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pathPointsField != null)
                {
                    pathPointsField.SetValue(spawner, new Transform[] { waypoint1.transform, waypoint2.transform });
                    Debug.Log("[TowerSpawnerAutoSetup] Set pathPoints for near-path placement");
                }
            }
            
            // Set offset from path
            var offsetField = spawnerType.GetField("offsetFromPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (offsetField != null)
            {
                offsetField.SetValue(spawner, 2f);
                Debug.Log("[TowerSpawnerAutoSetup] Set offsetFromPath = 2f");
            }
            
            // Set number of towers
            var numberOfTowersField = spawnerType.GetField("numberOfTowers", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (numberOfTowersField != null)
            {
                numberOfTowersField.SetValue(spawner, 5);
                Debug.Log("[TowerSpawnerAutoSetup] Set numberOfTowers = 5");
            }
            
            // Enable randomize
            var randomizeField = spawnerType.GetField("randomizeTowerTypes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (randomizeField != null)
            {
                randomizeField.SetValue(spawner, false);
                Debug.Log("[TowerSpawnerAutoSetup] Set randomizeTowerTypes = false");
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(spawner);
#endif
            
            Debug.Log("[TowerSpawnerAutoSetup] ✅ Setup completed!");
            Debug.Log("[TowerSpawnerAutoSetup] NOTE: Bạn cần manually assign Tower Prefabs và Configs trong Inspector!");
        }
    }
}
