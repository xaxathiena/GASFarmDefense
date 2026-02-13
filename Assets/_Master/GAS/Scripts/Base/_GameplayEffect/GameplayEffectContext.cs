namespace GAS
{
    /// <summary>
    /// Base context for gameplay effect execution.
    /// This is a minimal base class that games can extend with their own specific data.
    /// Each game should create their own context class inheriting from this.
    /// Example: FDGameplayEffectContext, MyGameEffectContext, etc.
    /// </summary>
    public class GameplayEffectContext
    {
        /// <summary>
        /// The AbilitySystemComponent that is the source of the effect (caster/attacker)
        /// </summary>
        public AbilitySystemComponent SourceASC { get; set; }
        
        /// <summary>
        /// The AbilitySystemComponent that is the target of the effect (receiver/defender)
        /// </summary>
        public AbilitySystemComponent TargetASC { get; set; }
        
        /// <summary>
        /// The ability that spawned this effect (optional)
        /// </summary>
        public GameplayAbilityData SourceAbility { get; set; }
        
        /// <summary>
        /// Level of the effect or ability
        /// </summary>
        public float Level { get; set; } = 1f;
        
        /// <summary>
        /// Stack count for stacking effects
        /// </summary>
        public float StackCount { get; set; } = 1f;
        
        /// <summary>
        /// Static/thread-local context for current calculation
        /// Used to pass context through the calculation pipeline
        /// </summary>
        [System.ThreadStatic]
        public static GameplayEffectContext Current;
        
        /// <summary>
        /// Set this context as the current context
        /// </summary>
        public void MakeCurrent()
        {
            Current = this;
        }
        
        /// <summary>
        /// Clear the current context
        /// </summary>
        public static void ClearCurrent()
        {
            Current = null;
        }
    }
}
