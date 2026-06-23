using UnityEngine;
using System.Collections.Generic;

namespace MiniGameDemo.Data
{
    /// <summary>
    /// Defines the possible rewards and slice count for one zone tier (Standard / Safe / Super).
    /// Configure this in the WheelConfigData ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class ZoneRules
    {
        [Tooltip("Total number of slices on the wheel (for Standard zones one slot is reserved for the bomb).")]
        public int sliceCount = 8;

        [Tooltip("Pool of rewards that can appear on this wheel tier. Weighted by RewardItemData.baseWeight.")]
        public List<RewardItemData> possibleRewards;
    }

    /// <summary>
    /// ScriptableObject containing all tunable parameters for the wheel game.
    /// Create via: Assets → Create → MiniGameDemo → Wheel Config
    /// </summary>
    [CreateAssetMenu(fileName = "NewWheelConfig", menuName = "MiniGameDemo/Wheel Config")]
    public class WheelConfigData : ScriptableObject
    {
        [Tooltip("The Bomb reward item — assigned to one slice on standard zones.")]
        public RewardItemData bombReward;

        [Header("Zone Rules")]
        [Tooltip("Standard zone rules — one slice is the bomb.")]
        public ZoneRules standardZoneRules;

        [Tooltip("Safe zone rules — no bomb (silver wheel). Appears every safeZoneInterval zones.")]
        public ZoneRules safeZoneRules;

        [Tooltip("Super zone rules — no bomb, premium rewards (gold wheel). Appears every superZoneInterval zones.")]
        public ZoneRules superZoneRules;

        [Header("Zone Intervals")]
        [Tooltip("A safe zone appears every N zones (default: 5).")]
        public int safeZoneInterval = 5;

        [Tooltip("A super zone appears every N zones (default: 30).")]
        public int superZoneInterval = 30;

        // ------------------------------------------------------------------ Public API

        /// <summary>Returns the ZoneTier for the given 1-based zone index.</summary>
        public Core.ZoneTier GetTierForZone(int zoneIndex)
        {
            if (zoneIndex > 0 && zoneIndex % superZoneInterval == 0) return Core.ZoneTier.Super;
            if (zoneIndex > 0 && zoneIndex % safeZoneInterval  == 0) return Core.ZoneTier.Safe;
            return Core.ZoneTier.Standard;
        }

        /// <summary>Returns the ZoneRules matching the tier of the given zone index.</summary>
        public ZoneRules GetRulesForZone(int zoneIndex)
        {
            switch (GetTierForZone(zoneIndex))
            {
                case Core.ZoneTier.Safe:  return safeZoneRules;
                case Core.ZoneTier.Super: return superZoneRules;
                default:                  return standardZoneRules;
            }
        }

        // ------------------------------------------------------------------ Editor Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnIfEmpty(safeZoneRules,  "safeZoneRules");
            WarnIfEmpty(superZoneRules, "superZoneRules");

            if (bombReward == null)
                Debug.LogWarning($"[WheelConfigData] '{name}': bombReward is not assigned!", this);
        }

        private void WarnIfEmpty(ZoneRules rules, string label)
        {
            if (rules == null) return;
            if (rules.possibleRewards == null || rules.possibleRewards.Count == 0)
                Debug.LogWarning($"[WheelConfigData] '{name}': {label}.possibleRewards is empty — " +
                                 "safe/super zones will throw exceptions at runtime!", this);
        }
#endif
    }
}
