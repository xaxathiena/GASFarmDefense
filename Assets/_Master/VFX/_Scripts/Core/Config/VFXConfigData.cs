using System;
using UnityEngine;
using Effekseer;

namespace FD.Modules.VFX
{
    [Serializable]
    public class VFXConfigData
    {
        [Tooltip("Tên định danh (vd: fire_explosion, ice_spike)")]
        public string VfxID;

        [Tooltip("File asset chính của Effekseer (.efkproj đã import)")]
        public EffekseerEffectAsset EffectAsset;

        [Header("Base Parameters")]
        [Tooltip("Tỉ lệ scale mặc định")]
        public float Scale = 1f;

        [Tooltip("Tốc độ chạy (1: bình thường, <1: chậm, >1: nhanh)")]
        public float Speed = 1f;

        [Tooltip("Có tự động lặp không? (thường dùng cho Aura / Trail / Đạn bay)")]
        public bool IsLoop;

        [Tooltip("Có load sẵn vào RAM khi bật game để tránh giật lag lúc spawn?")]
        public bool Preload = true;

        [Tooltip("Thời gian sống thủ công (> 0: Tự Hủy sau x giây, -1: theo vòng đời gốc)")]
        public float Duration = -1f;
    }
}
