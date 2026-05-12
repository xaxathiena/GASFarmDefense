using Abel.GAS.Abilities;
using Abel.GAS.Effects;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat
{
    [CreateAssetMenu(menuName = "GAS/Samples/OrbAbilityData")]
    public class OrbAbilityData : GameplayAbilityData
    {
        [Header("Orb Combat Extensions")]
        public GameplayEffect[] effectsToApply;
    }
}
