using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;
using GASFarmDefense.UIToolkit.Core;
using GASFarmDefense.UIToolkit.Core.Animations;
using Abel.TranHuongDao.Core;
using GASFarmDefense.UIToolkit.TranHuongDao;
using Cysharp.Threading.Tasks;

namespace GASFarmDefense.UIToolkit.Core
{
    /// <summary>
    /// Initializes map-specific UI (HUD and Popups) using a local UIDocument.
    /// Supports both standalone and integrated game modes.
    /// </summary>
    public class MapUISetupProvider : IStartable
    {
        private readonly ViewManager _viewManager;
        private readonly PopupManager _popupManager;
        private readonly IObjectResolver _resolver;
        private readonly VisualTreeAsset _hudUxml;
        private readonly List<MapPopupConfig> _mapPopups;

        public MapUISetupProvider(
            ViewManager viewManager,
            PopupManager popupManager,
            IObjectResolver resolver,
            VisualTreeAsset hudUxml,
            List<MapPopupConfig> mapPopups)
        {
            _viewManager = viewManager;
            _popupManager = popupManager;
            _resolver = resolver;
            _hudUxml = hudUxml;
            _mapPopups = mapPopups;
        }

        public void Start()
        {
            // 1. Find or Setup UIDocument
            var uiDoc = GameObject.FindFirstObjectByType<UIDocument>();
            if (uiDoc == null)
            {
                Debug.LogWarning("[MapUISetupProvider] No UIDocument found in scene. Creating a temporary one for standalone testing.");
                var go = new GameObject("StandaloneUIDocument");
                uiDoc = go.AddComponent<UIDocument>();
                uiDoc.panelSettings = Resources.Load<PanelSettings>("UI/DefaultPanelSettings"); // Adjust path as needed
            }

            var root = uiDoc.rootVisualElement;

            // 2. Setup Containers if they don't exist
            var viewContainer = root.Q<VisualElement>("ViewContainer");
            if (viewContainer == null)
            {
                viewContainer = new VisualElement { name = "ViewContainer" };
                viewContainer.style.flexGrow = 1;
                root.Add(viewContainer);
            }

            var popupContainer = root.Q<VisualElement>("PopupContainer");
            if (popupContainer == null)
            {
                popupContainer = new VisualElement { name = "PopupContainer" };
                popupContainer.style.position = Position.Absolute;
                popupContainer.style.width = Length.Percent(100);
                popupContainer.style.height = Length.Percent(100);
                popupContainer.pickingMode = PickingMode.Ignore;
                root.Add(popupContainer);
            }

            // 3. Initialize Managers
            _viewManager.Init(viewContainer);
            _popupManager.Init(popupContainer);

            // 4. Register HUD
            if (_hudUxml != null)
            {
                // Note: We use RandomFarmTDView specifically for now. 
                // In a more generic system, we could pass the view type in config.
                var hud = new RandomFarmTDView();
                _resolver.Inject(hud);
                hud.Initialize(_hudUxml);
                hud.UIAnimation = new UIFadeAnimation();
                _viewManager.RegisterView(hud);
                
                // Show HUD immediately
                _viewManager.SwitchView<RandomFarmTDView>().Forget();
            }

            // 5. Register Popups
            foreach (var config in _mapPopups)
            {
                if (config.Uxml == null) continue;

                PopupBase popupInstance = CreatePopupInstance(config);
                if (popupInstance != null)
                {
                    _resolver.Inject(popupInstance);
                    popupInstance.Initialize(config.Uxml);
                    popupInstance.UIAnimation = new UIFadeAnimation();
                    _popupManager.RegisterPopup(config.PopupId, popupInstance);
                }
            }
        }

        private PopupBase CreatePopupInstance(MapPopupConfig config)
        {
            // Use specific logic based on PopupId or ClassName
            switch (config.PopupId)
            {
                case "Settings": return new FDSettingsPopup();
                case "Shop": return new ShopPopup();
                case "Market": return new MarketPopup();
                case "Farm": return new FarmPopup();
                case "Guide": return new GuidePopup();
                default:
                    if (!string.IsNullOrEmpty(config.PopupClassName))
                    {
                        Type type = Type.GetType(config.PopupClassName);
                        if (type != null && typeof(PopupBase).IsAssignableFrom(type))
                        {
                            return (PopupBase)Activator.CreateInstance(type);
                        }
                    }
                    return new GuidePopup(); // Fallback
            }
        }
    }
}
