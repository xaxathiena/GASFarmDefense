using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
namespace Abel.GAS.Sample
{
    /// <summary>
    /// Character AI states
    /// </summary>
    public enum ECharacterState
    {
        Idle,           // Doing nothing
        SearchTarget,   // Looking for enemies
        NormalAttack,   // Using normal attack
        UseSkill,       // Using skill ability
        Dead            // Character is dead
    }
}


