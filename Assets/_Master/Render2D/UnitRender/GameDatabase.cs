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
        [Header("Memory Pool")]
        [Tooltip("Maximum number of active units of this type at the same time.")]
        public int maxCapacity = 1000; // Default is 1000
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
    [System.Serializable]
    public class BulletProfileData
    {
        public string bulletID = "New_Bullet";
        public int maxCapacity = 2000; // Đạn thường cần số lượng lớn
        public string logicTypeAQN;
        public string logicDisplayName;
        
        public Mesh mesh;
        public Material baseMaterial;
        public UnitAnimData animData;
        
        public float moveSpeed = 20.0f; // Đạn chỉ cần tốc độ bay
    }
    [CreateAssetMenu(fileName = "UnitsDatabase", menuName = "Abel/Units Database")]
    public class GameDatabase : ScriptableObject
    {
        public List<UnitProfileData> units = new List<UnitProfileData>();
        public List<BulletProfileData> bullets = new List<BulletProfileData>();

        // Helper lấy data theo ID
        public UnitProfileData GetUnitByID(string id)
        {
            return units.Find(u => u.unitID == id);
        }
        public BulletProfileData GetBulletByID(string id)
        {
            return bullets.Find(b => b.bulletID == id);
        }
    }
}