using UnityEngine;
using System.Collections.Generic;
using GAS;

namespace FD.Data
{
    /// <summary>
    /// ScriptableObject wrapper cho TowerData
    /// Dùng để design towers trong Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "TowerConfig", menuName = "FD/Tower Config")]
    public class TowerConfigSO : ScriptableObject
    {
        [Header("Identification")]
        public string towerID = "Tower_Basic";
        public int level = 1;
        
        [Header("Targeting")]
        public float targetRange = 6f;
        public int maxTargets = 1;
        public LayerMask targetLayerMask = ~0;
        
        [Header("Combat Stats")]
        public float baseDamage = 15f;
        public float attackSpeed = 1f;
        public float criticalChance = 0.1f;
        public float criticalMultiplier = 2f;
        
        [Header("Resources")]
        public float maxHealth = 200f;
        public float maxMana = 100f;
        public float manaRegen = 2f;
        
        [Header("Abilities")]
        public List<TowerAbilityEntry> abilities = new List<TowerAbilityEntry>();
        
        [Header("Performance")]
        public float targetUpdateInterval = 0.2f;
        
        
    }
    
    [System.Serializable]
    public class TowerAbilityEntry
    {
        public GameplayAbilityData ability;
        public int level = 1;
        public bool isPassive = false;
    }
}
