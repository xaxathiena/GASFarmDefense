using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class MainMenuView : ViewBase
    {
        private Button _btnPlay;
        [VContainer.Inject] private ViewManager _viewManager;

        protected override void OnSetup()
        {
            base.OnSetup();

            _btnPlay = RootElement.Q<Button>("btn-play");
            if (_btnPlay != null)
            {
                _btnPlay.clicked += OnPlayClicked;
            }
        }

        private void OnPlayClicked()
        {
            UnityEngine.Debug.Log("Play Game Clicked. Switching to MapSelectionView...");
            _viewManager.SwitchView<MapSelectionView>().Forget();
        }

        public override async UniTask Hide()
        {
            var rightPanel = RootElement.Q<VisualElement>("right-panel");
            if (rightPanel != null)
            {
                rightPanel.RemoveFromClassList("slide-visible");
                rightPanel.AddToClassList("slide-right-hidden");
                // Wait for the slide animation to finish before letting the base Hide set display=None
                await UniTask.Delay(200); 
            }
            await base.Hide();
        }

        public override async UniTask Show()
        {
            await base.Show();
            var rightPanel = RootElement.Q<VisualElement>("right-panel");
            if (rightPanel != null)
            {
                rightPanel.RemoveFromClassList("slide-right-hidden");
                rightPanel.AddToClassList("slide-visible");
            }
        }
    }
}
