using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class FarmPopup : PopupBase
    {
        private Button _btnClose;
        
        [Inject] private Abel.TranHuongDao.Core.TDEconomyService _economyService;

        private Label _lblSeeds;
        private Label _lblGrowingCarrot, _lblGrowingPumpkin, _lblGrowingGrape;
        private Label _lblInvCarrot, _lblInvPumpkin, _lblInvGrape;

        private Button _btnPlantCarrot, _btnPlantPumpkin, _btnPlantGrape;

        protected override void OnSetup()
        {
            base.OnSetup();
            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _lblSeeds = RootElement.Q<Label>("lbl-seeds");
            _lblGrowingCarrot = RootElement.Q<Label>("lbl-growing-carrot");
            _lblGrowingPumpkin = RootElement.Q<Label>("lbl-growing-pumpkin");
            _lblGrowingGrape = RootElement.Q<Label>("lbl-growing-grape");

            _lblInvCarrot = RootElement.Q<Label>("lbl-inv-carrot");
            _lblInvPumpkin = RootElement.Q<Label>("lbl-inv-pumpkin");
            _lblInvGrape = RootElement.Q<Label>("lbl-inv-grape");

            _btnPlantCarrot = RootElement.Q<Button>("btn-plant-carrot");
            _btnPlantPumpkin = RootElement.Q<Button>("btn-plant-pumpkin");
            _btnPlantGrape = RootElement.Q<Button>("btn-plant-grape");

            if (_btnPlantCarrot != null) _btnPlantCarrot.clicked += () => PlantCrop("Carrot");
            if (_btnPlantPumpkin != null) _btnPlantPumpkin.clicked += () => PlantCrop("Pumpkin");
            if (_btnPlantGrape != null) _btnPlantGrape.clicked += () => PlantCrop("Grape");

            if (_economyService != null)
            {
                _economyService.OnDataChanged += RefreshData;
                RefreshData();
            }
        }

        private void PlantCrop(string type)
        {
            if (_economyService != null && _economyService.Seeds > 0)
            {
                _economyService.PlantCrop(type);
                Debug.Log($"Planted {type}!");
            }
        }

        private void RefreshData()
        {
            if (_economyService == null) return;

            if (_lblSeeds != null) _lblSeeds.text = _economyService.Seeds.ToString();
            
            if (_lblGrowingCarrot != null) _lblGrowingCarrot.text = $"Growing: {_economyService.GrowingCrops["Carrot"]}";
            if (_lblGrowingPumpkin != null) _lblGrowingPumpkin.text = $"Growing: {_economyService.GrowingCrops["Pumpkin"]}";
            if (_lblGrowingGrape != null) _lblGrowingGrape.text = $"Growing: {_economyService.GrowingCrops["Grape"]}";

            if (_lblInvCarrot != null) _lblInvCarrot.text = _economyService.Inventory["Carrot"].ToString();
            if (_lblInvPumpkin != null) _lblInvPumpkin.text = _economyService.Inventory["Pumpkin"].ToString();
            if (_lblInvGrape != null) _lblInvGrape.text = _economyService.Inventory["Grape"].ToString();
        }
    }
}
