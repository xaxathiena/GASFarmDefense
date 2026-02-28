using UnityEngine;
using System.Collections.Generic;

namespace Abel.TranHuongDao.Core
{
    [CreateAssetMenu(fileName = "UnitsConfig", menuName = "Abel/TranHuongDao/Units Config Database")]
    public class UnitsConfig : ScriptableObject
    {
        [Header("Runtime Database")]
        public List<UnitConfigData> unitEntries = new List<UnitConfigData>();

        /// <summary>
        /// Hàm tiện ích để tra cứu Config bằng ID với tốc độ cao (O(N) nhưng N nhỏ).
        /// Có thể tối ưu thành Dictionary lúc Runtime khởi tạo nếu số lượng Unit quá lớn.
        /// </summary>
        public bool TryGetConfig(string id, out UnitConfigData configData)
        {
            foreach (var entry in unitEntries)
            {
                if (entry.UnitID == id)
                {
                    configData = entry;
                    return true;
                }
            }
            configData = default;
            return false;
        }
    }
}