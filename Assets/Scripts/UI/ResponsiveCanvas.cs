using UnityEngine;
using UnityEngine.UI;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Automatically adjusts the CanvasScaler's Match Width Or Height property
    /// specifically to handle 20:9 ultrawide screens, while keeping others unchanged.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvas : MonoBehaviour
    {
        private CanvasScaler _scaler;

        [Tooltip("The match value for normal screens (e.g., 0 for Match Width)")]
        [SerializeField] private float _defaultMatch = 0f;

        [Tooltip("The match value for ultrawide 20:9 screens (User preferred 0.5)")]
        [SerializeField] private float _ultrawideMatch = 0.5f;

        private void Start()
        {
            _scaler = GetComponent<CanvasScaler>();
            UpdateScale();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                UpdateScale();
            }
        }
#endif

        private void OnRectTransformDimensionsChange()
        {
            UpdateScale();
        }

        private void UpdateScale()
        {
            if (_scaler == null) return;
            if (_scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return;

            float currentAspect = (float)Screen.width / Screen.height;

            // 20:9 aspect ratio is approximately 2.22
            // We use >= 2.1f to catch 20:9 and other ultra-wide displays
            if (currentAspect >= 2.1f)
            {
                _scaler.matchWidthOrHeight = _ultrawideMatch;
            }
            else
            {
                _scaler.matchWidthOrHeight = _defaultMatch;
            }
        }
    }
}
