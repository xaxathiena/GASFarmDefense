using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine.UIElements;
using VContainer;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class FDSettingsPopup : PopupBase
    {
        private Button _btnClose;
        private Button _btnBackHome;
        private Slider _sliderMusic;
        private Slider _sliderSfx;

        [Inject] private SceneLoaderService _sceneLoader;

        protected override void OnSetup()
        {
            base.OnSetup();

            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;

            _btnBackHome = RootElement.Q<Button>("btn-back-home");
            if (_btnBackHome != null) _btnBackHome.clicked += OnBackHomeClicked;

            _sliderMusic = RootElement.Q<Slider>("slider-music");
            _sliderSfx = RootElement.Q<Slider>("slider-sfx");
        }

        private void OnBackHomeClicked()
        {
            Close();
            _sceneLoader.ReturnToHome().Forget();
        }
    }
}
