namespace GAS
{
    /// <summary>
    /// Base attribute enum - Each game can define their own enum
    /// Example: public enum MyGameAttributes { Health, Mana, Energy, Shield }
    /// </summary>
    public enum EGameplayAttributeType
    {
        // Primary Attributes
        Health,
        MaxHealth,
        Mana,
        MaxMana,
        ManaRegen,
        Stamina,
        MaxStamina,
        
        // Combat Attributes
        AttackPower,
        Defense,
        MoveSpeed,
        Armor,
        CriticalChance,
        CriticalMultiplier,
        BaseDamage,
        ROF,              // Rate of Fire: attacks per second
        NormalCooldownRate, // Multiplier for normal ability cooldowns
        SkillCooldownRate,  // Multiplier for skill ability cooldowns
        
        // Add more attributes as needed for your game
        // MagicResist,
        // etc...
    }
}
