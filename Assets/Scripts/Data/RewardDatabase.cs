using System.Collections.Generic;
using UnityEngine;

namespace MiniGameDemo.Data
{
    /// <summary>
    /// ScriptableObject that holds all 35 RewardItemData assets.
    ///
    /// Create via: Assets → Create → MiniGameDemo → Reward Database
    ///
    /// How to populate:
    ///   1. Create one RewardItemData asset per reward (35 total) via
    ///      Assets → Create → MiniGameDemo → Reward Item.
    ///   2. Assign each asset into the AllRewards list below.
    ///   3. Assign this RewardDatabase asset to WheelController._rewardDatabase in the Inspector.
    ///
    /// The WheelController will draw DISTINCT items from this list for each wheel spin.
    /// Bomb items (RewardType.Bomb) are filtered out automatically — the bomb is injected
    /// separately by WheelController based on zone tier.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardDatabase", menuName = "MiniGameDemo/Reward Database")]
    public class RewardDatabase : ScriptableObject
    {
        [Tooltip("Drag all 35 RewardItemData assets here. " +
                 "Do NOT include the Bomb item — it is managed separately in WheelConfigData.")]
        public List<RewardItemData> AllRewards = new List<RewardItemData>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AllRewards == null) return;

            // Warn if list has fewer than the expected 35 items
            if (AllRewards.Count < 35)
                Debug.LogWarning($"[RewardDatabase] '{name}': Only {AllRewards.Count} items assigned. " +
                                 "Expected 35. Add more RewardItemData assets.");

            // Warn if any bomb items are accidentally included
            foreach (var r in AllRewards)
            {
                if (r == null) continue;
                if (r.rewardType == Core.RewardType.Bomb)
                    Debug.LogWarning($"[RewardDatabase] '{name}': Contains a Bomb-type item '{r.name}'. " +
                                     "Bomb items should be assigned to WheelConfigData.bombReward instead.");
            }
        }
#endif
    }
}
