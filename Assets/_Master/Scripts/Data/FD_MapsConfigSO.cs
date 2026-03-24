using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    [CreateAssetMenu(fileName = "MapsConfig", menuName = "Map/RandomFarmTD/Maps Config")]
    public class FD_MapsConfigSO : BaseConfigSO
    {
        public List<FD_MapConfigSO> Maps = new List<FD_MapConfigSO>();

        public FD_MapConfigSO GetMapById(string id)
        {
            return Maps.Find(m => m.MapId == id);
        }
    }
}
