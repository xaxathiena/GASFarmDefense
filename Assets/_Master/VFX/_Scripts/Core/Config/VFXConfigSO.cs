using System.Collections.Generic;
using UnityEngine;

namespace FD.Modules.VFX
{
    [CreateAssetMenu(fileName = "VFXConfig", menuName = "FD/Config/VFXConfig")]
    public class VFXConfigSO : Abel.TranHuongDao.Core.BaseConfigSO
    {
        public List<VFXConfigData> VFXList = new List<VFXConfigData>();

        private Dictionary<string, VFXConfigData> _vfxDict;

        /// <summary>
        /// Khởi tạo Dictionary để tra cứu O(1) khi chạy. Cần được gọi 1 lần khi load Config.
        /// </summary>
        public override void InitializeConfig()
        {
            if (_vfxDict == null)
            {
                _vfxDict = new Dictionary<string, VFXConfigData>(VFXList.Count);
                foreach (var data in VFXList)
                {
                    if (!string.IsNullOrEmpty(data.VfxID) && !_vfxDict.ContainsKey(data.VfxID))
                    {
                        _vfxDict.Add(data.VfxID, data);
                    }
                    else
                    {
                        Debug.LogWarning($"[VFXConfig] Duplicate or empty VfxID: {data.VfxID}");
                    }
                }
            }
        }

        public bool TryGetConfig(string vfxID, out VFXConfigData data)
        {
            if (_vfxDict == null)
            {
                InitializeConfig();
            }
            return _vfxDict.TryGetValue(vfxID, out data);
        }
    }
}
