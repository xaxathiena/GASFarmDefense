using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core.Animations
{
    public class UIFadeAnimation : UIAnimationBase
    {
        private float _durationMs;
        private string _hiddenClassName;

        public UIFadeAnimation(float durationSeconds = 0.3f, string hiddenClassName = "ui-hidden")
        {
            _durationMs = durationSeconds * 1000f;
            _hiddenClassName = hiddenClassName;
        }

        public override async UniTask PlayShowAnimation(VisualElement target)
        {
            // Remove the hidden class to trigger the USS transition
            target.RemoveFromClassList(_hiddenClassName);
            await UniTask.Delay((int)_durationMs, ignoreTimeScale: true);
        }

        public override async UniTask PlayHideAnimation(VisualElement target)
        {
            // Add the hidden class to trigger the USS transition
            target.AddToClassList(_hiddenClassName);
            await UniTask.Delay((int)_durationMs, ignoreTimeScale: true);
        }
    }
}
