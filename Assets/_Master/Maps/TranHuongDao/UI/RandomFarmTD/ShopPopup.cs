using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class ShopPopup : PopupBase
    {
        private Button _btnClose;
        private Button _btnBuyPack1;
        private Button _btnBuyPack5;
        private Button _btnRerollWeapons;
        private Button _btnRerollBuffs;

        [Inject] private Abel.TranHuongDao.Core.TDEconomyService _economyService;

        protected override void OnSetup()
        {
            base.OnSetup();
            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _btnBuyPack1 = RootElement.Q<Button>("btn-buy-pack-1");
            if (_btnBuyPack1 != null) _btnBuyPack1.clicked += () => BuyPack(10);

            _btnBuyPack5 = RootElement.Q<Button>("btn-buy-pack-5");
            if (_btnBuyPack5 != null) _btnBuyPack5.clicked += () => BuyPack(100);

            _btnRerollWeapons = RootElement.Q<Button>("btn-reroll-weapons");
            if (_btnRerollWeapons != null) _btnRerollWeapons.clicked += RerollWeapons;

            _btnRerollBuffs = RootElement.Q<Button>("btn-reroll-buffs");
            if (_btnRerollBuffs != null) _btnRerollBuffs.clicked += RerollBuffs;
            
            RefreshUI();
        }

        private void BuyPack(int cost)
        {
            if (_economyService != null && _economyService.TrySpendGold(cost))
            {
                Debug.Log($"Bought Pack for {cost} Gold!");
                // Logic to add random tower card to player hand goes here
            }
        }

        private void RerollWeapons()
        {
            if (_economyService != null && _economyService.TrySpendGold(5))
            {
                Debug.Log("Rerolled Weapons!");
                // Animation and update grid
            }
        }

        private void RerollBuffs()
        {
            if (_economyService != null && _economyService.TrySpendGold(5))
            {
                Debug.Log("Rerolled Buffs!");
                // Animation and update grid
            }
        }

        private void RefreshUI()
        {
            // Update labels if needed
        }
    }
}
