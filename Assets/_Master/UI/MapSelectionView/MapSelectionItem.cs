using System;
using Abel.TranHuongDao.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class MapSelectionItem
    {
        public VisualElement Root { get; private set; }
        public FD_MapConfigSO Config { get; private set; }

        private Label _lblTitle;
        private Label _lblRegion;
        private VisualElement _colorBar;

        public event Action<MapSelectionItem> OnClicked;

        public MapSelectionItem(VisualElement root)
        {
            Root = root;
            _lblTitle = Root.Q<Label>("map-item-title");
            _lblRegion = Root.Q<Label>("map-item-region");
            _colorBar = Root.Q<VisualElement>("map-item-color");

            Root.RegisterCallback<ClickEvent>(evt => OnClicked?.Invoke(this));
        }

        public void Setup(FD_MapConfigSO config)
        {
            Config = config;
            if (_lblTitle != null) _lblTitle.text = config.DisplayName.ToUpper();
            if (_lblRegion != null) _lblRegion.text = $"REGION: {config.MapId.ToUpper()}";
            if (_colorBar != null) _colorBar.style.backgroundColor = config.PreviewColor;
        }

        public void SetSelected(bool isSelected)
        {
            if (isSelected) Root.AddToClassList("selected");
            else Root.RemoveFromClassList("selected");
        }
    }
}
