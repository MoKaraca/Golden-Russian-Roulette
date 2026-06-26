using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.Wheel
{
    /// Manages the logical state of the wheel Generating slices for zones, 
    /// calculating weights, picking random rewards, and orchestration.
    /// Defers all UI updates and animations to WheelVisuals.cs.
    public class WheelController : MonoBehaviour
    {
        [Header("Visual Controller")]
        [Tooltip("Reference to the new WheelVisuals component that handles all UI/DOTween logic.")]
        [SerializeField] private WheelVisuals _wheelVisuals;
        private readonly List<WheelSliceData> _currentSlices = new List<WheelSliceData>();
        private bool _isSpinning;
        private ZoneTier? _lastTier = null;
        public event System.Action<List<WheelSliceData>> OnWheelGenerated;
        public void GenerateWheelForZone(int zoneIndex)
        {
            var config = GameManager.Instance.GetConfig();
            var rules  = config.GetRulesForZone(zoneIndex);
            var tier   = config.GetTierForZone(zoneIndex);

            bool tierChanged = _lastTier == null || _lastTier.Value != tier;
            _lastTier = tier;

            _currentSlices.Clear();

            bool addBomb      = tier == ZoneTier.Standard;
            int  rewardCount  = addBomb ? rules.sliceCount - 1 : rules.sliceCount;
            float multiplier  = GameManager.Instance.GetMultiplierForZone(zoneIndex);

            List<RewardItemData> rewardPool = BuildRewardPool(rules);

            if (rewardPool == null || rewardPool.Count == 0)
            {
                Debug.LogError($"[WheelController] Zone {zoneIndex} rules have no possible rewards!");
                return;
            }

            List<RewardItemData> selectedRewards = SelectDistinctRewards(rewardPool, rewardCount);

            foreach (var reward in selectedRewards)
            {
                int baseAmount = reward.GetAmountForZone(zoneIndex);
                _currentSlices.Add(new WheelSliceData
                {
                    reward = reward,
                    amount = Mathf.RoundToInt(baseAmount * multiplier),
                    weight = reward.baseWeight
                });
            }

            if (addBomb)
            {
                int insertAt = Random.Range(0, _currentSlices.Count + 1);
                _currentSlices.Insert(insertAt, new WheelSliceData
                {
                    reward = config.bombReward,
                    amount = 0,
                    weight = 1f
                });
            }

            if (_wheelVisuals != null)
            {
                _wheelVisuals.UpdateWheelSprites(tier);
                _wheelVisuals.RebuildSliceVisuals(_currentSlices, tierChanged);
            }
            else
            {
                Debug.LogWarning("[WheelController] WheelVisuals is not assigned! UI will not update.");
            }

            OnWheelGenerated?.Invoke(_currentSlices);
        }

        public void Spin()
        {
            if (_isSpinning || _currentSlices.Count == 0) return;
            if (_wheelVisuals == null)
            {
                Debug.LogError("[WheelController] Cannot spin because WheelVisuals is missing!");
                return;
            }

            int targetIndex = GetWeightedRandomSliceIndex();
            _isSpinning = true;
            GameManager.Instance.SetState(GameState.Spinning);
            
            _wheelVisuals.SpinClockwise(targetIndex, _currentSlices.Count, OnSpinAnimationComplete);
        }

        private void OnSpinAnimationComplete(int targetIndex)
        {
            var result = _currentSlices[targetIndex];

            if (result.reward != null && result.reward.rewardType == RewardType.Bomb)
            {
                _wheelVisuals.PlayBombAnimation(targetIndex, () => 
                {
                    _isSpinning = false;
                    GameManager.Instance.SetState(GameState.GameOver);
                });
            }
            else
            {
                _wheelVisuals.PlayRewardAnimation(targetIndex, () => 
                {
                    _isSpinning = false;
                    GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
                });
            }
        }

        
        private Dictionary<ZoneRules, List<RewardItemData>> _poolCache = new Dictionary<ZoneRules, List<RewardItemData>>();

        private List<RewardItemData> BuildRewardPool(ZoneRules rules)
        {
            if (rules.possibleRewards == null || rules.possibleRewards.Count == 0)
                return null;

            if (_poolCache.TryGetValue(rules, out var cached)) return cached;

            var pool = rules.possibleRewards
                .Where(r => r != null && r.rewardType != RewardType.Bomb)
                .ToList();
            
            _poolCache[rules] = pool;
            return pool;
        }

        private static List<RewardItemData> SelectDistinctRewards(List<RewardItemData> pool, int count)
        {
            var result    = new List<RewardItemData>(count);
            var remaining = new List<RewardItemData>(pool);

            for (int i = 0; i < count; i++)
            {
                if (remaining.Count == 0)
                    remaining = new List<RewardItemData>(pool); 

                var chosen = GetWeightedRandom(remaining);
                result.Add(chosen);
                remaining.Remove(chosen); 
            }
            return result;
        }

        private static RewardItemData GetWeightedRandom(List<RewardItemData> pool)
        {
            float total = 0f;
            foreach (var r in pool) total += r.baseWeight;
            float roll  = Random.Range(0f, total);
            float accum = 0f;

            foreach (var r in pool)
            {
                accum += r.baseWeight;
                if (roll <= accum) return r;
            }
            return pool[pool.Count - 1];
        }

        private int GetWeightedRandomSliceIndex()
        {
            float total = 0f;
            foreach (var s in _currentSlices)
                total += s.weight > 0f ? s.weight : 1f;

            float roll  = Random.Range(0f, total);
            float accum = 0f;

            for (int i = 0; i < _currentSlices.Count; i++)
            {
                float w = _currentSlices[i].weight > 0f ? _currentSlices[i].weight : 1f;
                accum += w;
                if (roll <= accum) return i;
            }
            return _currentSlices.Count - 1;
        }
    }
}
