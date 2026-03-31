using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abel.TranHuongDao.Core.UI
{
    // ─────────────────────────────────────────────────────────────────────────────
    // UIToolkitPortraitAnimator
    //
    // A robust animator for UI Toolkit that streams slices of a Texture2DArray
    // into a standard Texture2D using instant VRAM Graphics.CopyTexture.
    // This avoids needing complex custom MeshGeneration features in UI Toolkit.
    // ─────────────────────────────────────────────────────────────────────────────
    public class UIToolkitPortraitAnimator
    {
        private VisualElement _targetElement;
        private Texture2D _streamTexture;
        private Texture2DArray _currentArray;
        private CancellationTokenSource _cts;

        private int _startFrame;
        private int _frameCount;
        private float _fps;

        public UIToolkitPortraitAnimator(VisualElement targetElement)
        {
            _targetElement = targetElement;
        }

        public void PlayAnimation(Texture2DArray texArray, int startFrame, int frameCount, float fps)
        {
            if (texArray == null) return;

            Stop();

            _currentArray = texArray;
            _startFrame = startFrame;
            _frameCount = Mathf.Max(1, frameCount);
            _fps = fps;

            // Reconstruct stream texture if dimensions or format differ.
            // texArray.mipmapCount > 1 ensures we match the mip chain architecture for CopyTexture.
            if (_streamTexture == null ||

                _streamTexture.width != texArray.width ||

                _streamTexture.height != texArray.height ||

                _streamTexture.format != texArray.format)
            {
                if (_streamTexture != null) Object.Destroy(_streamTexture);

                _streamTexture = new Texture2D(texArray.width, texArray.height, texArray.format, texArray.mipmapCount > 1, true);
                _targetElement.style.backgroundImage = new StyleBackground(_streamTexture);
            }

            _cts = new CancellationTokenSource();
            _ = AnimationLoop(_cts.Token);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public void Clear()
        {
            Stop();
            _targetElement.style.backgroundImage = null;
        }

        private async UniTaskVoid AnimationLoop(CancellationToken token)
        {
            float startTime = Time.unscaledTime;


            while (!token.IsCancellationRequested)
            {
                if (_currentArray == null || _streamTexture == null) break;

                float elapsed = Time.unscaledTime - startTime;
                int localFrame = (int)(elapsed * _fps) % _frameCount;
                int sliceIndex = _startFrame + localFrame;

                // Safely copy the active slice straight from GPU memory into the stream texture.
                // We only copy mip 0 across, though the engine complains if mip count structures don't match.
                try

                {
                    Graphics.CopyTexture(_currentArray, sliceIndex, 0, _streamTexture, 0, 0);
                    _targetElement.MarkDirtyRepaint(); // Force UI update
                }

                catch (System.Exception ex)

                {
                    Debug.LogWarning($"[UIToolkitPortraitAnimator] CopyTexture failed: {ex.Message}");
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}
