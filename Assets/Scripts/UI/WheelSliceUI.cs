using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Data;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Displays a single collected reward in the inventory panel.
    /// Icon and amount are set at runtime — fields use _value suffix per spec.
    /// </summary>
    public class WheelSliceUI : MonoBehaviour
    {
        [SerializeField] private Image           img_reward_icon_value;
        [SerializeField] private TextMeshProUGUI txt_reward_amount_value;

        /// <summary>Populates this inventory item with reward data.</summary>
        public void Setup(RewardItemData reward, int amount)
        {
            if (img_reward_icon_value != null)
                img_reward_icon_value.sprite = reward.icon;

            if (txt_reward_amount_value != null)
                txt_reward_amount_value.text = reward.rewardType == Core.RewardType.Bomb
                    ? string.Empty
                    : $"x{amount}";
        }
    }
}
