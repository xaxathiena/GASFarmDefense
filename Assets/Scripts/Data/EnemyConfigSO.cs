using FD.Ability;
using UnityEngine;

namespace FD.Data
{
    /// <summary>
    /// ScriptableObject wrapper cho EnemyData
    /// Dùng để design enemies trong Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "FD/Enemy Config")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Identification")]
        public string enemyID = "Enemy_Basic";
        public int level = 1;
        
        [Header("Movement")]
        public float moveSpeed = 3f;
        public float waypointThreshold = 0.1f;
        
        [Header("Combat")]
        public float detectionRange = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;
        
        [Header("Stats")]
        public float initialHealth = 1000f;
        public float initialArmor = 5f;
        public EArmorType armorType = EArmorType.Medium;
        
        // Convert to runtime data
        public EnemyData ToEnemyData()
        {
            return new EnemyData
            {
                EnemyID = enemyID,
                EnemyLevel = level,
                MoveSpeed = moveSpeed,
                WaypointThreshold = waypointThreshold,
                DetectionRange = detectionRange,
                AttackRange = attackRange,
                AttackCooldown = attackCooldown,
                InitialHealth = initialHealth,
                InitialArmor = initialArmor,
                ArmorType = armorType
            };
        }
    }
}
