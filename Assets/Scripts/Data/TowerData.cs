using UnityEngine;
using System.Collections.Generic;
using GAS;
using static FD.Character.TowerBase;

namespace FD.Data
{
    /// <summary>
    /// Pure data class cho Tower configuration
    /// Immutable, serializable, testable
    /// </summary>
    [System.Serializable]
    public class TowerData
    {
        public int helloWorld;
        // Identification
        public string TowerID;
        public int Level;
        
        // Targeting
        public float TargetRange;
        public int MaxTargets;
        public LayerMask TargetLayerMask;
        
        // Combat Stats
        public float BaseDamage;
        public float AttackSpeed;
        public float CriticalChance;
        public float CriticalMultiplier;
        
        // Resources
        public float MaxHealth;
        public float MaxMana;
        public float ManaRegen;
        
        // Abilities
        public List<AbilityInit> Abilities;
        
        // Performance
        public float TargetUpdateInterval;
        
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
            Abilities = new List<AbilityInit>();
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
        public GameplayAbilityData Ability { get; set; }
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
