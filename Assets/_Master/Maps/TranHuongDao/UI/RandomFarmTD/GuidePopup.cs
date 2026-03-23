using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class GuidePopup : PopupBase
    {
        private Button _btnClose;
        private Button _tabHowToPlay, _tabShopInfo, _tabTowerTiers;
        private VisualElement _contentHowToPlay, _contentShopInfo;

        protected override void OnSetup()
        {
            base.OnSetup();
            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _tabHowToPlay = RootElement.Q<Button>("tab-how-to-play");
            _tabShopInfo = RootElement.Q<Button>("tab-shop-info");
            _tabTowerTiers = RootElement.Q<Button>("tab-tower-tiers");

            _contentHowToPlay = RootElement.Q<ScrollView>("scroll-how-to-play");
            _contentShopInfo = RootElement.Q<VisualElement>("content-shop-info");

            if (_tabHowToPlay != null) _tabHowToPlay.clicked += () => SwitchTab("how-to-play");
            if (_tabShopInfo != null) _tabShopInfo.clicked += () => SwitchTab("shop-info");
            if (_tabTowerTiers != null) _tabTowerTiers.clicked += () => SwitchTab("tower-tiers");
            
            SwitchTab("how-to-play");
        }

        private void SwitchTab(string tabName)
        {
            // Reset all buttons
            _tabHowToPlay?.RemoveFromClassList("active");
            _tabShopInfo?.RemoveFromClassList("active");
            _tabTowerTiers?.RemoveFromClassList("active");

            // Hide all content
            if (_contentHowToPlay != null) _contentHowToPlay.style.display = DisplayStyle.None;
            if (_contentShopInfo != null) _contentShopInfo.style.display = DisplayStyle.None;

            switch (tabName)
            {
                case "how-to-play":
                    _tabHowToPlay?.AddToClassList("active");
                    if (_contentHowToPlay != null) _contentHowToPlay.style.display = DisplayStyle.Flex;
                    break;
                case "shop-info":
                    _tabShopInfo?.AddToClassList("active");
                    if (_contentShopInfo != null) _contentShopInfo.style.display = DisplayStyle.Flex;
                    break;
                case "tower-tiers":
                    _tabTowerTiers?.AddToClassList("active");
                    // Implement tower tiers content if needed
                    break;
            }
        }
    }
}
