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
            
            // Calculate the raw increase per zone
            float increasePerZone = baseAmount * amountMultiplierPerZone;
            
            // Fix: If there is supposed to be scaling, ensure it increases by AT LEAST 1 per zone.
            // Otherwise, items with a baseAmount of 1 and a 0.1 multiplier will take 10 zones just to increase by 1!
            if (amountMultiplierPerZone > 0f && increasePerZone < 1f)
            {
                increasePerZone = 1f;
            }

            float scaledAmount = baseAmount + (increasePerZone * (zone - 1));
            return Mathf.RoundToInt(scaledAmount);
        }
    }
}
