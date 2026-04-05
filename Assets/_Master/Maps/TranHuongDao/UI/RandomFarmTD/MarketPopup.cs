using Abel.TranHuongDao.Core;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    /// <summary>
    /// Market popup — shows current prices and allows selling harvested crops.
    /// Full UI implementation (chart, trend arrows) is Phase 2 & 4.
    /// This implementation compiles cleanly with the new TDEconomyService API.
    /// </summary>
    public class MarketPopup : PopupBase
    {
        private Button _btnClose;

        [Inject] private TDEconomyService _economyService;

        // ── Labels ────────────────────────────────────────────────────────
        private Label _lblPriceCarrot, _lblTrendCarrot;
        private Label _lblPricePumpkin, _lblTrendPumpkin;
        private Label _lblPriceGrape, _lblTrendGrape;

        private Label _lblInvCarrot, _lblInvPumpkin, _lblInvGrape;

        // ── Sell buttons ──────────────────────────────────────────────────
        private Button _btnSellCarrot, _btnSellPumpkin, _btnSellGrape;

        // ── Previous prices for trend calculation ─────────────────────────
        private float _prevCarrot, _prevPumpkin, _prevGrape;

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

            _lblInvCarrot = RootElement.Q<Label>("lbl-inv-carrot");
            _lblInvPumpkin = RootElement.Q<Label>("lbl-inv-pumpkin");
            _lblInvGrape = RootElement.Q<Label>("lbl-inv-grape");

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

        // ── Private ───────────────────────────────────────────────────────

        private void SellAll(string cropID)
        {
            if (_economyService == null) return;

            if (_economyService.TrySellAllCrop(cropID, out int goldEarned))
                Debug.Log($"[MarketPopup] Sold all {cropID} for {goldEarned}g.");
        }

        private void RefreshPrices()
        {
            if (_economyService == null) return;

            UpdateCropUI("Carrot", _lblPriceCarrot, _lblTrendCarrot, _lblInvCarrot, ref _prevCarrot);
            UpdateCropUI("Pumpkin", _lblPricePumpkin, _lblTrendPumpkin, _lblInvPumpkin, ref _prevPumpkin);
            UpdateCropUI("Grape", _lblPriceGrape, _lblTrendGrape, _lblInvGrape, ref _prevGrape);
        }

        private void UpdateCropUI(string cropID, Label priceLabel, Label trendLabel,
                                   Label invLabel, ref float prevPrice)
        {
            float currentPrice = _economyService.GetCurrentPrice(cropID);
            float delta = currentPrice - prevPrice;

            if (priceLabel != null)
                priceLabel.text = $"{(int)currentPrice}g";

            if (trendLabel != null)
            {
                trendLabel.text = prevPrice > 0
                    ? (delta >= 0 ? $"▲ +{(int)delta}" : $"▼ {(int)delta}")
                    : "—";

                trendLabel.RemoveFromClassList("trend-up");
                trendLabel.RemoveFromClassList("trend-down");
                trendLabel.AddToClassList(delta >= 0 ? "trend-up" : "trend-down");
            }

            if (invLabel != null)
                invLabel.text = $"x{_economyService.GetInventoryCount(cropID)}";

            prevPrice = currentPrice;
        }
    }
}
