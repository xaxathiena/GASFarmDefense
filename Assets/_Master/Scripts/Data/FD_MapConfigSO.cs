using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Abel.TranHuongDao.Core
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "GASFD/Map Config")]
    public class FD_MapConfigSO : BaseConfigSO
    {
        public string MapId;
        public AssetReference MapScene;
        public string DisplayName;
        [TextArea(3, 10)]
        public string Description;
        public Sprite PreviewImage;
        public Color PreviewColor = Color.gray;
    }
}
