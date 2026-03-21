using System.Runtime.InteropServices;

namespace Abel.TranHuongDao.Core
{
    // ---------------------------------------------------------------------------
    // NOTE: AttackType and TargetType enums live in UnitConfigData.cs.
    // ---------------------------------------------------------------------------

    // ---------------------------------------------------------------------------
    // UnitCoreStats — immutable snapshot of a tower's base combat parameters.
    //
    // Design rules:
    //   • All fields are primitive value types → struct is fully blittable.
    //   • StructLayout.Sequential + explicit sizes guarantee a stable memory
    //     footprint, safe for NativeArray<UnitCoreStats> and Burst jobs.
    //   • No managed references (string, class, array) are allowed here.
    //   • Runtime-scaled values (e.g. after buffs) live in UnitAttributeSet;
    //     this struct represents only the raw authored baseline.
    // ---------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential)]
    public struct UnitCoreStats
    {
        // -- Damage --------------------------------------------------------------

        /// <summary>Raw damage delivered per attack before resistances are applied.</summary>
        public float baseDamage;

        // -- Timing --------------------------------------------------------------

        /// <summary>
        /// Seconds between two consecutive attacks.
        /// Using a cooldown value (not "attacks-per-second") simplifies countdown timers:
        ///   timer -= deltaTime; if (timer &lt;= 0) { Attack(); timer = attackCooldown; }
        /// </summary>
        public float attackCooldown;

        // -- Spatial -------------------------------------------------------------

        /// <summary>World-space radius within which this unit can acquire a target.</summary>
        public float attackRange;

        /// <summary>
        /// Units-per-second travel speed of the fired projectile.
        /// A value of 0 denotes an instant / hitscan attack (no projectile entity needed).
        /// </summary>
        public float projectileSpeed;

        // -- Classification ------------------------------------------------------

        /// <summary>Determines which damage-type resistances the target applies on hit.</summary>
        public AttackType attackType;

        /// <summary>Bitmask of valid target categories this unit may engage.</summary>
        public TargetType targetType;

        // -- Economy / Progression -----------------------------------------------

        /// <summary>Gold cost to place this unit on the map.</summary>
        public int buildCost;

        /// <summary>
        /// Upgrade tier (1 = base, higher = upgraded forms).
        /// Used by the upgrade system to gate ability unlocks and stat scaling.
        /// </summary>
        public int tier;

        // -- Constructor ---------------------------------------------------------

        /// <summary>
        /// Convenience constructor — allows initialising all fields in a single expression
        /// without relying on mutable object initialiser syntax.
        /// </summary>
        public UnitCoreStats(
            float      baseDamage,
            float      attackCooldown,
            float      attackRange,
            float      projectileSpeed,
            AttackType attackType,
            TargetType targetType,
            int        buildCost,
            int        tier)
        {
            this.baseDamage      = baseDamage;
            this.attackCooldown  = attackCooldown;
            this.attackRange     = attackRange;
            this.projectileSpeed = projectileSpeed;
            this.attackType      = attackType;
            this.targetType      = targetType;
            this.buildCost       = buildCost;
            this.tier            = tier;
        }
    }
}
