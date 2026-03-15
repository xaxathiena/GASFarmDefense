using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core.Abilities
{
    /// <summary>
    /// Behavior for TD_SummonAbilityData.
    /// spawns minions via MinionManager.
    /// </summary>
    public class TD_SummonAbilityBehaviour : IAbilityBehaviour
    {
        private readonly MinionManager _minionManager;

        public TD_SummonAbilityBehaviour(MinionManager minionManager)
        {
            _minionManager = minionManager;
        }

        public bool CanActivate(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            return data is TD_SummonAbilityData;
        }

        public void OnActivated(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var summonData = data as TD_SummonAbilityData;
            if (summonData == null) return;

            Vector3 basePos = asc.Position;
            
            for (int i = 0; i < summonData.count; i++)
            {
                // Simple radial spawning if multiple
                Vector3 offset = summonData.count > 1 
                    ? Quaternion.Euler(0, (360f / summonData.count) * i, 0) * summonData.spawnOffset 
                    : summonData.spawnOffset;
                
                Vector3 spawnPos = basePos + offset;
                _minionManager.SpawnMinion(summonData.unitID, spawnPos, summonData.overrideLogic);
            }
        }

        public void OnEnded(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
        public void OnCancelled(GameplayAbilityData data, AbilitySystemComponent asc, GameplayAbilitySpec spec) { }
    }
}
