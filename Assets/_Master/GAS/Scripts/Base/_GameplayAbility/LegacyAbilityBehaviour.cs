namespace GAS
{
    /// <summary>
    /// Legacy behaviour adapter for old GameplayAbility subclasses.
    /// This allows old abilities that override virtual methods to continue working.
    /// </summary>
    public class LegacyAbilityBehaviour : IAbilityBehaviour
    {
        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Delegate to base logic - legacy abilities use CanActivateAbility override
            return true; // Will be handled by GameplayAbility.CanActivateAbility
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Legacy abilities handle this in OnAbilityActivated virtual method
            // No-op here as GameplayAbility calls the virtual method
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Legacy abilities handle this in OnAbilityEnded virtual method
            // No-op here as GameplayAbility calls the virtual method
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Legacy abilities handle this in OnAbilityCancelled virtual method
            // No-op here as GameplayAbility calls the virtual method
        }
    }
}
