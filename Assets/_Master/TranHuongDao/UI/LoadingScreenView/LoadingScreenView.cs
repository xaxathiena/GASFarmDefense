using Cysharp.Threading.Tasks;
using GASFarmDefense.UIToolkit.Core;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class LoadingScreenView : ViewBase
    {
        private VisualElement _progressBar;
        private Label _progressLabel;

        protected override void OnSetup()
        {
            base.OnSetup();

            _progressBar = RootElement.Q<VisualElement>("bar-progress");
            _progressLabel = RootElement.Q<Label>("lbl-progress");
        }

        protected override void OnBeforeShow()
        {
            SetProgress(0f);
            SimulateLoading(); // Mock loading
        }

        public void SetProgress(float normalized)
        {
            if (_progressBar != null)
                _progressBar.style.width = new Length(normalized * 100f, LengthUnit.Percent);

            if (_progressLabel != null)
                _progressLabel.text = $"{(int)(normalized * 100)} %";
        }

        private async void SimulateLoading()
        {
            float prog = 0f;
            while (prog < 1f)
            {
                await Cysharp.Threading.Tasks.UniTask.Delay(100);
                prog += 0.05f;
                SetProgress(UnityEngine.Mathf.Clamp01(prog));
            }
        }
    }
}
