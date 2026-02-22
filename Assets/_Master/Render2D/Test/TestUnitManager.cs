using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.Test
{
    public class TestUnitManager : UnitLogicSystemBase
    {
        [Header("Simulation Settings")]
        public List<string> unitIDsToSpawn;
        public UnitState targetAnimState = UnitState.Idle;
        [Range(0, 10000)] public int spawnCount = 0;

        [Header("Debug Actions")]
        public int indexToRemove = 0;
        public bool triggerRemove = false;

        [Header("Debug Gizmos")]
        public bool showGizmos = true;
        public Color gizmoColor = Color.cyan;
        public float gizmoRadius = 0.5f;

        private static int nextGlobalID = 1;

        // --- INTERNAL LOGIC DATA ---
        private class SimEntity : ILogicEntity
        {
            public int instanceID;
            public string id;
            public float2 position;
            public float rotation;
            public float scale;
            public UnitState currentState;

            // Implementing ILogicEntity
            public string UnitID => id;
            public float2 Position => position;
            public float Rotation => rotation;
            public float Scale => scale;
            public UnitState CurrentState => currentState;
            public float PlaySpeed => 1.0f;
            public int GetInstanceID() => instanceID;

            public string GetUnitInfo()
            {
                return $"Logic Name: {id}\nSimulated HP: 100/100\nState: {currentState}";
            }
        }

        void Update()
        {
            if (unitIDsToSpawn == null || unitIDsToSpawn.Count == 0) return;

            HandleSpawnCountChanges();
            HandleRemoveRequest();
            
            // Call the base class method to push everything to the GPU!
            PushDataToRenderSystem();
        }

        private void HandleSpawnCountChanges()
        {
            // Add new entities
            while (activeEntities.Count < spawnCount)
            {
                string randomID = unitIDsToSpawn[UnityEngine.Random.Range(0, unitIDsToSpawn.Count)];
                float2 pos = (spawnCount == 1)
                    ? float2.zero
                    : new float2(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-20f, 20f));

                var newEntity = new SimEntity
                {
                    instanceID = nextGlobalID++,
                    id = randomID,
                    position = pos,
                    rotation = UnityEngine.Random.Range(0f, 360f),
                    scale = 1f,
                    currentState = targetAnimState
                };
                
                // Use the base class method to register
                AddEntity(newEntity); 
            }

            // Sync animation state if changed in Inspector
            foreach (var entity in activeEntities.Values.Cast<SimEntity>())
            {
                entity.currentState = targetAnimState;
            }

            // Remove excess entities
            if (activeEntities.Count > spawnCount)
            {
                int excess = activeEntities.Count - spawnCount;
                for (int i = 0; i < excess; i++)
                {
                    // Remove the last added entity for simplicity in testing
                    int lastKey = activeEntities.Keys.Last();
                    RemoveEntity(lastKey);
                }
            }
        }

        private void HandleRemoveRequest()
        {
            if (triggerRemove)
            {
                triggerRemove = false;
                // Try to find the N-th entity to remove (since dictionary isn't index-based)
                if (indexToRemove >= 0 && indexToRemove < activeEntities.Count)
                {
                    int targetID = activeEntities.Keys.ElementAt(indexToRemove);
                    RemoveEntity(targetID);
                    spawnCount = activeEntities.Count; // Sync slider
                    Debug.Log($"Removed entity at logical index {indexToRemove} (ID: {targetID}). Remaining: {spawnCount}");
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || activeEntities == null || activeEntities.Count == 0) return;

            Gizmos.color = gizmoColor;

            foreach (var entity in activeEntities.Values)
            {
                Vector3 worldPos = new Vector3(entity.Position.x, 0, entity.Position.y);
                Gizmos.DrawWireSphere(worldPos, gizmoRadius);

                float rad = entity.Rotation * Mathf.Deg2Rad;
                Vector3 forwardDir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
                Gizmos.DrawLine(worldPos, worldPos + forwardDir * gizmoRadius * 1.5f);
            }
        }
    }
}