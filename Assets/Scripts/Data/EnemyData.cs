using FD.Ability;
using UnityEngine;

namespace FD.Data
{
    /// <summary>
    /// Configuration data cho enemy - Không có logic!
    /// Có thể tạo từ ScriptableObject hoặc khởi tạo runtime
    /// </summary>
    public class EnemyData
    {
        // Movement config
        public float MoveSpeed { get; set; } = 3f;
        public float WaypointThreshold { get; set; } = 0.1f;
        
        // Combat config
        public float DetectionRange { get; set; } = 10f;
        public float AttackRange { get; set; } = 2f;
        public float AttackCooldown { get; set; } = 1f;
        
        // Stats
        public float InitialHealth { get; set; } = 1000f;
        public float InitialArmor { get; set; } = 5f;
        public EArmorType ArmorType { get; set; } = EArmorType.Medium;
        
        // Identification
        public string EnemyID { get; set; }
        public int EnemyLevel { get; set; } = 1;
        
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
