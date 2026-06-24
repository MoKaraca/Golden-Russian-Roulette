using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// A single compact badge in the zone progress strip.
    /// The background is transparent by default; only the current zone gets a coloured highlight.
    /// </summary>
    public class ZoneItemUI : MonoBehaviour
    {
        [SerializeField] private Image           img_bg_value;
        [SerializeField] private TextMeshProUGUI txt_zone_number_value;

        // Compact fixed size for each badge (set by ZoneMapUIController at runtime)
        private static readonly Vector2 BADGE_SIZE = new Vector2(40f, 50f);

        public void Setup(int zoneNumber, Color bgColor, bool isCurrent)
        {
            // Set fixed badge size regardless of prefab defaults
            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = BADGE_SIZE;

            if (txt_zone_number_value != null)
            {
                txt_zone_number_value.text      = zoneNumber.ToString();
                txt_zone_number_value.fontSize  = 14f;
                txt_zone_number_value.alignment = TextAlignmentOptions.Center;
                // Current zone text = white, others = light grey
                txt_zone_number_value.color = isCurrent ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            if (img_bg_value != null)
            {
                // Only the current zone gets a visible background pill
                img_bg_value.color = isCurrent ? bgColor : Color.clear;
            }

            // Scale up the current zone badge slightly for emphasis
            transform.localScale = Vector3.zero;
            float targetScale = isCurrent ? 1.2f : 1f;
            transform.DOScale(targetScale, 0.4f).SetEase(DG.Tweening.Ease.OutBack);
        }
    }
}
