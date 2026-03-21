using System;
using System.Collections.Generic;
using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    [Serializable]
    public class TagVFXData
    {
        [Tooltip("The tag that triggers this VFX")]
        public GameplayTag tag;
        
        [Tooltip("The VFX ID registered in VFXManager")]
        public string vfxID;
        
        [Tooltip("Offset relative to the unit's position")]
        public Vector3 offset;
        
        [Tooltip("Higher priority VFX might overlay lower ones if needed")]
        public int priority;
    }

    [CreateAssetMenu(fileName = "TagVFXConfig", menuName = "GASFD/Config/Tag VFX Config")]
    public class TagVFXConfig : BaseConfigSO
    {
        [SerializeField] private List<TagVFXData> vfxMappings = new List<TagVFXData>();
        
        // Fast O(1) lookup dictionary
        private Dictionary<GameplayTag, TagVFXData> _vfxDict;

        public override void InitializeConfig()
        {
            base.InitializeConfig();
            
            _vfxDict = new Dictionary<GameplayTag, TagVFXData>();
            foreach (var mapping in vfxMappings)
            {
                if (!_vfxDict.ContainsKey(mapping.tag))
                {
                    _vfxDict.Add(mapping.tag, mapping);
                }
                else
                {
                    Debug.LogWarning($"[TagVFXConfig] Duplicate mapping found for tag: {mapping.tag}");
                }
            }
        }

        public TagVFXData GetVFXData(GameplayTag tag)
        {
            if (_vfxDict != null && _vfxDict.TryGetValue(tag, out var data))
            {
                return data;
            }
            return null;
        }
    }
}
