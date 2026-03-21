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
