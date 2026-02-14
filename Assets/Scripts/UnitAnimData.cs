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
        public string animName;    // Tên (Attack, Idle...)
        public int startFrame;     // Bắt đầu từ slice nào
        public int frameCount;     // Dài bao nhiêu frame
        public float fps;          // Tốc độ gốc
        public float duration;     // Thời lượng (giây)
        public bool loop;          // (Tùy chọn)
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