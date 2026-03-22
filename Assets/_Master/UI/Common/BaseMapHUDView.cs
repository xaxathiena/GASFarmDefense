using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class BaseMapHUDView : ViewBase
    {
        private Button _btnSettings;
        private Button _btnBackHome;

        [Inject] private SceneLoaderService _sceneLoader;

        protected override void OnSetup()
        {
            base.OnSetup();

            _btnSettings = RootElement.Q<Button>("btn-settings");
            if (_btnSettings != null) _btnSettings.clicked += OnSettingsClicked;

            _btnBackHome = RootElement.Q<Button>("btn-back-home");
            if (_btnBackHome != null) _btnBackHome.clicked += OnBackHomeClicked;
        }

        private void OnSettingsClicked()
        {
            GameUIManager.Instance.PopupManager.ShowPopup("Settings").Forget();
        }

        private void OnBackHomeClicked()
        {
            _sceneLoader.ReturnToHome().Forget();
        }
    }
}
