using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public abstract class PopupBase : UIComponentBase
    {
        public virtual async UniTask Show()
        {
            RootElement.style.display = DisplayStyle.Flex;
            InvokeBeforeShow();

            if (UIAnimation != null)
            {
                await UIAnimation.PlayShowAnimation(RootElement);
            }
        }

        public virtual async UniTask Hide()
        {
            if (UIAnimation != null)
            {
                await UIAnimation.PlayHideAnimation(RootElement);
            }

            RootElement.style.display = DisplayStyle.None;
            InvokeAfterHide();
        }
    }
}
