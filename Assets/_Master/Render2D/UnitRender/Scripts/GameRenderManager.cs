using UnityEngine;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Unity.Collections;

namespace Abel.TowerDefense.Render
{
    public class GameRenderManager : MonoBehaviour
    {
        [Header("Database")]
        public UnitRenderDatabase gameDatabase;

        [Header("Health Bar")]
        // Shared quad mesh used for every health-bar instance (assign a built-in Unity Quad).
        public UnityEngine.Mesh     hpQuadMesh;
        // Material that reads _HPPercent per instance to draw the bar (GPU instancing must be enabled).
        public UnityEngine.Material defaultHPMaterial;

        // Only one dictionary is needed, because rendering is the same for both units and bullets
        private Dictionary<string, RenderGroup> renderGroups = new Dictionary<string, RenderGroup>();
        public IReadOnlyDictionary<string, RenderGroup> LoadedRenderGroups => renderGroups;
        void Start()
        {
            if (gameDatabase == null) return;

            // Initialize one RenderGroup per unit type registered in the database.
            foreach (var unit in gameDatabase.units)
            {
                if (!renderGroups.ContainsKey(unit.unitID))
                    renderGroups.Add(unit.unitID, new RenderGroup(unit, hpQuadMesh, defaultHPMaterial));
            }
        }

        /// <summary>
        /// System A (Logic) be call thì function it the end of loop
        /// </summary>
        public void PushDataToRender(string entityID, NativeArray<UnitSyncData> data, int count)
        {
            if (renderGroups.TryGetValue(entityID, out var group))
            {
                group.SyncAndRender(data, count, Time.deltaTime);
            }
            else
            {
                var unitData = gameDatabase.GetUnitByID(entityID); // Optional: try bullets if not found in units
                if (unitData == null)
                {
                    Debug.LogWarning($"RenderManager: Unknown ID {entityID}");
                }
                else
                {
                    // Lazily create a RenderGroup for unit types not present at Start.
                    renderGroups.Add(unitData.unitID, new RenderGroup(unitData, hpQuadMesh, defaultHPMaterial));
                    renderGroups[unitData.unitID].SyncAndRender(data, count, Time.deltaTime);
                }
            }
        }

        void OnDestroy()
        {
            foreach (var group in renderGroups.Values) group.Dispose();
        }
    }
}