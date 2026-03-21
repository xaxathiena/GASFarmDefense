using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public abstract class UIComponentBase
    {
        public VisualElement RootElement { get; private set; }
        public UIAnimationBase UIAnimation { get; set; }

        public virtual void Initialize(VisualTreeAsset asset)
        {
            RootElement = asset.Instantiate();
            RootElement.style.flexGrow = 1;
            RootElement.style.display = DisplayStyle.None; // Hide by default
            
            OnSetup();
        }

        /// <summary>
        /// Called once after the VisualElement is created from the UXML.
        /// Use this to bind elements using RootElement.Q<T>().
        /// </summary>
        protected virtual void OnSetup() { }

        /// <summary>
        /// Called right before the view/popup is shown and the show animation plays.
        /// Use this to update dynamic data.
        /// </summary>
        protected virtual void OnBeforeShow() { }

        /// <summary>
        /// Called right after the view/popup is hidden and the hide animation finishes.
        /// </summary>
        protected virtual void OnAfterHide() { }

        // Internally called by ViewBase/PopupBase
        internal void InvokeBeforeShow() => OnBeforeShow();
        internal void InvokeAfterHide() => OnAfterHide();
    }
}
