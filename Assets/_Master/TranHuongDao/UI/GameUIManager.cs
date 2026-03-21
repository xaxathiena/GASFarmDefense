using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using GASFarmDefense.UIToolkit.Core.Animations;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    /// <summary>
    /// Singleton Manager for the specific game.
    /// This inherits from UIManager which handles the View/Popup containers.
    /// </summary>
    public class GameUIManager : UIManager
    {
        public static GameUIManager Instance { get; private set; }

        [Header("UXML Assets")]
        [SerializeField] private VisualTreeAsset _backgroundUxml;
        [SerializeField] private VisualTreeAsset _topBarUxml;
        [SerializeField] private VisualTreeAsset _mainMenuUxml;
        [SerializeField] private VisualTreeAsset _mapSelectionUxml;
        [SerializeField] private VisualTreeAsset _loadingScreenUxml;

        protected override void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            base.Awake(); // Sets up Root, Containers, Managers

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
        }

        /// <summary>
        /// Registers all the specific views for this game.
        /// </summary>
        protected override void RegisterCoreUI()
        {
            // 1. Instantiate and Register MainMenuView
            var mainMenuView = new MainMenuView();
            if (_mainMenuUxml != null)
            {
                mainMenuView.Initialize(_mainMenuUxml);
                mainMenuView.UIAnimation = new UIFadeAnimation(); 
                ViewManager.RegisterView(mainMenuView);
            }

            // 2. Instantiate and Register MapSelectionView
            var mapSelView = new MapSelectionView();
            if (_mapSelectionUxml != null)
            {
                mapSelView.Initialize(_mapSelectionUxml);
                mapSelView.UIAnimation = new UIFadeAnimation();
                ViewManager.RegisterView(mapSelView);
            }

            // 3. Instantiate and Register LoadingScreenView
            var loadingView = new LoadingScreenView();
            if (_loadingScreenUxml != null)
            {
                loadingView.Initialize(_loadingScreenUxml);
                loadingView.UIAnimation = new UIFadeAnimation();
                ViewManager.RegisterView(loadingView);
            }
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
