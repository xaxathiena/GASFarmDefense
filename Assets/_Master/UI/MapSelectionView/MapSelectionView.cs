using System.Collections.Generic;
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
        
        private List<VisualElement> _mapItems = new List<VisualElement>();
        private Dictionary<string, MapData> _mapDataDict = new Dictionary<string, MapData>();

        protected override void OnSetup()
        {
            base.OnSetup();

            _btnStart = RootElement.Q<Button>("btn-start");
            if (_btnStart != null) _btnStart.clicked += OnStartClicked;

            _btnBack = RootElement.Q<Button>("btn-back");
            if (_btnBack != null) _btnBack.clicked += OnBackClicked;

            _lblReviewTitle = RootElement.Q<Label>("map-review-title");
            _lblReviewDescription = RootElement.Q<Label>("map-review-description");
            _reviewPreview = RootElement.Q<VisualElement>("map-review-preview");

            // Setup Data
            _mapDataDict["map-frozen"] = new MapData { 
                Name = "FROZEN WASTES", 
                Region = "NORTH-REACH", 
                Description = "A desolate tundra where the cold itself is an enemy. Low visibility makes long-range defense difficult.",
                PreviewColor = new Color(0.35f, 0.7f, 0.72f) // #5bb4b8
            };
            _mapDataDict["map-iron"] = new MapData { 
                Name = "IRON FORTRESS", 
                Region = "CORE-DEPTHS", 
                Description = "Located in the deepest reaches of the Core-Depths. Extreme heat causes periodic lava flows. High volatility requires careful placement of cooling towers.",
                PreviewColor = new Color(0.48f, 0.49f, 0.32f) // #7b7e52
            };
            _mapDataDict["map-emerald"] = new MapData { 
                Name = "EMERALD SANCTUM", 
                Region = "LIFE-WOMB", 
                Description = "A vibrant forest pulsating with raw mana. Magic storms occasionally overload tower systems, requiring manual resets.",
                PreviewColor = new Color(0.1f, 0.29f, 0.21f) // #1a4a35
            };

            // Register Map Items
            var items = RootElement.Query<VisualElement>(className: "map-item").ToList();
            foreach (var item in items)
            {
                _mapItems.Add(item);
                item.RegisterCallback<ClickEvent>(evt => OnMapItemClicked(item));
            }
        }

        private void OnMapItemClicked(VisualElement clickedItem)
        {
            // Update UI Selection state
            foreach (var item in _mapItems)
            {
                item.RemoveFromClassList("selected");
            }
            clickedItem.AddToClassList("selected");

            // Update Review Panel
            if (_mapDataDict.TryGetValue(clickedItem.name, out var data))
            {
                if (_lblReviewTitle != null) _lblReviewTitle.text = data.Name;
                if (_lblReviewDescription != null) _lblReviewDescription.text = data.Description;
                if (_reviewPreview != null) _reviewPreview.style.backgroundColor = data.PreviewColor;
            }
        }

        private void OnStartClicked()
        {
            Debug.Log("Start Battle Clicked. Transitioning to Map Game...");
            
            // Hide global persistent elements for the game
            GameUIManager.Instance.SetGlobalUIActive(false);

            // Transition flow: Map Selection -> Loading -> Random Farm TD
            SwitchToGameFlow().Forget();
        }

        private async UniTaskVoid SwitchToGameFlow()
        {
            // Show loading
            await GameUIManager.Instance.ViewManager.SwitchView<LoadingScreenView>();
            
            // Simulate loading data
            await UniTask.Delay(1500);

            // Switch to the actual game HUD
            GameUIManager.Instance.ViewManager.SwitchView<RandomFarmTDView>().Forget();
            Debug.Log("Loading Complete. Game HUD is now showing.");
        }

        private void OnBackClicked()
        {
            Debug.Log("Back Clicked. Returning to Main Menu...");
            GameUIManager.Instance.SetGlobalUIActive(true);
            GameUIManager.Instance.ViewManager.SwitchView<MainMenuView>().Forget();
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
