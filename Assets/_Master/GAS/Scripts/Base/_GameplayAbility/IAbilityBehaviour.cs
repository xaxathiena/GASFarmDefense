namespace GAS
{
    /// <summary>
    /// Interface for ability behaviour implementations.
    /// All ability logic is implemented through this interface.
    /// Behaviours are registered as Singletons in VContainer and shared across all instances.
    /// </summary>
    public interface IAbilityBehaviour
    {
        /// <summary>
        /// Check if the ability can be activated.
        /// Override to add custom activation conditions.
        /// </summary>
        bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec);

        /// <summary>
        /// Called when the ability is activated.
        /// Implement custom ability logic here (spawn projectiles, apply effects, etc.).
        /// </summary>
        void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec);

        /// <summary>
        /// Called when the ability ends normally.
        /// Clean up any ability-specific state here.
        /// </summary>
        void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec);

        /// <summary>
        /// Called when the ability is cancelled.
        /// Handle cancellation logic here (interrupt animations, refund partial costs, etc.).
        /// </summary>
        void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec);
    }
}
