using UnityEngine;
using UnityEngine.UI;

namespace Abel.TranHuongDao.Core
{
    // ─────────────────────────────────────────────────────────────────────────────
    // UIPortraitAnimator
    //
    // Drives a RawImage using the UI_Texture2DArrayAnim shader to animate a
    // Texture2DArray by advancing the _SliceIndex property each frame.
    // ─────────────────────────────────────────────────────────────────────────────
    [RequireComponent(typeof(RawImage))]
    public class UIPortraitAnimator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Shader Material")]
        [Tooltip("Drag the UI_Texture2DArrayAnim material asset here. " +
                 "A private instance will be created at runtime; the asset is never modified.")]
        [SerializeField] private Material baseMaterial;

        // ── Private state ─────────────────────────────────────────────────────────

        private RawImage       _rawImage;
        private Material       _matInstance;   // Owned by this component; destroyed in OnDestroy.

        private Texture2DArray _currentTex;    // The Texture2DArray currently bound to the material.
        private int            _startFrame;    // First slice index of the clip.
        private int            _frameCount;    // Number of slices in the clip.
        private float          _fps;           // Playback speed in frames per second.
        private float          _startTime;     // Time.unscaledTime when PlayAnimation was last called.
        private bool           _isPlaying;     // True while the animation is advancing.

        // Shader property IDs cached to avoid per-frame string hashing.
        private static readonly int SliceIndexID = Shader.PropertyToID("_SliceIndex");
        private static readonly int MainTexArrayID = Shader.PropertyToID("_MainTexArray"); // Đã update

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            // Guard: baseMaterial must be assigned in the Inspector.
            if (baseMaterial == null)
            {
                Debug.LogError("[UIPortraitAnimator] baseMaterial is not assigned. " +
                               "Drag the UI_Texture2DArrayAnim material into the Inspector.", this);
                return;
            }

            // Create an owned instance so this component never writes to the shared asset.
            _matInstance = new Material(baseMaterial);
            _rawImage.material = _matInstance;
        }

        private void Update()
        {
            if (!_isPlaying || _matInstance == null || _frameCount <= 0)
                return;

            // Calculate elapsed time without being affected by Time.timeScale.
            float elapsed = Time.unscaledTime - _startTime;

            // Determine the current slice using integer modulo to loop the clip.
            int localFrame = (int)(elapsed * _fps) % _frameCount;
            int sliceIndex = _startFrame + localFrame;

            _matInstance.SetFloat(SliceIndexID, sliceIndex);
        }

        private void OnDestroy()
        {
            // Release the material instance we own to prevent memory leaks.
            if (_matInstance != null)
            {
                Destroy(_matInstance);
                _matInstance = null;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins (or restarts) playback of a clip within the supplied Texture2DArray.
        /// The animation loops indefinitely until <see cref="Stop"/> is called.
        /// </summary>
        public void PlayAnimation(Texture2DArray texArray, int startFrame, int frameCount, float fps)
        {
            if (_matInstance == null)
            {
                Debug.LogWarning("[UIPortraitAnimator] Cannot play — material instance is null. " +
                                 "Check that baseMaterial is assigned.", this);
                return;
            }

            // Swap the texture only when it actually changes to avoid redundant GPU upload.
            if (_currentTex != texArray)
            {
                _currentTex = texArray;
                
                // ĐÃ FIX CƠ CHẾ LỪA CANVAS: Truyền vào biến _MainTexArray thay vì _MainTex
                _matInstance.SetTexture(MainTexArrayID, _currentTex);
            }

            _startFrame  = startFrame;
            _frameCount  = Mathf.Max(1, frameCount); // Guard against zero-frame division.
            _fps         = fps;
            _startTime   = Time.unscaledTime;
            _isPlaying   = true;
        }

        /// <summary>Halts frame advancement and freezes the display on the current slice.</summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>Returns true while an animation is actively advancing.</summary>
        public bool IsPlaying => _isPlaying;
    }
}