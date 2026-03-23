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
        [VContainer.Inject] private GASFarmDefense.UIToolkit.Core.UIManager _uiManager;
        [VContainer.Inject] private SceneLoaderService _sceneLoader;

        protected override void OnSetup()
        {
            base.OnSetup();

            _lblGold = RootElement.Q<Label>("lbl-gold");
            _lblCarrot = RootElement.Q<Label>("lbl-carrot");
            _lblPumpkin = RootElement.Q<Label>("lbl-pumpkin");
            _lblGrape = RootElement.Q<Label>("lbl-grape");

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

            SetupCardItems();
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

        private void SetupCardItems()
        {
            _cardTemplate = Resources.Load<VisualTreeAsset>("UI/Templates/TDCardItemUI");
            var cardContainer = RootElement.Q<VisualElement>("card-container");
            if (cardContainer == null) return;
            if (_cardTemplate == null) return;

            cardContainer.Clear();
            _cards.Clear();

            // Example: Generate 5 cards dynamically
            var cardData = new[]
            {
                new { name = "", lvl = "1", gold = false },
                new { name = "", lvl = "1", gold = false },
                new { name = "", lvl = "1", gold = false },
                new { name = "", lvl = "5", gold = true },
                new { name = "", lvl = "Item", gold = false }
            };

            foreach (var data in cardData)
            {
                var cardUI = new TDCardItemUI(_cardTemplate, _towerDragManager);
                cardUI.SetData(data.name, data.lvl, data.gold);


                cardUI.OnCardPlayed += (c, pos) => Debug.Log($"Card Played at {pos}");
                cardUI.OnCardClicked += (c) => Debug.Log("Card Clicked");

                cardContainer.Add(cardUI.Root);
                _cards.Add(cardUI);
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
