using UnityEngine;
using System;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;
using Abel.TowerDefense.Logic;

namespace Abel.TowerDefense.Render
{
    public class GameUnitManager : MonoBehaviour
    {
        [Header("Database")]
        public GameDatabase gameDatabase;

        // Separate dictionaries to prevent ID collisions
        private Dictionary<string, UnitGroupBase> unitGroups = new Dictionary<string, UnitGroupBase>();

        // Notice the type is BulletGroupBase, allowing us to use bullet-specific methods (like Spawn with direction)
        private Dictionary<string, BulletGroup> bulletGroups = new Dictionary<string, BulletGroup>();
        public IReadOnlyDictionary<string, UnitGroupBase> LoadedUnitGroups => unitGroups;
        public IReadOnlyDictionary<string, BulletGroup> LoadedBulletGroups => bulletGroups;
        void Start()
        {
            if (gameDatabase == null) return;

            // 1. Initialize Units
            foreach (var unitData in gameDatabase.units)
            {
                CreateGroup(unitData.unitID, unitData.logicTypeAQN, unitData, unitGroups);
            }
        }

        // A generic helper to instantiate and register groups via Reflection
        private void CreateGroup<TGroup, TData>(string id, string logicAQN, TData profileData, Dictionary<string, TGroup> dictionary)
            where TGroup : UnitGroupBase
        {
            if (string.IsNullOrEmpty(logicAQN)) return;

            try
            {
                Type logicType = Type.GetType(logicAQN);
                // Create instance passing the specific profile data (UnitProfileData or BulletProfileData)
                TGroup group = (TGroup)Activator.CreateInstance(logicType, new object[] { profileData });

                if (!dictionary.ContainsKey(id))
                {
                    dictionary.Add(id, group);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameUnitManager] Failed to create group for {id}: {e.Message}");
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Update all units
            foreach (var group in unitGroups.Values) group.Update(dt);

            // Update all bullets
            foreach (var group in bulletGroups.Values) group.Update(dt);
        }

        // --- PUBLIC API FOR INPUT / GAMEPLAY ---

        // API for Units (Needs only Position)
        public void SpawnUnit(string unitID, Vector2 pos)
        {
            if (unitGroups.TryGetValue(unitID, out var group))
            {
                group.Spawn(pos);
            }
        }

        // API for Bullets (Needs Position AND Direction)
        public void SpawnBullet(string bulletID, Vector2 pos, Vector2 direction)
        {
            if (bulletGroups.TryGetValue(bulletID, out var group))
            {
                // We can safely call the specific bullet spawn method because we strictly typed the dictionary
                group.Spawn(pos, direction);
            }
        }
        public void SetPathForGroup(string unitID, Vector2[] path)
        {
            if (unitGroups.TryGetValue(unitID, out var group))
            {
                if (group is Abel.TowerDefense.Logic.EnemyFollowingGroup pathGroup)
                {
                    pathGroup.pathWaypoints = path;
                }
            }
            else
            {
                Debug.LogWarning($"Không thể set path! Chưa tạo Group cho UnitID {unitID}");
            }
        }

        void OnDestroy()
        {
            foreach (var group in unitGroups.Values) group.Dispose();
            foreach (var group in bulletGroups.Values) group.Dispose();
        }
    }
}