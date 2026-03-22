using System;
using UnityEngine.UIElements;

namespace Abel.TranHuongDao.Core
{
    [Serializable]
    public class MapPopupConfig
    {
        public string PopupId;
        public VisualTreeAsset Uxml;
        public string PopupClassName; // Optional: Full class name if different from default
    }
}
