using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Abel.TranHuongDao.Core.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _textComponent;
        
        private IObjectPool<FloatingText> _pool;
        private Vector3 _moveVector;
        private float _timeAlive;
        private float _duration;
        private Color _initialColor;

        private void Awake()
        {
            if (_textComponent == null)
                _textComponent = GetComponent<TextMeshPro>();
                
            // Ensure the text aligns exactly with the camera like the health bars do
            transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        public void Setup(IObjectPool<FloatingText> pool, string text, Color color, float scale, Vector3 direction, float duration = 1f)
        {
            _pool = pool;
            _timeAlive = 0f;
            _duration = duration;
            _moveVector = direction;
            _initialColor = color;

            if (_textComponent != null)
            {
                _textComponent.text = text;
                _textComponent.color = color;
            }

            transform.localScale = Vector3.one * scale;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;
            
            // Move along the randomized vector
            transform.position += _moveVector * Time.deltaTime;

            // Fade out over the last half of its life
            if (_timeAlive > _duration * 0.5f && _textComponent != null)
            {
                float fadeRatio = 1f - ((_timeAlive - (_duration * 0.5f)) / (_duration * 0.5f));
                Color c = _initialColor;
                c.a = fadeRatio;
                _textComponent.color = c;
            }

            // Return to pool when done
            if (_timeAlive >= _duration)
            {
                if (_pool != null)
                    _pool.Release(this);
                else
                    Destroy(gameObject); // Fallback if not pooled
            }
        }
    }
}
