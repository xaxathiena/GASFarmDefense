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
        [Inject] private Abel.TranHuongDao.Core.TDHandService _handService;
        [Inject] private Abel.TranHuongDao.Core.TowerBuilderConfig _towerConfig;
        private Abel.TranHuongDao.Core.UnitsConfig _unitsConfig;
        [Inject] private Abel.TranHuongDao.Core.IConfigService _configService;

        protected override void OnSetup()
        {
            base.OnSetup();

            if (_configService != null)
            {
                _unitsConfig = _configService.GetConfig<Abel.TranHuongDao.Core.UnitsConfig>();
            }

            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _btnBuyPack1 = RootElement.Q<Button>("btn-buy-pack-1");
            if (_btnBuyPack1 != null) _btnBuyPack1.clicked += () => BuyPack(1, 10, _btnBuyPack1);

            _btnBuyPack5 = RootElement.Q<Button>("btn-buy-pack-5");
            if (_btnBuyPack5 != null) _btnBuyPack5.clicked += () => BuyPack(5, 100, _btnBuyPack5);

            _btnRerollWeapons = RootElement.Q<Button>("btn-reroll-weapons");
            if (_btnRerollWeapons != null) _btnRerollWeapons.clicked += RerollWeapons;

            _btnRerollBuffs = RootElement.Q<Button>("btn-reroll-buffs");
            if (_btnRerollBuffs != null) _btnRerollBuffs.clicked += RerollBuffs;
            
            RefreshUI();
        }

        private void BuyPack(int tier, int cost, Button sourceButton)
        {
            if (_economyService != null && _economyService.TrySpendGold(cost))
            {
                Debug.Log($"Bought Tier {tier} Pack for {cost} Gold!");
                
                if (_towerConfig != null && _unitsConfig != null)
                {
                    string towerID = _towerConfig.GetTowerIDWithTier(tier, _unitsConfig);
                    if (!string.IsNullOrEmpty(towerID))
                    {
                        // Get button position for the fly-in animation
                        Vector2 startPos = sourceButton.worldBound.center;
                        _handService?.AddCard(towerID, tier, startPos);
                    }
                    else
                    {
                        Debug.LogWarning($"[ShopPopup] No tower found for tier {tier}");
                    }
                }
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
