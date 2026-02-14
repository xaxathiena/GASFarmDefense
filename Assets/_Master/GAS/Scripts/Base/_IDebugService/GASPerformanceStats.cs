using System.Collections.Generic;

/// <summary>
/// GAS Performance profiling stats - Deep profiler cho Gameplay Ability System
/// </summary>
public readonly struct GASPerformanceStats
{
    // Core metrics
    public readonly int TotalASCCount;
    public readonly int TotalActiveEffects;
    public readonly int EffectsAppliedThisFrame;
    public readonly int EffectsRemovedThisFrame;
    public readonly int PeriodicEffectTicks;
    
    // Abilities
    public readonly int AbilityActivationsThisFrame;
    public readonly int FailedActivations;
    public readonly int TotalAbilitiesOnCooldown;
    public readonly int TotalGrantedAbilities;
    
    // Attributes
    public readonly int AttributeModsThisFrame;
    public readonly int AttributeCallbacksTriggered;
    
    // Performance (ms)
    public readonly float TotalASCTickTimeMs;
    public readonly float EffectProcessingTimeMs;
    public readonly float AttributeProcessingTimeMs;
    
    // Memory (bytes)
    public readonly long TotalMemoryUsage;
    public readonly int GCAllocationsKB;
    
    // Per-entity breakdown (top 5 slowest)
    public readonly List<EntityPerformance> TopSlowEntities;
    
    public GASPerformanceStats(
        int totalASC,
        int totalActiveEffects,
        int effectsApplied,
        int effectsRemoved,
        int periodicTicks,
        int abilityActivations,
        int failedActivations,
        int abilitiesOnCD,
        int grantedAbilities,
        int attributeMods,
        int attributeCallbacks,
        float ascTickTime,
        float effectProcessing,
        float attributeProcessing,
        long memoryUsage,
        int gcAlloc,
        List<EntityPerformance> topSlow)
    {
        TotalASCCount = totalASC;
        TotalActiveEffects = totalActiveEffects;
        EffectsAppliedThisFrame = effectsApplied;
        EffectsRemovedThisFrame = effectsRemoved;
        PeriodicEffectTicks = periodicTicks;
        AbilityActivationsThisFrame = abilityActivations;
        FailedActivations = failedActivations;
        TotalAbilitiesOnCooldown = abilitiesOnCD;
        TotalGrantedAbilities = grantedAbilities;
        AttributeModsThisFrame = attributeMods;
        AttributeCallbacksTriggered = attributeCallbacks;
        TotalASCTickTimeMs = ascTickTime;
        EffectProcessingTimeMs = effectProcessing;
        AttributeProcessingTimeMs = attributeProcessing;
        TotalMemoryUsage = memoryUsage;
        GCAllocationsKB = gcAlloc;
        TopSlowEntities = topSlow;
    }
    
    // Thresholds cho warning levels
    public enum PerformanceLevel
    {
        Good,
        Warning,
        Critical
    }
    
    public PerformanceLevel GetEffectsLevel()
    {
        if (TotalActiveEffects > 500) return PerformanceLevel.Critical;
        if (TotalActiveEffects > 200) return PerformanceLevel.Warning;
        return PerformanceLevel.Good;
    }
    
    public PerformanceLevel GetTickTimeLevel()
    {
        if (TotalASCTickTimeMs > 3f) return PerformanceLevel.Critical;
        if (TotalASCTickTimeMs > 1f) return PerformanceLevel.Warning;
        return PerformanceLevel.Good;
    }
    
    public PerformanceLevel GetGCLevel()
    {
        if (GCAllocationsKB > 20) return PerformanceLevel.Critical;
        if (GCAllocationsKB > 5) return PerformanceLevel.Warning;
        return PerformanceLevel.Good;
    }
}

/// <summary>
/// Performance data cho từng entity (tower/enemy)
/// </summary>
public struct EntityPerformance
{
    public string EntityName;
    public string EntityType; // "Tower" or "Enemy"
    public float TickTimeMs;
    public int ActiveEffects;
    public int AbilitiesOnCD;
    
    public EntityPerformance(string name, string type, float tickTime, int effects, int cooldowns)
    {
        EntityName = name;
        EntityType = type;
        TickTimeMs = tickTime;
        ActiveEffects = effects;
        AbilitiesOnCD = cooldowns;
    }
}
