using UnityEngine;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Unity.Collections;

namespace Abel.TowerDefense.Render
{
    public class GameRenderManager : MonoBehaviour
    {
        [Header("Database")]
        public GameDatabase gameDatabase;

        // Only one dictionary is needed, because rendering is the same for both units and bullets
        private Dictionary<string, RenderGroup> renderGroups = new Dictionary<string, RenderGroup>();

        void Start()
        {
            if (gameDatabase == null) return;

            // Initialize RenderGroup for all IDs in the database
            foreach (var unit in gameDatabase.units)
            {
            if (!renderGroups.ContainsKey(unit.unitID))
                renderGroups.Add(unit.unitID, new RenderGroup(unit));
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
                    renderGroups.Add(unitData.unitID, new RenderGroup(unitData));
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