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

        protected override void OnSetup()
        {
            base.OnSetup();

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

        [VContainer.Inject] private Abel.TranHuongDao.Core.TowerDragDropManager _towerDragManager;

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
            GameUIManager.Instance.PopupManager.ShowPopup<ShopPopup>().Forget();
        }

        private void OnMarketClicked()
        {
            Debug.Log("Open Market Popup");
            GameUIManager.Instance.PopupManager.ShowPopup<MarketPopup>().Forget();
        }

        private void OnFarmClicked()
        {
            Debug.Log("Open Farm Popup");
            GameUIManager.Instance.PopupManager.ShowPopup<FarmPopup>().Forget();
        }

        private void OnGuideClicked()
        {
            Debug.Log("Open Guide Popup");
            GameUIManager.Instance.PopupManager.ShowPopup<GuidePopup>().Forget();
        }

        private void OnMenuClicked()
        {
            Debug.Log("Return to Main Menu (Demo)");
            GameUIManager.Instance.SetGlobalUIActive(true);
            GameUIManager.Instance.ViewManager.SwitchView<MainMenuView>().Forget();
        }
    }
}
