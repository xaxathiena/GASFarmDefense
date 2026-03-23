using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    /// <summary>
    /// Base Entry point for UI Toolkit framework. 
    /// Inherit this class in your specific game to register views and popups.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIManager : MonoBehaviour
    {
        [SerializeField] protected UIDocument _uiDocument;

        [VContainer.Inject] public ViewManager ViewManager { get; set; }
        [VContainer.Inject] public PopupManager PopupManager { get; set; }
        public VisualElement BackgroundContainer { get; private set; }
        public VisualElement TopBarContainer { get; private set; }

        protected virtual void Awake()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();

            var root = _uiDocument.rootVisualElement;

            // 1. Setup Background Container (Bottom layer)
            BackgroundContainer = new VisualElement { name = "BackgroundContainer" };
            BackgroundContainer.style.position = Position.Absolute;
            BackgroundContainer.style.top = 0;
            BackgroundContainer.style.left = 0;
            BackgroundContainer.style.right = 0;
            BackgroundContainer.style.bottom = 0;
            BackgroundContainer.pickingMode = PickingMode.Ignore;
            root.Add(BackgroundContainer);

            // 2. Setup View Container
            var viewContainer = new VisualElement { name = "ViewContainer" };
            viewContainer.style.flexGrow = 1;
            viewContainer.style.width = new Length(100, LengthUnit.Percent);
            viewContainer.style.height = new Length(100, LengthUnit.Percent);
            root.Add(viewContainer);

            // 3. Setup TopBar Container (Always on top of views)
            TopBarContainer = new VisualElement { name = "TopBarContainer" };
            TopBarContainer.style.position = Position.Absolute;
            TopBarContainer.style.top = 0;
            TopBarContainer.style.left = 0;
            TopBarContainer.style.right = 0;
            TopBarContainer.style.bottom = 0;
            TopBarContainer.pickingMode = PickingMode.Ignore;
            root.Add(TopBarContainer);

            // 4. Setup Popup Container (Topmost layer)
            var popupContainer = new VisualElement { name = "PopupContainer" };
            popupContainer.style.flexGrow = 1;
            popupContainer.style.position = Position.Absolute;
            popupContainer.style.top = 0;
            popupContainer.style.bottom = 0;
            popupContainer.style.left = 0;
            popupContainer.style.right = 0;
            popupContainer.pickingMode = PickingMode.Ignore;
            root.Add(popupContainer);

            // Use injected managers or create new ones if not provided
            ViewManager ??= new ViewManager();
            ViewManager.Init(viewContainer);

            PopupManager ??= new PopupManager();
            PopupManager.Init(popupContainer);

            RegisterCoreUI();
        }

        public virtual void SetGlobalUIActive(bool active)
        {
            var style = active ? DisplayStyle.Flex : DisplayStyle.None;
            if (BackgroundContainer != null) BackgroundContainer.style.display = style;
            if (TopBarContainer != null) TopBarContainer.style.display = style;
        }

        /// <summary>
        /// Override this method to register specific views and popups for the game.
        /// </summary>
        protected abstract void RegisterCoreUI();
    }
}
