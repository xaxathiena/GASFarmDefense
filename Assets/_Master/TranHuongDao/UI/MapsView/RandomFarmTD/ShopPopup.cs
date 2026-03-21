using GASFarmDefense.UIToolkit.Core;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class ShopPopup : PopupBase
    {
        private Button _btnClose;

        protected override void OnSetup()
        {
            base.OnSetup();
            _btnClose = RootElement.Q<Button>("btn-close");
            if (_btnClose != null) _btnClose.clicked += Close;
        }
    }
}
