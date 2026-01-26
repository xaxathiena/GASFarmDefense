using FD.Ability;
using UnityEngine;

namespace  GAS
{


    public static class AbilitySystemExtensions
    {
        // Hàm mở rộng giúp lấy ASC nhanh và tiện
        public static AbilitySystemComponent GetAbilitySystemComponent(this GameObject target)
        {
            if (target == null) return null;

            if (target.TryGetComponent(out AbilitySystemComponent directAsc))
            {
                return directAsc;
            }

            if (target.TryGetComponent(out IAbilitySystemComponent interfaceAsc))
            {
                return interfaceAsc.AbilitySystemComponent;
            }

            return null;
        }
        public static AbilitySystemComponent GetAbilitySystemComponent(this Transform target)
        {
            if (target == null) return null;

            if (target.TryGetComponent(out AbilitySystemComponent directAsc))
            {
                return directAsc;
            }
            if (target.TryGetComponent(out IAbilitySystemComponent interfaceAsc))
            {
                return interfaceAsc.AbilitySystemComponent;
            }

            return null;
        }
    }
}