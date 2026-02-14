using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace GAS
{
    /// <summary>
    /// Registry for ability behaviours. Manages type mappings and caches behaviour instances.
    /// Behaviours are created once and reused (Singleton pattern).
    /// This is the ONLY place where ability type mappings are stored.
    /// </summary>
    public class AbilityBehaviourRegistry
    {
        private readonly IObjectResolver container;
        private readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type>(); // Use FullName as key for reliable comparison
        private readonly Dictionary<Type, IAbilityBehaviour> behaviourCache = new Dictionary<Type, IAbilityBehaviour>();

        public AbilityBehaviourRegistry(IObjectResolver container)
        {
            this.container = container;
        }
        
        /// <summary>
        /// Register a mapping between data type and behaviour type.
        /// REQUIRED for all abilities to work.
        /// </summary>
        public void RegisterBehaviourType(Type dataType, Type behaviourType)
        {
            var key = dataType.FullName;
            typeMap[key] = behaviourType;
        }

        /// <summary>
        /// Gets the behaviour for a given ability data.
        /// Creates and caches the behaviour on first access.
        /// </summary>
        public IAbilityBehaviour GetBehaviour(GameplayAbilityData data)
        {
            if (data == null)
            {
                //Debug.LogError("AbilityData is null!");
                return null;
            }

            var dataType = data.GetType();
            var key = dataType.FullName;
            
            // Debug.Log($"[AbilityBehaviourRegistry] Looking up behaviour for {data.abilityName} (Type: {key})");
            
            // Check if we have an explicit mapping
            if (!typeMap.TryGetValue(key, out Type behaviourType))
            {                
                // Auto-detect by convention (DataName -> DataNameBehaviour)
                string dataTypeName = dataType.Name;
                if (dataTypeName.EndsWith("Data"))
                {
                    string behaviourTypeName = dataTypeName.Replace("Data", "Behaviour");
                    behaviourType = Type.GetType($"{dataType.Namespace}.{behaviourTypeName}");
                    if (behaviourType != null)
                    {
                        typeMap[key] = behaviourType; // Cache it
                        //Log($"[AbilityBehaviourRegistry] Auto-detected: {behaviourTypeName}");
                    }
                }
            }
            else
            {
                //Debug.Log($"[AbilityBehaviourRegistry] Found mapping: {behaviourType.Name}");
            }
            
            if (behaviourType == null)
            {
                return null;
            }

            if (!behaviourCache.TryGetValue(behaviourType, out var behaviour))
            {
                try
                {
                    behaviour = container.Resolve(behaviourType) as IAbilityBehaviour;
                    if (behaviour == null)
                    {
                        //Debug.LogError($"Resolved type {behaviourType.Name} is not an IAbilityBehaviour!");
                        return null;
                    }
                    behaviourCache[behaviourType] = behaviour;
                }
                catch (Exception e)
                {
                    //Debug.LogError($"Failed to resolve behaviour {behaviourType.Name}: {e.Message}");
                    return null;
                }
            }

            return behaviour;
        }

        /// <summary>
        /// Clears the behaviour cache (useful for hot-reload scenarios).
        /// </summary>
        public void ClearCache()
        {
            behaviourCache.Clear();
        }
    }
}
