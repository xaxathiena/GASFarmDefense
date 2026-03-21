using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public abstract class UIAnimationBase
    {
        public abstract UniTask PlayShowAnimation(VisualElement target);
        public abstract UniTask PlayHideAnimation(VisualElement target);
    }
}
