using GAS;
using UnityEngine;

namespace FD
{
    public class TowerView: MonoBehaviour, IAbilitySystemComponent
    {
        public AbilitySystemComponent ownerASC;
        public AbilitySystemComponent AbilitySystemComponent => ownerASC;
    }
}