using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class RandomFarmTDView : ViewBase
    {
        private Button _btnShop;
        private Button _btnMarket;
        private Button _btnFarm;
        private Button _btnGuide;
        private Button _btnMenu;

        private Label _lblGold;
        private Label _lblCarrot;
        private Label _lblPumpkin;
        private Label _lblGrape;

        [VContainer.Inject] private Abel.TranHuongDao.Core.TDEconomyService _economyService;
        [VContainer.Inject] private Abel.TranHuongDao.Core.TowerDragDropManager _towerDragManager;
        [VContainer.Inject] private ViewManager _viewManager;
        [VContainer.Inject] private PopupManager _popupManager;
        [VContainer.Inject] private SceneLoaderService _sceneLoader;
        [VContainer.Inject] private GASFarmDefense.UIToolkit.Core.UIManager _uiManager;
        [VContainer.Inject] private Abel.TranHuongDao.Core.TDHandService _handService;
        private Abel.TranHuongDao.Core.UnitsConfig _unitsConfig;
        [VContainer.Inject] private Abel.TranHuongDao.Core.ITowerManager _towerManager;
        [VContainer.Inject] private Abel.TowerDefense.Config.UnitRenderDatabase _renderDatabase;
        [VContainer.Inject] private Abel.TranHuongDao.Core.IConfigService _configService;
        [VContainer.Inject] private FD.IEventBus _eventBus;

        private VisualElement _unitInfoPanel;
        private Label _lblInfoName;
        private Label _lblInfoHp;
        private Label _lblInfoDmg;
        private Label _lblInfoDmgType;
        private Label _lblInfoArmorType;
        private Label _lblInfoRof;
        private Label _lblInfoSpeed;
        private VisualElement _unitPortrait;
        private Button _btnInfoSell;

        private Abel.TranHuongDao.Core.UI.UIToolkitPortraitAnimator _portraitAnimator;
        private int _selectedInstanceID = -1;
        private int _selectedSellCost = 0;
        private bool _isSelectedIsTower = false;

        protected override void OnSetup()
        {
            base.OnSetup();

            _lblGold = RootElement.Q<Label>("lbl-gold");
            _lblCarrot = RootElement.Q<Label>("lbl-carrot");
            _lblPumpkin = RootElement.Q<Label>("lbl-pumpkin");
            _lblGrape = RootElement.Q<Label>("lbl-grape");

            if (_configService != null)
            {
                _unitsConfig = _configService.GetConfig<Abel.TranHuongDao.Core.UnitsConfig>();
            }

            if (_economyService != null)
            {
                _economyService.OnDataChanged += RefreshResources;
                RefreshResources();
            }

            _btnShop = RootElement.Q<Button>("btn-shop");
            if (_btnShop != null) _btnShop.clicked += OnShopClicked;

            _btnMarket = RootElement.Q<Button>("btn-market");
            if (_btnMarket != null) _btnMarket.clicked += OnMarketClicked;

            _btnFarm = RootElement.Q<Button>("btn-farm");
            if (_btnFarm != null) _btnFarm.clicked += OnFarmClicked;

            _btnGuide = RootElement.Q<Button>("btn-guide");
            if (_btnGuide != null) _btnGuide.clicked += OnGuideClicked;

            _btnMenu = RootElement.Q<Button>("btn-menu");
            if (_btnMenu != null) _btnMenu.clicked += OnMenuClicked;

            _unitInfoPanel = RootElement.Q<VisualElement>("unit-info-panel");
            _lblInfoName = RootElement.Q<Label>("info-name");
            _lblInfoHp = RootElement.Q<Label>("info-hp");
            _lblInfoDmg = RootElement.Q<Label>("info-dmg");
            _lblInfoDmgType = RootElement.Q<Label>("info-dmg-type");
            _lblInfoArmorType = RootElement.Q<Label>("info-armor-type");
            _lblInfoRof = RootElement.Q<Label>("info-rof");
            _lblInfoSpeed = RootElement.Q<Label>("info-speed");
            _unitPortrait = RootElement.Q<VisualElement>("unit-portrait");
            _btnInfoSell = RootElement.Q<Button>("btn-sell");

            if (_unitPortrait != null)
            {
                _portraitAnimator = new Abel.TranHuongDao.Core.UI.UIToolkitPortraitAnimator(_unitPortrait);
            }

            if (_btnInfoSell != null)
            {
                _btnInfoSell.clicked += OnSellClicked;
            }

            if (_eventBus != null)
            {
                _eventBus.Subscribe<Abel.TranHuongDao.Core.TDHandService.CardAddedEvent>(OnCardAdded);
                _eventBus.Subscribe<Abel.TranHuongDao.Core.TowerSelectionManager.UnitSelectedEvent>(OnUnitSelected);
                _eventBus.Subscribe<Abel.TranHuongDao.Core.TowerSelectionManager.UnitDeselectedEvent>(OnUnitDeselected);
            }

            if (_handService != null)
            {
                // Initial hand setup
                foreach (var cardID in _handService.CurrentCards)
                {
                    // For initial setup, we don't need animation
                    _ = CreateCardUI(cardID, 1, Vector2.zero, animate: false);
                }
            }

            // SetupCardItems(); // Removed hardcoded
        }

        private void OnCardAdded(Abel.TranHuongDao.Core.TDHandService.CardAddedEvent evt)
        {
            _ = CreateCardUI(evt.TowerID, evt.Tier, evt.StartScreenPos, animate: true);
        }

        private void RefreshResources()
        {
            if (_economyService == null) return;
            if (_lblGold != null) _lblGold.text = _economyService.Gold.ToString();
            if (_lblCarrot != null) _lblCarrot.text = _economyService.Inventory["Carrot"].ToString();
            if (_lblPumpkin != null) _lblPumpkin.text = _economyService.Inventory["Pumpkin"].ToString();
            if (_lblGrape != null) _lblGrape.text = _economyService.Inventory["Grape"].ToString();
        }

        [SerializeField] private VisualTreeAsset _cardTemplate;
        private List<TDCardItemUI> _cards = new List<TDCardItemUI>();

        private async UniTaskVoid CreateCardUI(string towerID, int tier, Vector2 startScreenPos, bool animate)
        {
            _cardTemplate = Resources.Load<VisualTreeAsset>("UI/Templates/TDCardItemUI");
            var cardContainer = RootElement.Q<VisualElement>("card-container");
            if (cardContainer == null || _cardTemplate == null) return;

            var cardUI = new TDCardItemUI(_cardTemplate, _towerDragManager);

            // Get Display Name from UnitsConfig if available

            string displayName = towerID;
            if (_unitsConfig != null && _unitsConfig.TryGetConfig(towerID, out var config))
            {
                displayName = config.UnitID; // Using UnitID as name
            }


            cardUI.SetData(displayName, tier, towerID);
            cardUI.OnCardPlayed += (c, pos) => Debug.Log($"Card Played at {pos}");


            var root = cardUI.Root;
            cardContainer.Add(root);
            _cards.Add(cardUI);

            if (animate)
            {
                // Wait for layout to calculate the target position
                await UniTask.WaitForEndOfFrame();

                // Calculate the offset from the click position to the final layout position
                // Note: startScreenPos is in world space. root.worldBound gives current world position.
                Vector2 targetPos = root.worldBound.center;
                Vector2 offset = startScreenPos - targetPos;

                // Set initial "flying" state
                root.AddToClassList("card-flying");
                root.style.translate = new Translate(offset.x, offset.y, 0);
                root.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1));
                root.style.opacity = 0.5f;

                // Wait one frame to start the transition
                await UniTask.WaitForEndOfFrame();

                root.RemoveFromClassList("card-flying");
                root.AddToClassList("card-spawn-animate");
                root.style.translate = new Translate(0, 0, 0);
                root.style.scale = new Scale(Vector3.one);
                root.style.opacity = 1f;

                // Clean up animation class after it's done

                await UniTask.Delay(600);
                root.RemoveFromClassList("card-spawn-animate");
            }
        }

        private void OnShopClicked()
        {
            Debug.Log("Open Shop Popup");
            _popupManager.ShowPopup<ShopPopup>().Forget();
        }

        private void OnMarketClicked()
        {
            Debug.Log("Open Market Popup");
            _popupManager.ShowPopup<MarketPopup>().Forget();
        }

        private void OnFarmClicked()
        {
            Debug.Log("Open Farm Popup");
            _popupManager.ShowPopup<FarmPopup>().Forget();
        }

        private void OnGuideClicked()
        {
            Debug.Log("Open Guide Popup");
            _popupManager.ShowPopup<GuidePopup>().Forget();
        }

        private void OnMenuClicked()
        {
            Debug.Log("Return to Main Menu... Unloading Map.");
            _sceneLoader.ReturnToHome().Forget();
        }

        private void OnUnitSelected(Abel.TranHuongDao.Core.TowerSelectionManager.UnitSelectedEvent evt)
        {
            if (_unitInfoPanel != null) _unitInfoPanel.RemoveFromClassList("panel-hidden");

            _selectedInstanceID = evt.InstanceID;
            _selectedSellCost = Mathf.FloorToInt(evt.Config.BuildCost * 0.5f);
            _isSelectedIsTower = evt.IsTower;

            if (_btnInfoSell != null)
            {
                _btnInfoSell.style.display = evt.IsTower ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_lblInfoName != null) _lblInfoName.text = evt.Config.UnitID;
            if (_lblInfoDmgType != null) _lblInfoDmgType.text = evt.Config.AttackType.ToString();
            if (_lblInfoArmorType != null) _lblInfoArmorType.text = evt.Config.ArmorType.ToString();

            if (evt.ASC != null)
            {
                _currentStatsLoopID++;
                UpdateStatsLoop(evt.ASC, _currentStatsLoopID).Forget();
            }

            if (_portraitAnimator != null && _renderDatabase != null)
            {
                var renderProfile = _renderDatabase.GetUnitByID(evt.Config.UnitRenderID);
                if (renderProfile != null && renderProfile.animData != null)
                {
                    if (renderProfile.animData.GetAnim(Abel.TowerDefense.Config.UnitAnimState.Idle, out var idleClip))
                    {
                        float speed = idleClip.speedModifier > 0f ? idleClip.speedModifier : 1f;
                        _portraitAnimator.PlayAnimation(renderProfile.animData.textureArray, idleClip.startFrame, idleClip.frameCount, idleClip.fps * speed);
                    }
                    else if (renderProfile.animData.animations.Count > 0)
                    {
                        var first = renderProfile.animData.animations[0];
                        float speed = first.speedModifier > 0f ? first.speedModifier : 1f;
                        _portraitAnimator.PlayAnimation(renderProfile.animData.textureArray, first.startFrame, first.frameCount, first.fps * speed);
                    }
                }
                else
                {
                    _portraitAnimator.Clear();
                }
            }
        }

        private int _currentStatsLoopID = 0;

        private async UniTaskVoid UpdateStatsLoop(GAS.AbilitySystemComponent asc, int loopID)
        {
            while (loopID == _currentStatsLoopID && asc != null)
            {
                var attrs = asc.GetAttributeSet<Abel.TranHuongDao.Core.UnitAttributeSet>();
                if (attrs != null)
                {
                    if (_lblInfoHp != null) _lblInfoHp.text = $"{Mathf.CeilToInt(attrs.Health.CurrentValue)}/{Mathf.CeilToInt(attrs.MaxHealth.CurrentValue)}";
                    if (_lblInfoDmg != null) _lblInfoDmg.text = Mathf.CeilToInt(attrs.Damage.CurrentValue).ToString();
                    if (_lblInfoRof != null) _lblInfoRof.text = attrs.ROF.CurrentValue.ToString("0.##");
                    if (_lblInfoSpeed != null) _lblInfoSpeed.text = attrs.MoveSpeed.CurrentValue <= 0 ? "N/A" : attrs.MoveSpeed.CurrentValue.ToString("0.##");
                }
                await UniTask.Yield();
            }
        }

        private void OnUnitDeselected(Abel.TranHuongDao.Core.TowerSelectionManager.UnitDeselectedEvent evt)
        {
            if (_unitInfoPanel != null) _unitInfoPanel.AddToClassList("panel-hidden");
            if (_portraitAnimator != null) _portraitAnimator.Stop();
            _selectedInstanceID = -1;
            _currentStatsLoopID++;
        }

        private void OnSellClicked()
        {
            if (_selectedInstanceID != -1 && _isSelectedIsTower)
            {
                if (_economyService != null) _economyService.AddGold(_selectedSellCost);
                if (_towerManager != null) _towerManager.RemoveTower(_selectedInstanceID);
                
                if (_eventBus != null) {
                    _eventBus.Publish(new Abel.TranHuongDao.Core.TowerSelectionManager.UnitDeselectedEvent());
                } else {
                    OnUnitDeselected(new Abel.TranHuongDao.Core.TowerSelectionManager.UnitDeselectedEvent());
                }
            }
        }
    }
}
