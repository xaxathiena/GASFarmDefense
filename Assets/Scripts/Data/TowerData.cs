using UnityEngine;
using System.Collections.Generic;
using GAS;

namespace FD.Data
{
    /// <summary>
    /// Pure data class cho Tower configuration
    /// Immutable, serializable, testable
    /// </summary>
    [System.Serializable]
    public class TowerData
    {
        // Identification
        public string TowerID { get; set; }
        public int Level { get; set; }
        
        // Targeting
        public float TargetRange { get; set; }
        public int MaxTargets { get; set; }
        public LayerMask TargetLayerMask { get; set; }
        
        // Combat Stats
        public float BaseDamage { get; set; }
        public float AttackSpeed { get; set; }
        public float CriticalChance { get; set; }
        public float CriticalMultiplier { get; set; }
        
        // Resources
        public float MaxHealth { get; set; }
        public float MaxMana { get; set; }
        public float ManaRegen { get; set; }
        
        // Abilities
        public List<AbilityConfig> Abilities { get; set; }
        
        // Performance
        public float TargetUpdateInterval { get; set; }
        
        public TowerData()
        {
            TowerID = "Tower_Basic";
            Level = 1;
            TargetRange = 6f;
            MaxTargets = 1;
            TargetLayerMask = ~0;
            BaseDamage = 15f;
            AttackSpeed = 1f;
            CriticalChance = 0.1f;
            CriticalMultiplier = 2f;
            MaxHealth = 200f;
            MaxMana = 100f;
            ManaRegen = 2f;
            TargetUpdateInterval = 0.2f;
            Abilities = new List<AbilityConfig>();
        }
        
        public static TowerData CreateBasic()
        {
            return new TowerData
            {
                TowerID = "Tower_Basic",
                TargetRange = 8f,
                MaxTargets = 1,
                BaseDamage = 15f
            };
        }
        
        public static TowerData CreateSniper()
        {
            return new TowerData
            {
                TowerID = "Tower_Sniper",
                TargetRange = 15f,
                MaxTargets = 1,
                BaseDamage = 50f,
                AttackSpeed = 0.5f
            };
        }
        
        public static TowerData CreateAOE()
        {
            return new TowerData
            {
                TowerID = "Tower_AOE",
                TargetRange = 10f,
                MaxTargets = 5,
                BaseDamage = 20f
            };
        }
    }
    
    [System.Serializable]
    public class AbilityConfig
    {
        public GameplayAbility Ability { get; set; }
        public int Level { get; set; }
        public bool IsPassive { get; set; }
        
        public AbilityConfig()
        {
            Level = 1;
            IsPassive = false;
        }
    }
    
    /// <summary>
    /// Runtime state cho Tower
    /// Mutable, không serialize
    /// </summary>
    public class TowerState
    {
        public List<Transform> CachedTargets { get; set; }
        public float NextTargetUpdateTime { get; set; }
        public bool IsActive { get; set; }
        
        public TowerState()
        {
            CachedTargets = new List<Transform>();
            NextTargetUpdateTime = 0f;
            IsActive = true;
        }
        
        public bool ShouldUpdateTargets(float currentTime)
        {
            return currentTime >= NextTargetUpdateTime;
        }
        
        public void SetNextUpdateTime(float currentTime, float interval)
        {
            NextTargetUpdateTime = currentTime + interval;
        }
    }
}
