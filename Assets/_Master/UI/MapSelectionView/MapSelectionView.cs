using System.Collections.Generic;
using Abel.TranHuongDao.Core;
using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class MapSelectionView : ViewBase
    {
        private class MapData
        {
            public string Name;
            public string Region;
            public string Description;
            public Color PreviewColor;
        }

        private Button _btnStart;
        private Button _btnBack;
        private Label _lblReviewTitle;
        private Label _lblReviewDescription;
        private VisualElement _reviewPreview;
        private VisualElement _mapListContainer;

        private VisualTreeAsset _mapItemTemplate;

        private List<MapSelectionItem> _mapItems = new List<MapSelectionItem>();
        private List<FD_MapConfigSO> _mapConfigs = new List<FD_MapConfigSO>();
        private FD_MapConfigSO _selectedMap;

        [VContainer.Inject] private SceneLoaderService _sceneLoader;
        [VContainer.Inject] private IConfigService _configService;
        [VContainer.Inject] private ViewManager _viewManager;
        [VContainer.Inject] private GASFarmDefense.UIToolkit.Core.UIManager _uiManager;

        protected override void OnSetup()
        {
            base.OnSetup();

            // 1. Load Maps from Config
            var mapsConfig = _configService.GetConfig<FD_MapsConfigSO>();
            if (mapsConfig != null)
            {
                _mapConfigs = mapsConfig.Maps;
            }

            // 2. Bind static elements
            _btnStart = RootElement.Q<Button>("btn-start");
            if (_btnStart != null) _btnStart.clicked += OnStartClicked;

            _btnBack = RootElement.Q<Button>("btn-back");
            if (_btnBack != null) _btnBack.clicked += OnBackClicked;

            _lblReviewTitle = RootElement.Q<Label>("map-review-title");
            _lblReviewDescription = RootElement.Q<Label>("map-review-description");
            _reviewPreview = RootElement.Q<VisualElement>("map-review-preview");

            _mapListContainer = RootElement.Q<VisualElement>("map-list");

            // 3. Load Templates from Resources (Convention-based)
            _mapItemTemplate = Resources.Load<VisualTreeAsset>("UI/Templates/MapSelectionItem");
            if (_mapItemTemplate == null)
            {
                Debug.LogError("[MapSelectionView] Failed to load MapSelectionItem template from Resources/UI/Templates/MapSelectionItem");
            }

            // 4. Dynamic Map Items
            RefreshMapList();
        }

        private void RefreshMapList()
        {
            if (_mapListContainer == null || _mapItemTemplate == null) return;

            // Clear existing (though we cleaned UXML, good to be safe)
            // But we need to keep the header labels! 
            // Better: find a sub-container or only remove items with .map-item class
            var existingItems = _mapListContainer.Query<VisualElement>(className: "map-item").ToList();
            foreach (var item in existingItems) item.RemoveFromHierarchy();


            _mapItems.Clear();

            for (int i = 0; i < _mapConfigs.Count; i++)
            {
                var config = _mapConfigs[i];
                var item = _mapItemTemplate.Instantiate();
                var itemRoot = item.Q<VisualElement>(className: "map-item");


                if (itemRoot != null)
                {
                    var mapItem = new MapSelectionItem(itemRoot);
                    mapItem.Setup(config);
                    _mapListContainer.Add(itemRoot);
                    _mapItems.Add(mapItem);

                    mapItem.OnClicked += (mi) => OnMapItemClicked(mi);

                    // Auto-select first map
                    if (i == 0) OnMapItemClicked(mapItem);
                }
            }
        }


        private void OnMapItemClicked(MapSelectionItem clickedItem)
        {
            // Update UI Selection state
            foreach (var item in _mapItems)
            {
                item.SetSelected(false);
            }
            clickedItem.SetSelected(true);

            _selectedMap = clickedItem.Config;

            // Update Review Panel
            if (_lblReviewTitle != null) _lblReviewTitle.text = _selectedMap.DisplayName;
            if (_lblReviewDescription != null) _lblReviewDescription.text = _selectedMap.Description;
            if (_reviewPreview != null) _reviewPreview.style.backgroundColor = _selectedMap.PreviewColor;
        }

        private void OnStartClicked()
        {
            if (_selectedMap == null) return;

            Debug.Log($"Starting Battle for {_selectedMap.DisplayName}...");
            _sceneLoader.LoadMap(_selectedMap).Forget();
        }

        private void OnBackClicked()
        {
            Debug.Log("Back Clicked. Returning to Main Menu...");
            _uiManager.SetGlobalUIActive(true);
            _viewManager.SwitchView<MainMenuView>().Forget();
        }

        public override async UniTask Hide()
        {
            var mapList = RootElement.Q<VisualElement>("map-list");
            var mapReview = RootElement.Q<VisualElement>("map-review");


            if (mapList != null)
            {
                mapList.RemoveFromClassList("slide-visible");
                mapList.AddToClassList("slide-top-hidden");
            }
            if (mapReview != null)
            {
                mapReview.RemoveFromClassList("slide-visible");
                mapReview.AddToClassList("slide-right-hidden");
            }

            await UniTask.Delay(200);
            await base.Hide();
        }

        public override async UniTask Show()
        {
            await base.Show();
            var mapList = RootElement.Q<VisualElement>("map-list");
            var mapReview = RootElement.Q<VisualElement>("map-review");
            if (mapList != null)
            {
                mapList.RemoveFromClassList("slide-top-hidden");
                mapList.AddToClassList("slide-visible");
            }
            if (mapReview != null)
            {
                await UniTask.Delay(50);
                mapReview.RemoveFromClassList("slide-right-hidden");
                mapReview.AddToClassList("slide-visible");
            }
        }
    }
}
