namespace GAS
{
    /// <summary>
    /// Gameplay tags for ability system (byte-based, 0-255)
    /// Each game can define its own set of tags
    /// </summary>
    public enum GameplayTag : byte
    {
        None = 0,
        
        // State Tags - Character States
        State_Stunned = 1,
        State_Dead = 2,
        State_Immune = 3,
        State_Immune_CC = 4,
        State_Immune_Stun = 5,
        State_Disabled = 6,
        State_Silenced = 7,
        State_Invulnerable = 8,
        State_Buffed = 9,
        State_CannotMove = 10,
        State_CannotAttack = 11,
        
        // Elemental State Tags
        State_Burning = 12,
        State_Shocked = 13,
        State_Wet = 14,
        State_Frozen = 15,
        State_Poisoned = 16,
        
        // Buff Tags
        Buff_Speed = 20,
        Buff_Attack = 21,
        Buff_Stamina = 22,
        Buff_Defense = 23,
        
        // Debuff Tags
        Debuff_Poison = 30,
        Debuff_DefenseBreak = 31,
        Debuff_Slow = 32,
        
        // Ability Tags
        Ability_Attack = 40,
        Ability_Defense = 41,
        Ability_Magic = 42,
        
        // Reserve space for game-specific tags (100-255)
        Custom_Start = 100
    }
}
