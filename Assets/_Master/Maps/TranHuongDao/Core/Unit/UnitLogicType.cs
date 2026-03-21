namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Defines the internal logic type for a unit.
    /// Used as a key for the UnitLogicFactory and provides a dropdown in the Inspector.
    /// </summary>
    public enum EUnitLogicType : byte
    {
        /// <summary>Default - No specific logic.</summary>
        None = 0,
        
        /// <summary>Stationary unit that attacks targets within range (Towers).</summary>
        StationaryAttack = 1,
        
        /// <summary>Unit that follows a predefined path (Enemies).</summary>
        PathFollower = 2,
        
        /// <summary>Moves towards the closest enemy and triggers an effect upon contact/range (Summoned Exploders).</summary>
        HomingSuicide = 3,
        
        /// <summary>Stays near the owner and follows its position (Summoned Guardians).</summary>
        PetFollower = 4,

        /// <summary>Logic defined entirely via custom scripts or external handlers.</summary>
        Custom = 100
    }
}
