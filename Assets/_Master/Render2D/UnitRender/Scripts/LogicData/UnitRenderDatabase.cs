using UnityEngine;
using Abel.TowerDefense.Data;
using System.Collections.Generic;
using static UnitAnimData;

namespace Abel.TowerDefense.Config
{
    public enum UnitAnimState
    {
        Idle = 0,
        Attack = 1,
        Move = 2,
        Die = 3,
        CastSkill1 = 4,
        CastSkill2 = 5,
        CastSkill3 = 6,
        CastSkill4 = 7,
        CastSkill5 = 8,
        Sprint = 9,
        Hurt = 10,
        Stun = 11,
        
    }
    [System.Serializable]
    public class UnitRenderProfileData
    {
        [Header("Identity")]
        public string unitID; // VD: "Goblin_Archer"
        [Header("Memory Pool")]
        [Tooltip("Maximum number of active units of this type at the same time.")]
        public int maxCapacity = 1000; // Default is 1000
        [Header("Logic Configuration")]

        [Header("Visual Resources")]
        public Mesh mesh;
        public Material baseMaterial;
        public UnitAnimData animData;

        public float baseMoveSpeed = 5.0f;
        public float baseAttackSpeed = 1.0f;

        [Header("Health Bar")]
        [Tooltip("Enable to render a health bar above this unit type. Disable for projectiles, FX, etc.")]
        public bool showHealthBar = true;
        public int GetAnimIndex(UnitAnimState state)
        {
            var animIndex = animData.animations.FindIndex(i => i.animState == state);
            if (animIndex >= 0) return animData.animations[animIndex].startFrame;
            return 0;
        }
    }
    [CreateAssetMenu(fileName = "UnitsDatabase", menuName = "Abel/Units Database")]
    public class UnitRenderDatabase : ScriptableObject
    {
        // Store the list of prefixes directly in the database so it saves across sessions
        public List<string> definedPrefixes = new List<string> { "unit_", "bullet_", "effect_", "weapon_" };
        public List<UnitRenderProfileData> units = new List<UnitRenderProfileData>();

        public UnitRenderProfileData GetUnitByID(string id)
        {
            return units.Find(u => u.unitID == id);
        }
    }
}