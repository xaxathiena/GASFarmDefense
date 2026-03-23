using System;
using System.Collections.Generic;
using Abel.TranHuongDao.Core;
using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using GASFarmDefense.UIToolkit.Core.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    /// <summary>
    /// Singleton Manager for the specific game.
    /// This inherits from UIManager which handles the View/Popup containers.
    /// </summary>
    public class GameUIManager : UIManager
    {
        [Header("UXML Assets")]
        [SerializeField] private VisualTreeAsset _backgroundUxml;
        [SerializeField] private VisualTreeAsset _topBarUxml;
        [SerializeField] private VisualTreeAsset _mainMenuUxml;
        [SerializeField] private VisualTreeAsset _mapSelectionUxml;
        [SerializeField] private VisualTreeAsset _loadingScreenUxml;
        [SerializeField] private VisualTreeAsset _settingsPopupUxml;

        protected override void Awake()
        {
            base.Awake(); // Sets up Root, Containers, Managers using injected dependencies

            // 0. Setup Global Background
            if (_backgroundUxml != null && BackgroundContainer != null)
            {
                var bg = _backgroundUxml.Instantiate();
                bg.style.flexGrow = 1;
                BackgroundContainer.Add(bg);

                // Track layers for parallax
                _layerBg = bg.Q<VisualElement>("layer-bg");
                _layerMid = bg.Q<VisualElement>("layer-mid");
                _layerFront = bg.Q<VisualElement>("layer-front");

                // Listen for pointer move on root for global parallax
                _uiDocument.rootVisualElement.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            }

            // 0.1 Setup Global TopBar
            if (_topBarUxml != null && TopBarContainer != null)
            {
                var topBar = _topBarUxml.Instantiate();
                topBar.pickingMode = PickingMode.Ignore; // Don't block background clicks
                TopBarContainer.Add(topBar);
            }
            ViewManager.SwitchView<MainMenuView>().Forget();
        }

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }


        /// <summary>
        /// Registers all the specific global views for the project.
        /// </summary>
        protected override void RegisterCoreUI()
        {
            if (_resolver == null || ViewManager == null) return;

            // 1. Global Views
            RegisterGlobalView<MainMenuView>(_mainMenuUxml);
            RegisterGlobalView<MapSelectionView>(_mapSelectionUxml);
            RegisterGlobalView<LoadingScreenView>(_loadingScreenUxml);

            // 2. Default Popups
            RegisterDefaultPopups();
        }

        private void RegisterGlobalView<T>(VisualTreeAsset uxml) where T : ViewBase, new()
        {
            if (uxml == null) return;
            var view = new T();
            _resolver.Inject(view);
            view.Initialize(uxml);
            view.UIAnimation = new UIFadeAnimation();
            ViewManager.RegisterView(view);
        }

        private void RegisterDefaultPopups()
        {
            if (_settingsPopupUxml != null && _resolver != null && PopupManager != null)
            {
                var settings = new FDSettingsPopup();
                _resolver.Inject(settings);
                settings.Initialize(_settingsPopupUxml);
                settings.UIAnimation = new UIFadeAnimation();
                PopupManager.RegisterPopup("Settings", settings);
            }
        }


        private void RegisterPopup<T>(VisualTreeAsset uxml) where T : PopupBase, new()
        {
            if (uxml == null) return;
            var popup = new T();
            popup.Initialize(uxml);
            popup.UIAnimation = new UIFadeAnimation();
            PopupManager.RegisterPopup(popup);
        }

        private void Start()
        {
            // Show initial view on startup
            ViewManager.SwitchView<MainMenuView>().Forget();
        }
        private VisualElement _layerBg;
        private VisualElement _layerMid;
        private VisualElement _layerFront;

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (_layerBg == null) return;

            // Simple parallax calculation
            float xPerc = (evt.position.x / Screen.width) - 0.5f;
            float yPerc = (evt.position.y / Screen.height) - 0.5f;

            _layerBg.transform.position = new Vector3(xPerc * 10, yPerc * 10, 0);
            _layerMid.transform.position = new Vector3(xPerc * 25, yPerc * 25, 0);
            _layerFront.transform.position = new Vector3(xPerc * 50, yPerc * 50, 0);
        }
    }
}
