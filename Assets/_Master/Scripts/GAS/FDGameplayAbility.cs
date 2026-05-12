using FD.Ability;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;
using Abel.GAS;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// DEPRECATED: FDGameplayAbility is obsolete.
    /// Use GameplayAbilityData + IAbilityBehaviour pattern instead.
    /// This class is kept for asset compatibility only.
    /// </summary>
    [System.Obsolete("Use GameplayAbilityData + IAbilityBehaviour pattern")]
    [CreateAssetMenu(fileName = "FDGameplayAbility", menuName = "Map/RandomFarmTD/Abilities/FD Gameplay Ability (Deprecated)")]
    public class FDGameplayAbility : GameplayAbilityData
    {
        [Header("FD Damage Configuration")]
        [Tooltip("Damage type of this ability (Pierce, Magic, Chaos, etc.)")]
        public EDamageType damageType = EDamageType.Normal;
    }
}


