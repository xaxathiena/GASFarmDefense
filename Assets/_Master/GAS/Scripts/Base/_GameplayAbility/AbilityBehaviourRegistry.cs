using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace GAS
{
    /// <summary>
    /// Registry for ability behaviours. Resolves and caches behaviour instances.
    /// Behaviours are created once and reused (Singleton pattern).
    /// </summary>
    public class AbilityBehaviourRegistry
    {
        private readonly IObjectResolver container;
        private readonly Dictionary<Type, IAbilityBehaviour> behaviourCache = new Dictionary<Type, IAbilityBehaviour>();

        public AbilityBehaviourRegistry(IObjectResolver container)
        {
            this.container = container;
        }

        /// <summary>
        /// Gets the behaviour for a given ability data.
        /// Creates and caches the behaviour on first access.
        /// </summary>
        public IAbilityBehaviour GetBehaviour(GameplayAbilityData data)
        {
            if (data == null)
            {
                Debug.LogError("AbilityData is null!");
                return null;
            }

            var behaviourType = data.GetBehaviourType();
            if (behaviourType == null)
            {
                Debug.LogError($"AbilityData {data.abilityName} returned null behaviour type!");
                return null;
            }

            if (!behaviourCache.TryGetValue(behaviourType, out var behaviour))
            {
                try
                {
                    behaviour = container.Resolve(behaviourType) as IAbilityBehaviour;
                    if (behaviour == null)
                    {
                        Debug.LogError($"Resolved type {behaviourType.Name} is not an IAbilityBehaviour!");
                        return null;
                    }
                    behaviourCache[behaviourType] = behaviour;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to resolve behaviour {behaviourType.Name}: {e.Message}");
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
