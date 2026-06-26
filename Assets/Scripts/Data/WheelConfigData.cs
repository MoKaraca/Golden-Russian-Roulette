using UnityEngine;
using System.Collections.Generic;
namespace MiniGameDemo.Data
{
    // ScriptableObject that holds the AllRewards list of RewardItemData assets. 
    // The WheelController will draw DISTINCT items from this list for each wheel spin.
    [System.Serializable]
    public class ZoneRules
    {
        public int sliceCount = 8;
        public List <RewardItemData> possibleRewards;
    }
    [CreateAssetMenu(fileName = "WheelConfig", menuName = "GameDevDemo/Wheel Config")]
    public class WheelConfigData : ScriptableObject
    {
        public RewardItemData bombReward;

        public ZoneRules standardZoneRules;
        public ZoneRules safeZoneRules;
        public ZoneRules superZoneRules;

        public int safeZoneInterval = 5;
        public int superZoneInterval = 30;

        public Core.ZoneTier GetTierForZone(int zoneIndex)
        {
            if (zoneIndex > 0 && zoneIndex % superZoneInterval == 0) return Core.ZoneTier.Super;
            if (zoneIndex > 0 && zoneIndex % safeZoneInterval  == 0) return Core.ZoneTier.Safe;
            return Core.ZoneTier.Standard;
        }
        public ZoneRules GetRulesForZone(int zoneIndex)
        {
            switch(GetTierForZone(zoneIndex))
            {
                case Core.ZoneTier.Safe: return safeZoneRules;
                case Core.ZoneTier.Super: return superZoneRules;
                default: return standardZoneRules;
            }
        }
    #if UNITY_EDITOR
        private void OnValidate()
        {   
            if (bombReward == null)
                Debug.LogWarning($"[WheelConfigData] '{name}': bombReward is not assigned. Assign a Bomb-type RewardItemData asset.");
        }
    #endif

    }
}
