using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class RandomFarmTDView : ViewBase
    {
        private Button _btnShop;
        private Button _btnMarket;
        private Button _btnFarm;
        private Button _btnGuide;
        private Button _btnMenu;

        private Label _lblGold;
        private Label _lblCarrot;
        private Label _lblPumpkin;
        private Label _lblGrape;

        [VContainer.Inject] private Abel.TranHuongDao.Core.TDEconomyService _economyService;
        [VContainer.Inject] private Abel.TranHuongDao.Core.TowerDragDropManager _towerDragManager;
        [VContainer.Inject] private ViewManager _viewManager;
        [VContainer.Inject] private PopupManager _popupManager;
        [VContainer.Inject] private SceneLoaderService _sceneLoader;
        [VContainer.Inject] private GASFarmDefense.UIToolkit.Core.UIManager _uiManager;
        [VContainer.Inject] private Abel.TranHuongDao.Core.TDHandService _handService;
        private Abel.TranHuongDao.Core.UnitsConfig _unitsConfig;
        [VContainer.Inject] private Abel.TranHuongDao.Core.IConfigService _configService;
        [VContainer.Inject] private FD.IEventBus _eventBus;

        protected override void OnSetup()
        {
            base.OnSetup();

            _lblGold = RootElement.Q<Label>("lbl-gold");
            _lblCarrot = RootElement.Q<Label>("lbl-carrot");
            _lblPumpkin = RootElement.Q<Label>("lbl-pumpkin");
            _lblGrape = RootElement.Q<Label>("lbl-grape");

            if (_configService != null)
            {
                _unitsConfig = _configService.GetConfig<Abel.TranHuongDao.Core.UnitsConfig>();
            }

            if (_economyService != null)
            {
                _economyService.OnDataChanged += RefreshResources;
                RefreshResources();
            }

            _btnShop = RootElement.Q<Button>("btn-shop");
            if (_btnShop != null) _btnShop.clicked += OnShopClicked;

            _btnMarket = RootElement.Q<Button>("btn-market");
            if (_btnMarket != null) _btnMarket.clicked += OnMarketClicked;

            _btnFarm = RootElement.Q<Button>("btn-farm");
            if (_btnFarm != null) _btnFarm.clicked += OnFarmClicked;

            _btnGuide = RootElement.Q<Button>("btn-guide");
            if (_btnGuide != null) _btnGuide.clicked += OnGuideClicked;

            _btnMenu = RootElement.Q<Button>("btn-menu");
            if (_btnMenu != null) _btnMenu.clicked += OnMenuClicked;

            if (_eventBus != null)
            {
                _eventBus.Subscribe<Abel.TranHuongDao.Core.TDHandService.CardAddedEvent>(OnCardAdded);
            }

            if (_handService != null)
            {
                // Initial hand setup
                foreach (var cardID in _handService.CurrentCards)
                {
                    // For initial setup, we don't need animation
                    _ = CreateCardUI(cardID, 1, Vector2.zero, animate: false);
                }
            }

            // SetupCardItems(); // Removed hardcoded
        }

        private void OnCardAdded(Abel.TranHuongDao.Core.TDHandService.CardAddedEvent evt)
        {
            _ = CreateCardUI(evt.TowerID, evt.Tier, evt.StartScreenPos, animate: true);
        }

        private void RefreshResources()
        {
            if (_economyService == null) return;
            if (_lblGold != null) _lblGold.text = _economyService.Gold.ToString();
            if (_lblCarrot != null) _lblCarrot.text = _economyService.Inventory["Carrot"].ToString();
            if (_lblPumpkin != null) _lblPumpkin.text = _economyService.Inventory["Pumpkin"].ToString();
            if (_lblGrape != null) _lblGrape.text = _economyService.Inventory["Grape"].ToString();
        }

        [SerializeField] private VisualTreeAsset _cardTemplate;
        private List<TDCardItemUI> _cards = new List<TDCardItemUI>();

        private async UniTaskVoid CreateCardUI(string towerID, int tier, Vector2 startScreenPos, bool animate)
        {
            _cardTemplate = Resources.Load<VisualTreeAsset>("UI/Templates/TDCardItemUI");
            var cardContainer = RootElement.Q<VisualElement>("card-container");
            if (cardContainer == null || _cardTemplate == null) return;

            var cardUI = new TDCardItemUI(_cardTemplate, _towerDragManager);

            // Get Display Name from UnitsConfig if available

            string displayName = towerID;
            if (_unitsConfig != null && _unitsConfig.TryGetConfig(towerID, out var config))
            {
                displayName = config.UnitID; // Using UnitID as name
            }


            cardUI.SetData(displayName, tier, towerID);
            cardUI.OnCardPlayed += (c, pos) => Debug.Log($"Card Played at {pos}");


            var root = cardUI.Root;
            cardContainer.Add(root);
            _cards.Add(cardUI);

            if (animate)
            {
                // Wait for layout to calculate the target position
                await UniTask.WaitForEndOfFrame();

                // Calculate the offset from the click position to the final layout position
                // Note: startScreenPos is in world space. root.worldBound gives current world position.
                Vector2 targetPos = root.worldBound.center;
                Vector2 offset = startScreenPos - targetPos;

                // Set initial "flying" state
                root.AddToClassList("card-flying");
                root.style.translate = new Translate(offset.x, offset.y, 0);
                root.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1));
                root.style.opacity = 0.5f;

                // Wait one frame to start the transition
                await UniTask.WaitForEndOfFrame();

                root.RemoveFromClassList("card-flying");
                root.AddToClassList("card-spawn-animate");
                root.style.translate = new Translate(0, 0, 0);
                root.style.scale = new Scale(Vector3.one);
                root.style.opacity = 1f;

                // Clean up animation class after it's done

                await UniTask.Delay(600);
                root.RemoveFromClassList("card-spawn-animate");
            }
        }

        private void OnShopClicked()
        {
            Debug.Log("Open Shop Popup");
            _popupManager.ShowPopup<ShopPopup>().Forget();
        }

        private void OnMarketClicked()
        {
            Debug.Log("Open Market Popup");
            _popupManager.ShowPopup<MarketPopup>().Forget();
        }

        private void OnFarmClicked()
        {
            Debug.Log("Open Farm Popup");
            _popupManager.ShowPopup<FarmPopup>().Forget();
        }

        private void OnGuideClicked()
        {
            Debug.Log("Open Guide Popup");
            _popupManager.ShowPopup<GuidePopup>().Forget();
        }

        private void OnMenuClicked()
        {
            Debug.Log("Return to Main Menu... Unloading Map.");
            _sceneLoader.ReturnToHome().Forget();
        }
    }
}
