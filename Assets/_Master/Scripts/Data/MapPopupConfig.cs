using System;
using UnityEngine.UIElements;

namespace Abel.TranHuongDao.Core
{
    [Serializable]
    public class MapPopupConfig
    {
        public VisualTreeAsset Uxml;
        public string PopupClassName; // Name of the class inheriting from PopupBase
    }
}
