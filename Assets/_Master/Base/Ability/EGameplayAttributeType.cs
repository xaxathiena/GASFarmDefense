namespace _Master.Base.Ability
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
        Stamina,
        MaxStamina,
        
        // Combat Attributes
        AttackPower,
        Defense,
        MoveSpeed,
        
        // Add more attributes as needed for your game
        // CriticalChance,
        // CriticalDamage,
        // Armor,
        // MagicResist,
        // etc...
    }
}
