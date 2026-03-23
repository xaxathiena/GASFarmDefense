using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class MarketPopup : PopupBase
    {
        private Button _btnClose;
        
        [Inject] private Abel.TranHuongDao.Core.TDEconomyService _economyService;

        private Label _lblPriceCarrot, _lblTrendCarrot;
        private Label _lblPricePumpkin, _lblTrendPumpkin;
        private Label _lblPriceGrape, _lblTrendGrape;
        
        private Button _btnSellCarrot, _btnSellPumpkin, _btnSellGrape;

        protected override void OnSetup()
        {
            base.OnSetup();
            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _lblPriceCarrot = RootElement.Q<Label>("lbl-price-carrot");
            _lblTrendCarrot = RootElement.Q<Label>("lbl-trend-carrot");
            _lblPricePumpkin = RootElement.Q<Label>("lbl-price-pumpkin");
            _lblTrendPumpkin = RootElement.Q<Label>("lbl-trend-pumpkin");
            _lblPriceGrape = RootElement.Q<Label>("lbl-price-grape");
            _lblTrendGrape = RootElement.Q<Label>("lbl-trend-grape");

            _btnSellCarrot = RootElement.Q<Button>("btn-sell-carrot");
            _btnSellPumpkin = RootElement.Q<Button>("btn-sell-pumpkin");
            _btnSellGrape = RootElement.Q<Button>("btn-sell-grape");

            if (_btnSellCarrot != null) _btnSellCarrot.clicked += () => SellAll("Carrot");
            if (_btnSellPumpkin != null) _btnSellPumpkin.clicked += () => SellAll("Pumpkin");
            if (_btnSellGrape != null) _btnSellGrape.clicked += () => SellAll("Grape");

            if (_economyService != null)
            {
                _economyService.OnDataChanged += RefreshPrices;
                RefreshPrices();
            }
        }

        private void SellAll(string type)
        {
            if (_economyService != null)
            {
                int count = _economyService.Inventory[type];
                if (count > 0)
                {
                    float price = _economyService.MarketPrices[type];
                    int totalGold = (int)(count * price);
                    _economyService.Inventory[type] = 0;
                    _economyService.AddGold(totalGold);
                    Debug.Log($"Sold {count} {type} for {totalGold} Gold!");
                }
            }
        }

        private void RefreshPrices()
        {
            if (_economyService == null) return;

            UpdateItemUI("Carrot", _lblPriceCarrot, _lblTrendCarrot);
            UpdateItemUI("Pumpkin", _lblPricePumpkin, _lblTrendPumpkin);
            UpdateItemUI("Grape", _lblPriceGrape, _lblTrendGrape);
        }

        private void UpdateItemUI(string type, Label priceLabel, Label trendLabel)
        {
            if (priceLabel == null || trendLabel == null) return;

            float price = _economyService.MarketPrices[type];
            float trend = _economyService.PriceTrends[type];

            priceLabel.text = $"${(int)price}";
            trendLabel.text = trend > 0 ? $"+{trend}" : $"{trend}";
            trendLabel.RemoveFromClassList("trend-up");
            trendLabel.RemoveFromClassList("trend-down");
            trendLabel.AddToClassList(trend >= 0 ? "trend-up" : "trend-down");
        }
    }
}
