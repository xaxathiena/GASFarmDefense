using UnityEngine;
using System;
using System.Collections.Generic;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.Render
{
    public class GameUnitManager : MonoBehaviour
    {
        [Header("Load Profiles")]
        public UnitsProfile unitProfiles;

        // Dictionary quản lý các Group theo UnitID
        private Dictionary<string, UnitGroupBase> loadedGroups = new Dictionary<string, UnitGroupBase>();
        public IReadOnlyDictionary<string, UnitGroupBase> LoadedGroups => loadedGroups;

        private void CreateGroupFromProfile(UnitProfileData profile)
        {
            if (string.IsNullOrEmpty(profile.logicTypeAQN))
            {
                Debug.LogError($"Profile {profile.unitID} chưa chọn Logic Class!");
                return;
            }

            try
            {
                // REFLECTION MAGIC: Tạo instance của class kế thừa UnitGroupBase từ string
                Type logicType = Type.GetType(profile.logicTypeAQN);

                // Activator.CreateInstance(Type, params object[])
                // Gọi constructor: public MyGroup(UnitProfile p) : base(p)
                if (!loadedGroups.ContainsKey(profile.unitID))
                {
                    UnitGroupBase group = (UnitGroupBase)Activator.CreateInstance(logicType, new object[] { profile });
                    loadedGroups.Add(profile.unitID, group);
                    Debug.Log($"Loaded Group: {profile.unitID} using logic {logicType.Name}");
                }
                else
                {
                    Debug.LogWarning($"Group with UnitID {profile.unitID} already exists!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi tạo group cho {profile.unitID}: {e.Message}");
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var group in loadedGroups.Values)
            {
                group.Update(dt);
            }
        }

        // API Spawn cho Input gọi
        public void SpawnUnit(string unitID, Vector2 pos)
        {
            if (loadedGroups.TryGetValue(unitID, out var group))
            {
                group.Spawn(pos);
            }
            else
            {
                // create group on the fly if not exist (Optional)
                Debug.LogWarning($"UnitID {unitID} does not exist, creating group on the fly.");
                var profile = unitProfiles.GetUnitByID(unitID);
                if (profile != null)
                {
                    CreateGroupFromProfile(profile);
                    loadedGroups[unitID].Spawn(pos);
                }
                else
                {
                    Debug.LogError($"Không tìm thấy profile cho UnitID {unitID}!");
                }
            }
        }

        void OnDestroy()
        {
            foreach (var group in loadedGroups.Values)
            {
                group.Dispose();
            }
        }
    }
}