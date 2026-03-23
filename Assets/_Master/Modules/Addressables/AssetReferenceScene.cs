using System;
using UnityEngine.AddressableAssets;

namespace GASFarmDefense.Modules.Addressables
{
    /// <summary>
    /// Specialized AssetReference for Scene assets. 
    /// Used to provide better filtering in the Inspector and fix compilation errors where this type is expected but missing from core package.
    /// </summary>
    [Serializable]
    public class AssetReferenceScene : AssetReference
    {
        public AssetReferenceScene(string guid) : base(guid)
        {
        }
    }
}
