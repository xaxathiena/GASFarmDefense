namespace FD.Ability
{
    /// <summary>
    /// Armor types for Farm Defense game (based on Warcraft 3 system)
    /// Each character/unit will have one of these armor types.
    /// </summary>
    public enum EArmorType
    {
        /// <summary>
        /// Light armor - Weak to Pierce (200% damage), Normal vs others
        /// Typical: Scouts, flying units, light infantry
        /// </summary>
        Light,
        
        /// <summary>
        /// Medium armor - Weak to Normal (150%), Resists Magic (75%)
        /// Typical: Standard units, infantry
        /// </summary>
        Medium,
        
        /// <summary>
        /// Heavy armor - Weak to Magic (200%), Resists Pierce (35%)
        /// Typical: Knights, heavy units, tanks
        /// </summary>
        Heavy,
        
        /// <summary>
        /// Fortified armor - Weak to Siege (150%), Resists Normal/Pierce/Hero (50%, 35%, 50%)
        /// Typical: Buildings, structures, boss units
        /// </summary>
        Fortified,
        
        /// <summary>
        /// Hero armor - Weak to Siege (50%), Normal vs others
        /// Typical: Hero units, champions
        /// </summary>
        Hero,
        
        /// <summary>
        /// Unarmored - Weak to Pierce (200%), Normal vs others
        /// Typical: Workers, civilians, summoned units
        /// </summary>
        Unarmored,
        
        /// <summary>
        /// Magic Immune armor - Blocks Magic damage (0%), Normal vs others
        /// Typical: Magic-resistant bosses, special units
        /// </summary>
        MagicImmune
    }
}
