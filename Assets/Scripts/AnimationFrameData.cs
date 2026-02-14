using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AnimationClipInfo
{
    public string animationName;
    public int startFrame;
    public int endFrame;
    public int frameCount => endFrame - startFrame + 1;

    public AnimationClipInfo(string name, int start, int end)
    {
        animationName = name;
        startFrame = start;
        endFrame = end;
    }
}

[CreateAssetMenu(fileName = "AnimationFrameData", menuName = "Spine Baker/Animation Frame Data")]
public class AnimationFrameData : ScriptableObject
{
    public Texture2DArray textureArray;
    public List<AnimationClipInfo> animations = new List<AnimationClipInfo>();
    
    public AnimationClipInfo GetAnimationByName(string name)
    {
        return animations.Find(a => a.animationName == name);
    }
    
    public string GetSummary()
    {
        string summary = $"Total Frames: {textureArray?.depth ?? 0}\n";
        summary += $"Resolution: {textureArray?.width ?? 0}x{textureArray?.height ?? 0}\n\n";
        summary += "Animation Ranges:\n";
        
        foreach (var anim in animations)
        {
            summary += $"  {anim.animationName}: Frames {anim.startFrame}-{anim.endFrame} ({anim.frameCount} frames)\n";
        }
        
        return summary;
    }
}
