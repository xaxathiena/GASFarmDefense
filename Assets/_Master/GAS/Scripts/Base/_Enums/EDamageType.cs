namespace FD.Ability
{
    /// <summary>
    /// Attack/Damage types for Farm Defense game (based on Warcraft 3 system)
    /// Each ability will have one of these damage types.
    /// </summary>
    public enum EDamageType
    {
        /// <summary>
        /// Normal attack - Good vs Medium (150%), Bad vs Fortified (50%)
        /// </summary>
        Normal,
        
        /// <summary>
        /// Piercing attack - Excellent vs Light/Unarmored (200%), Very bad vs Heavy/Fortified (35%)
        /// Typical: Archers, ranged towers
        /// </summary>
        Pierce,
        
        /// <summary>
        /// Siege attack - Excellent vs Fortified (150%), Bad vs Light/Hero (50%)
        /// Typical: Catapults, siege weapons
        /// </summary>
        Siege,
        
        /// <summary>
        /// Magic attack - Excellent vs Heavy (200%), Bad vs Medium (75%), Blocked by MagicImmune (0%)
        /// Typical: Mages, spell casters
        /// </summary>
        Magic,
        
        /// <summary>
        /// Chaos attack - Always 100% damage vs ALL armor types (no weakness, no strength)
        /// Typical: Ultimate towers, legendary units
        /// </summary>
        Chaos,
        
        /// <summary>
        /// Hero attack - 100% vs most, 50% vs Fortified
        /// Typical: Hero units, champion towers
        /// </summary>
        Hero
    }
}
