using UnityEngine;
using Abel.TowerDefense.Data;
using System.Collections.Generic;
using static UnitAnimData;

namespace Abel.TowerDefense.Config
{
    public enum UnitState
    {
        Idle = 0,
        Attack = 1,
        Move = 2,
        Die = 3
    }
    [System.Serializable]
    public class UnitProfileData
    {
        [Header("Identity")]
        public string unitID; // VD: "Goblin_Archer"

        [Header("Logic Configuration")]
        // Lưu tên đầy đủ của class (Assembly Qualified Name) để Reflection tìm ra
        [HideInInspector] public string logicTypeAQN;
        // Tên ngắn gọn để hiện lên Editor cho đẹp
        [HideInInspector] public string logicDisplayName;

        [Header("Visual Resources")]
        public Mesh mesh;
        public Material baseMaterial;
        public UnitAnimData animData;
        
        public float baseMoveSpeed = 5.0f;
        public float baseAttackSpeed = 1.0f;
        // Hàm tiện ích để tra cứu nhanh
        public int GetAnimIndex(UnitState state)
        {
            var animIndex = animData.animations.FindIndex(i => i.animName == state.ToString());
            if (animIndex >= 0) return animData.animations[animIndex].startFrame;
            return 0;
        }
    }
    [CreateAssetMenu(fileName = "UnitsDatabase", menuName = "Abel/Units Database")]
    public class UnitsProfile : ScriptableObject
    {
        public List<UnitProfileData> units = new List<UnitProfileData>();

        // Helper lấy data theo ID
        public UnitProfileData GetUnitByID(string id)
        {
            return units.Find(u => u.unitID == id);
        }
    }
}