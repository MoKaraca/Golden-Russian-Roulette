using UnityEngine;

namespace MiniGameDemo.Data
{
    [CreateAssetMenu(fileName = "NewRewardItem", menuName = "MiniGameDemo/Reward Item")]
    public class RewardItemData : ScriptableObject
    {
        public string itemId;
        public Core.RewardType rewardType;
        public Sprite icon;
        [Tooltip("The base amount this reward yields at zone 1.")]
        public int baseAmount = 1;
        [Tooltip("Relative weight for probability calculation. Higher weight means more likely to drop.")]
        public float baseWeight = 1f;

        [Header("Scaling")]
        [Tooltip("How much the amount increases per zone.")]
        public float amountMultiplierPerZone = 0.1f;
        
        public int GetAmountForZone(int zone)
        {
            if (rewardType == Core.RewardType.Bomb) return 0;
            if (rewardType == Core.RewardType.Weapon) return 1;
            
            // Simple formula: base + base * multiplier * (zone - 1)
            float scaledAmount = baseAmount + (baseAmount * amountMultiplierPerZone * (zone - 1));
            return Mathf.RoundToInt(scaledAmount);
        }
    }
}
