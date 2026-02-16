using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Abel/Unit Anim Data")]
public class UnitAnimData : ScriptableObject
{
    [Header("Texture Data")]
    public Texture2DArray textureArray; // Tự động link texture vào đây

    [Header("Animation Metadata")]
    public List<AnimInfo> animations = new List<AnimInfo>();
[System.Serializable]
    public struct AnimInfo
    {
        public string animName;
        public int startFrame;
        public int frameCount;
        public float fps;
        public float duration;
        public bool loop;
        
        // --- THÊM BIẾN NÀY ---
        [Range(0.1f, 20f)]
        public float speedModifier; // 1 = Chuẩn, 2 = Nhanh gấp đôi, 0.5 = Chậm một nửa
        // --- THÊM BIẾN NÀY ---
        [Range(0.1f, 10.0f)]
        public float scale;         // Kích thước riêng (Mặc định là 1)
    }

    // Hàm tiện ích để tìm Animation theo tên lúc Runtime
    public bool GetAnim(string name, out AnimInfo info)
    {
        foreach (var a in animations)
        {
            if (a.animName == name)
            {
                info = a;
                return true;
            }
        }
        info = default;
        return false;
    }
}