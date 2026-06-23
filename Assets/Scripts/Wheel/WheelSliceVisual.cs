using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Data;

namespace MiniGameDemo.Wheel
{
    /// <summary>
    /// Visual for a single slice on the spinning wheel.
    /// The ROOT Image (background) is set to transparent by WheelController on spawn.
    /// Only img_icon_value (child) shows the reward sprite.
    /// </summary>
    public class WheelSliceVisual : MonoBehaviour
    {
        [SerializeField] private Image           img_icon_value;
        [SerializeField] private TextMeshProUGUI txt_amount_value;

        public void Setup(RewardItemData reward, int amount)
        {
            if (img_icon_value != null)
            {
                img_icon_value.sprite           = reward.icon;
                img_icon_value.preserveAspect   = true;
                // No background color on the icon image — fully opaque white means the sprite renders cleanly
                img_icon_value.color            = Color.white;

                // Make icon fill its RectTransform fully
                var iconRt = img_icon_value.GetComponent<RectTransform>();
                if (iconRt != null)
                {
                    iconRt.anchorMin        = Vector2.zero;
                    iconRt.anchorMax        = Vector2.one;
                    iconRt.offsetMin        = Vector2.zero;
                    iconRt.offsetMax        = Vector2.zero;
                }
            }

            if (txt_amount_value != null)
            {
                txt_amount_value.text      = reward.rewardType == Core.RewardType.Bomb
                    ? string.Empty
                    : $"x{amount}";
                txt_amount_value.fontSize  = 12f;
                txt_amount_value.color     = Color.white;

                // Pin text to bottom of slice cell
                var textRt = txt_amount_value.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin = new Vector2(0f, 0f);
                    textRt.anchorMax = new Vector2(1f, 0.3f);
                    textRt.offsetMin = Vector2.zero;
                    textRt.offsetMax = Vector2.zero;
                }
            }
        }
    }
}
