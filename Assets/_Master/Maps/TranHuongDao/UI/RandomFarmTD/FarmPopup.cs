using GASFarmDefense.UIToolkit.Core;
using Abel.TranHuongDao.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    /// <summary>
    /// Farm popup — lets the player spend seeds to plant crops.
    /// Full UI implementation is Phase 3; this stub compiles cleanly with
    /// the new TDEconomyService API and maintains the OnDataChanged subscription.
    /// </summary>
    public class FarmPopup : PopupBase
    {
        private Button _btnClose;

        [Inject] private TDEconomyService _economyService;

        // ── UI elements (wired in OnSetup) ───────────────────────────────
        private Label _lblSeeds;

        // Per-crop plant buttons — Phase 3 will wire these from dynamic UXML
        private Button _btnPlantCarrot, _btnPlantPumpkin, _btnPlantGrape;

        // Per-crop growing / inventory labels (legacy names kept for UXML compat)
        private Label _lblGrowingCarrot, _lblGrowingPumpkin, _lblGrowingGrape;
        private Label _lblInvCarrot,     _lblInvPumpkin,     _lblInvGrape;

        protected override void OnSetup()
        {
            base.OnSetup();

            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _lblSeeds        = RootElement.Q<Label>("lbl-seeds");
            _lblGrowingCarrot  = RootElement.Q<Label>("lbl-growing-carrot");
            _lblGrowingPumpkin = RootElement.Q<Label>("lbl-growing-pumpkin");
            _lblGrowingGrape   = RootElement.Q<Label>("lbl-growing-grape");
            _lblInvCarrot      = RootElement.Q<Label>("lbl-inv-carrot");
            _lblInvPumpkin     = RootElement.Q<Label>("lbl-inv-pumpkin");
            _lblInvGrape       = RootElement.Q<Label>("lbl-inv-grape");

            _btnPlantCarrot  = RootElement.Q<Button>("btn-plant-carrot");
            _btnPlantPumpkin = RootElement.Q<Button>("btn-plant-pumpkin");
            _btnPlantGrape   = RootElement.Q<Button>("btn-plant-grape");

            // Plant 1 seed per click (Phase 3: replace with configurable amount)
            if (_btnPlantCarrot  != null) _btnPlantCarrot.clicked  += () => PlantOne("Carrot");
            if (_btnPlantPumpkin != null) _btnPlantPumpkin.clicked += () => PlantOne("Pumpkin");
            if (_btnPlantGrape   != null) _btnPlantGrape.clicked   += () => PlantOne("Grape");

            if (_economyService != null)
            {
                _economyService.OnDataChanged += RefreshData;
                RefreshData();
            }
        }

        // ── Private ───────────────────────────────────────────────────────

        private void PlantOne(string cropID)
        {
            if (_economyService == null) return;
            bool planted = _economyService.TryPlantSeeds(cropID, 1);
            if (planted)
                Debug.Log($"[FarmPopup] Planted 1x {cropID}.");
        }

        private void RefreshData()
        {
            if (_economyService == null) return;

            if (_lblSeeds != null)
                _lblSeeds.text = $"Seeds: {_economyService.Seeds}";

            // Growing counts — sum of SeedCounts for each crop type
            int growingCarrot  = 0, growingPumpkin = 0, growingGrape = 0;
            foreach (var batch in _economyService.PlantedCrops)
            {
                if (batch.CropID == "Carrot")  growingCarrot  += batch.SeedCount;
                if (batch.CropID == "Pumpkin") growingPumpkin += batch.SeedCount;
                if (batch.CropID == "Grape")   growingGrape   += batch.SeedCount;
            }

            if (_lblGrowingCarrot  != null) _lblGrowingCarrot.text  = $"Growing: {growingCarrot}";
            if (_lblGrowingPumpkin != null) _lblGrowingPumpkin.text = $"Growing: {growingPumpkin}";
            if (_lblGrowingGrape   != null) _lblGrowingGrape.text   = $"Growing: {growingGrape}";

            if (_lblInvCarrot  != null) _lblInvCarrot.text  = _economyService.GetInventoryCount("Carrot").ToString();
            if (_lblInvPumpkin != null) _lblInvPumpkin.text = _economyService.GetInventoryCount("Pumpkin").ToString();
            if (_lblInvGrape   != null) _lblInvGrape.text   = _economyService.GetInventoryCount("Grape").ToString();
        }
    }
}
