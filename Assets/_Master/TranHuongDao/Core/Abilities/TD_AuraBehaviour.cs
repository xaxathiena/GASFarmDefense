using System.Collections.Generic;
using UnityEngine;
using VContainer;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    public class TD_AuraBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        
        // Maps the ability spec to the instantiated runtime tracker
        private readonly Dictionary<GameplayAbilitySpec, TD_AuraInstance> _instances = new Dictionary<GameplayAbilitySpec, TD_AuraInstance>();

        [Inject]
        public TD_AuraBehaviour(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            // Auras can always be activated
            return true;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var auraData = data as TD_AuraData;
            if (auraData == null || asc?.GetOwner() == null) return;

            // Create invisible component on the tower to run Update logic cleanly
            var instance = asc.GetOwner().gameObject.AddComponent<TD_AuraInstance>();
            instance.Initialize(asc, auraData, _enemyManager);
            
            _instances[spec] = instance;
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            if (_instances.TryGetValue(spec, out var instance))
            {
                if (instance != null) Object.Destroy(instance);
                _instances.Remove(spec);
            }
        }

        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            OnEnded(data, asc, spec);
        }
    }
}
