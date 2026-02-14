using System.Diagnostics;
using GAS;

/// <summary>
/// Performance profiler cho AbilitySystemComponent
/// Wraps ASC để track performance metrics without modifying core logic
/// </summary>
public class ASCPerformanceProfiler
{
    // Frame counters (reset mỗi frame)
    private int effectsAppliedThisFrame;
    private int effectsRemovedThisFrame;
    private int abilityActivationsThisFrame;
    private int failedActivationsThisFrame;
    private int attributeModsThisFrame;
    private int periodicTicksThisFrame;
    
    // Tick time tracking
    private readonly Stopwatch tickStopwatch = new Stopwatch();
    private float lastTickTimeMs;
    
    // Reference to ASC being profiled
    private readonly AbilitySystemComponent asc;
    private readonly string entityName;
    
    // Previous frame state for delta tracking
    private int previousEffectCount;
    
    public ASCPerformanceProfiler(AbilitySystemComponent asc, string entityName)
    {
        this.asc = asc;
        this.entityName = entityName;
    }
    
    /// <summary>
    /// Gọi trước khi Tick() ASC
    /// </summary>
    public void BeginTick()
    {
        if (!DebugConfig.EnableDebug) return;
        
        tickStopwatch.Restart();
        previousEffectCount = asc.GetActiveGameplayEffects().Count;
    }
    
    /// <summary>
    /// Gọi sau khi Tick() ASC
    /// </summary>
    public void EndTick()
    {
        if (!DebugConfig.EnableDebug) return;
        
        tickStopwatch.Stop();
        lastTickTimeMs = (float)tickStopwatch.Elapsed.TotalMilliseconds;
        
        // Track effect changes
        int currentEffectCount = asc.GetActiveGameplayEffects().Count;
        int delta = currentEffectCount - previousEffectCount;
        
        if (delta > 0)
            effectsAppliedThisFrame += delta;
        else if (delta < 0)
            effectsRemovedThisFrame += -delta;
    }
    
    /// <summary>
    /// Track ability activation
    /// </summary>
    public void RecordAbilityActivation(bool success)
    {
        if (!DebugConfig.EnableDebug) return;
        
        if (success)
            abilityActivationsThisFrame++;
        else
            failedActivationsThisFrame++;
    }
    
    /// <summary>
    /// Track attribute modification
    /// </summary>
    public void RecordAttributeModification()
    {
        if (!DebugConfig.EnableDebug) return;
        attributeModsThisFrame++;
    }
    
    /// <summary>
    /// Track periodic effect tick
    /// </summary>
    public void RecordPeriodicTick()
    {
        if (!DebugConfig.EnableDebug) return;
        periodicTicksThisFrame++;
    }
    
    /// <summary>
    /// Get current performance snapshot
    /// </summary>
    public EntityPerformance GetSnapshot()
    {
        var effects = asc.GetActiveGameplayEffects().Count;
#if UNITY_EDITOR
        var cooldowns = asc.EditorGetAbilityCooldowns();
        int cdCount = 0;
        foreach (var cd in cooldowns)
        {
            if (cd.Value > 0) cdCount++;
        }
#else
        int cdCount = 0;
#endif
        
        return new EntityPerformance(
            entityName,
            "Unknown", // Will be set by caller
            lastTickTimeMs,
            effects,
            cdCount
        );
    }
    
    /// <summary>
    /// Reset frame counters - gọi mỗi frame
    /// </summary>
    public void ResetFrameCounters()
    {
        effectsAppliedThisFrame = 0;
        effectsRemovedThisFrame = 0;
        abilityActivationsThisFrame = 0;
        failedActivationsThisFrame = 0;
        attributeModsThisFrame = 0;
        periodicTicksThisFrame = 0;
    }
    
    // Getters cho frame counters
    public int EffectsAppliedThisFrame => effectsAppliedThisFrame;
    public int EffectsRemovedThisFrame => effectsRemovedThisFrame;
    public int AbilityActivationsThisFrame => abilityActivationsThisFrame;
    public int FailedActivationsThisFrame => failedActivationsThisFrame;
    public int AttributeModsThisFrame => attributeModsThisFrame;
    public int PeriodicTicksThisFrame => periodicTicksThisFrame;
    public float LastTickTimeMs => lastTickTimeMs;
}
