using GAS;
using UnityEngine;

namespace FD
{
    public class TowerView: MonoBehaviour, IAbilitySystemComponent, IGASAvatar
    {
        public AbilitySystemComponent ownerASC;
        public AbilitySystemComponent AbilitySystemComponent => ownerASC;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        public Vector3 Scale => transform.localScale;

        public bool IsValid => true;
    }
}