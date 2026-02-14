using FD.Ability;
using UnityEngine;

namespace FD.Data
{
    /// <summary>
    /// Configuration data cho enemy - Không có logic!
    /// Có thể tạo từ ScriptableObject hoặc khởi tạo runtime
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "FD/Enemy Data", order = 2)]
    public class EnemyData : ScriptableObject
    {
        // Movement config
        public float MoveSpeed  = 3f;
        public float WaypointThreshold  = 0.1f;
        
        // Combat config
        public float DetectionRange  = 10f;
        public float AttackRange  = 2f;
        public float AttackCooldown  = 1f;
        
        // Stats
        public float InitialHealth  = 1000f;
        public float InitialArmor  = 5f;
        public EArmorType ArmorType  = EArmorType.Medium;
        
        // Identification
        public string EnemyID;
        public int EnemyLevel  = 1;
        
        // Constructor cho dễ khởi tạo
        public EnemyData() { }
        
        public EnemyData(float health, float speed, float detectionRange)
        {
            InitialHealth = health;
            MoveSpeed = speed;
            DetectionRange = detectionRange;
        }
        
        // Static factory cho default values
        public static EnemyData CreateDefault()
        {
            return new EnemyData
            {
                MoveSpeed = 3f,
                DetectionRange = 10f,
                AttackRange = 2f,
                InitialHealth = 1000f,
                ArmorType = EArmorType.Medium
            };
        }
    }
}
