using UnityEngine;
using System.Collections.Generic;
using Effekseer;

public class CreateVFXText : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Danh sách các Effekseer Effect Asset cần test")]
    public List<EffekseerEffectAsset> effectAssets = new List<EffekseerEffectAsset>();

    [Tooltip("Khoảng cách giữa các VFX")]
    public float spacing = 2.0f;

    void Start()
    {
        for (int i = 0; i < effectAssets.Count; i++)
        {
            var asset = effectAssets[i];
            if (asset == null) continue;

            // Tạo GameObject với tên của asset
            GameObject go = new GameObject(asset.name);

            // Set parent để dọn dẹp các object được tạo ra và sắp xếp vị trí cách nhau
            go.transform.SetParent(transform);
            go.transform.position = transform.position + new Vector3(i * spacing, 0, 0);

            // Add component EffekseerEmitter và setting như hình yêu cầu
            var emitter = go.AddComponent<EffekseerEmitter>();

            emitter.effectAsset = asset;
            emitter.playOnStart = true;
            emitter.isLooping = true;
            emitter.TimingOfUpdate = EffekseerEmitterTimingOfUpdate.Update;
            emitter.EmitterScale = EffekseerEmitterScale.Local;
            emitter.TimeScale = EffekseerTimeScale.Scale;
        }
    }
}
