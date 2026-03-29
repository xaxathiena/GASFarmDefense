using System;
using System.Runtime.InteropServices;
using FD.Ability;

namespace Abel.TranHuongDao.Core
{
    // ---------------------------------------------------------------------------
    // Attack damage category — determines which resistances the target applies.
    // Backed by byte to keep UnitConfigData fully blittable and tightly packed.
    // ---------------------------------------------------------------------------
    public enum AttackType : byte
    {
        Normal = 0,
        Magic = 1,
        Piercing = 2,
        Siege = 3,
        Chaos = 4,   // Chaos damage bypasses all resistances.
        Hero = 5,
        Heavy = 6,
    }

    // ---------------------------------------------------------------------------
    // Bitmask of valid target categories.
    // Both == Ground | Air, so a single bitwise AND suffices for filtering.
    // ---------------------------------------------------------------------------
    [Flags]
    public enum TargetType : byte
    {
        Ground = 1 << 0,   // 1
        Air = 1 << 1,   // 2
        Both = Ground | Air,
    }

    // ---------------------------------------------------------------------------
    // UnitConfigData — a fully blittable snapshot of authored balance data for
    // one unit tier. Loaded from CSV / ScriptableObject, then passed into
    // UnitAttributeSet.InitializeFromConfig().
    //
    // Split rationale:
    //   • Float fields → fed into GAS GameplayAttributes (runtime-modifiable).
    //   • Meta fields  → consumed directly by logic/AI; never touched by GAS.
    //
    // Memory layout: StructLayout.Sequential ensures a stable binary footprint
    // that is safe for NativeArray<UnitConfigData> and Burst jobs.
    // ---------------------------------------------------------------------------
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct UnitConfig
    {
        public string UnitID;

        /// <summary>
        /// ID used to look up visual profile in UnitRenderDatabase.
        /// If empty, fall back to UnitID.
        /// </summary>
        public string UnitRenderID;

        /// <summary>
        /// Uniform scale multiplier applied to the rendered unit.
        /// Useful for boss variants or special units.
        /// </summary>
        public float ScaleFactor;

        // ── GAS-mapped float stats ───────────────────────────────────────────────

        /// <summary>Maximum and initial health of the unit.</summary>
        public float MaxHealth;

        /// <summary>World-units per second along the path (0 for stationary towers).</summary>
        public float MoveSpeed;

        /// <summary>Raw damage per hit before target resistances are applied.</summary>
        public float BaseDamage;

        /// <summary>
        /// Rate of Fire: number of attacks per second.
        /// Cooldown is derived as: cooldown = 1.0f / ROF
        /// </summary>
        public float ROF;

        /// <summary>World-space acquisition radius.</summary>
        public float AttackRange;

        /// <summary>
        /// Projectile travel speed in world-units per second.
        /// 0 = instant/hitscan — no projectile entity is spawned.
        /// </summary>
        public float ProjectileSpeed;

        // ── Static meta stats (logic only, not in GAS) ──────────────────────────

        /// <summary>Damage category used for resistance/immunity look-ups.</summary>
        public AttackType AttackType;

        /// <summary>Bitmask of valid target categories (Ground, Air, or Both).</summary>
        public TargetType TargetType;

        // Explicit padding: aligns the struct to a 4-byte boundary after the two
        // byte-sized enum fields so subsequent int fields start at a natural offset.
        private byte _pad0;
        private byte _pad1;

        /// <summary>Gold cost to place this unit.</summary>
        public int BuildCost;
        public int Armor;

        /// <summary>Upgrade tier: 1 = base form, higher values = upgraded forms.</summary>
        public int Tier;

        /// <summary>Armor category for resistance calculation.</summary>
        public EArmorType ArmorType;

        // ── Ability IDs ─────────────────────────────────────────────────────────

        /// <summary>
        /// Asset name of the attack ability in AbilitiesConfig (e.g. "TD_TowerNormalAttack").
        /// TowerManager looks this up at spawn time. Leave empty for units with no basic attack.
        /// </summary>
        public string AttackAbilityID;

        /// <summary>
        /// Asset name of the skill ability in AbilitiesConfig.
        /// Leave empty for units with no skill.
        /// </summary>
        public string SkillAbilityID;

        // ── Constructor ─────────────────────────────────────────────────────────

        public UnitConfig(
            string unitID,
            float maxHealth,
            float moveSpeed,
            float baseDamage,
            float rof,
            float attackRange,
            float projectileSpeed,
            AttackType attackType,
            TargetType targetType,
            int armor,
            int buildCost,
            int tier,
            EArmorType armorType,
            string attackAbilityID = "",
            string skillAbilityID = "",
            string unitRenderID = "",
            float scaleFactor = 1.0f)
        {
            UnitID = unitID;
            UnitRenderID = string.IsNullOrEmpty(unitRenderID) ? unitID : unitRenderID;
            ScaleFactor = scaleFactor;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            BaseDamage = baseDamage;
            ROF = rof;
            AttackRange = attackRange;
            ProjectileSpeed = projectileSpeed;
            AttackType = attackType;
            TargetType = targetType;
            BuildCost = buildCost;
            Armor = armor;
            Tier = tier;
            ArmorType = armorType;
            AttackAbilityID = attackAbilityID;
            SkillAbilityID = skillAbilityID;
            _pad0 = 0;
            _pad1 = 0;
        }
    }
}
