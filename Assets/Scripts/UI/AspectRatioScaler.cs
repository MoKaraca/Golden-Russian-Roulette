using UnityEngine;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Scales up a RectTransform on wider aspect ratios (like 20:9) so it doesn't look tiny.
    /// Safely fixes UI scaling without touching the Canvas Scaler or breaking anchors.
    /// 
    /// Attach this to ui_panel_wheel_root (or any other object you want to scale up).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AspectRatioScaler : MonoBehaviour
    {
        [Tooltip("The reference aspect ratio this UI was built for (e.g. 16/9 = 1.77)")]
        [SerializeField] private float _referenceAspect = 1.777f;

        [Tooltip("Maximum scale multiplier to apply on ultra-wide screens to prevent it getting too huge")]
        [SerializeField] private float _maxScale = 1.5f;

        private void Start()
        {
            float currentAspect = (float)Screen.width / Screen.height;

            // If the screen is wider than 16:9 (like 20:9)
            if (currentAspect > _referenceAspect)
            {
                // Calculate how much wider the screen is
                float ratio = currentAspect / _referenceAspect;
                
                // Scale proportionally but clamp it
                float newScale = Mathf.Clamp(ratio, 1f, _maxScale);
                
                transform.localScale = new Vector3(newScale, newScale, 1f);
            }
        }
    }
}
