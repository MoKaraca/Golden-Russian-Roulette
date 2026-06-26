using System.Collections.Generic;
using UnityEngine;

namespace MiniGameDemo.Data
{
    // ScriptableObject that holds the AllRewards list of RewardItemData assets. 
    // The WheelController will draw DISTINCT items from this list for each wheel spin.
    [CreateAssetMenu(fileName = "RewardDatabase", menuName = "GameDevDemo/Reward Database")]
    public class RewardDatabase : ScriptableObject
    {
        [Tooltip("Bring all RewardItemData assets here. Do NOT include the Bomb item — it is managed separately in WheelConfigData.")]
        public List<RewardItemData> AllRewards = new List<RewardItemData>();
    }
    
    #if Unity_EDITOR
    private void OnValidate()
    {
        // Warn if any bomb items are accidentally included
        foreach (var r in AllRewards)
        {
            if (r == null) continue;
            if (r.rewardType == Data.RewardType.Bomb)
                Debug.LogWarning($"[RewardDatabase] '{name}': Contains a Bomb-type item '{r.name}'. " +
                                 "Bomb items should be assigned to WheelConfigData.bombReward instead.");
        }
    }
    #endif
}
