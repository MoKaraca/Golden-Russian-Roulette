using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.Wheel
{
    /// <summary>
    /// Manages:
    ///   1. Generating wheel slices for the current zone (rewards + optional bomb).
    ///      Issue 3 — selects DISTINCT items from the possibleRewards list in WheelConfigData;
    ///      exactly 1 bomb on Standard zones, 0 on Safe/Super.
    ///   2. Instantiating and positioning WheelSliceVisual prefabs on the wheel image.
    ///   3. Animating the spin via DOTween (Issue 5 — clockwise direction).
    ///   4. DOTween post-spin reward highlight / bomb shake animations.
    ///
    /// Attach to: ui_panel_wheel_root.
    /// </summary>
    public class WheelController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Header("Wheel Transforms")]
        [Tooltip("The RectTransform that is the parent for slice visual prefabs (ui_image_wheel_base_value).")]
        [SerializeField] private RectTransform _rt_slices_root;

        [Header("Slice Visuals")]
        [Tooltip("Prefab containing WheelSliceVisual (icon Image + amount TMP).")]
        [SerializeField] private GameObject _sliceVisualPrefab;

        [Tooltip("Distance from the wheel centre at which each slice icon is placed (pixels).\nIncrease to push icons outward, decrease to pull them inward.")]
        [SerializeField] private float _sliceIconRadius = 160f;

        [Tooltip("Width and height of each slice icon square (pixels).\n" +
                 "Make this smaller if icons look too large, larger if they look too small.\n" +
                 "Rule of thumb: keep it at ~50% of the wheel's diameter.")]
        [SerializeField] private float _sliceIconSize = 80f;

        [Header("Spin Settings")]
        [Tooltip("Total duration of one spin animation in seconds.")]
        [SerializeField] private float _spinDuration = 3f;

        [Tooltip("Minimum number of full 360° rotations before the wheel settles.")]
        [SerializeField] private int _minFullSpins = 5;

        [Header("Wheel Tier Visuals")]
        [Tooltip("The Image component on ui_image_wheel_base_value. Sprite is swapped per zone tier.")]
        [SerializeField] private Image _img_wheel_base;

        [Tooltip("The static indicator/needle Image (NOT a child of ui_animator_wheel so it doesn't rotate).\n" +
                 "Assign the 'indicator' object from the scene hierarchy.")]
        [SerializeField] private Image _img_indicator;

        [Header("Wheel Base Sprites")]
        [Tooltip("Bronze wheel base (Standard zones). Assign ui_spin_bronze_base sprite.")]
        [SerializeField] private Sprite _sprite_wheel_standard;
        [Tooltip("Silver wheel base (Safe zones, every 5th). Assign ui_spin_silver_base sprite.")]
        [SerializeField] private Sprite _sprite_wheel_safe;
        [Tooltip("Golden wheel base (Super zones, every 30th). Assign ui_spin_golden_base sprite.")]
        [SerializeField] private Sprite _sprite_wheel_super;

        [Header("Indicator / Needle Sprites")]
        [Tooltip("Bronze indicator arrow. Assign ui_spin_bronze_indicator sprite.")]
        [SerializeField] private Sprite _sprite_indicator_standard;
        [Tooltip("Silver indicator arrow. Assign ui_spin_silver_indicator sprite.")]
        [SerializeField] private Sprite _sprite_indicator_safe;
        [Tooltip("Gold indicator arrow. Assign ui_spin_golden_indicator sprite.")]
        [SerializeField] private Sprite _sprite_indicator_super;

        // ------------------------------------------------------------------ State

        private readonly List<WheelSliceData>   _currentSlices  = new List<WheelSliceData>();
        private readonly List<WheelSliceVisual> _sliceVisuals   = new List<WheelSliceVisual>();
        private bool _isSpinning;
        private ZoneTier? _lastTier = null;

        // ------------------------------------------------------------------ Events

        /// <summary>Fired after the wheel is generated. Passes the slice list for other systems.</summary>
        public event System.Action<List<WheelSliceData>> OnWheelGenerated;

        // ------------------------------------------------------------------ Public API

        /// <summary>
        /// Builds the slice list for the given zone and rebuilds the visual wheel.
        /// Called by GameUIManager when OnZoneChanged fires.
        ///
        /// Issue 3 rules:
        ///   • Standard zone  → (sliceCount - 1) DISTINCT reward items + exactly 1 bomb.
        ///   • Safe / Super   → sliceCount DISTINCT reward items, zero bombs.
        ///
        /// Uses the possibleRewards list from WheelConfigData (already has 35 items).
        /// </summary>
        public void GenerateWheelForZone(int zoneIndex)
        {
            var config = GameManager.Instance.GetConfig();
            var rules  = config.GetRulesForZone(zoneIndex);
            var tier   = config.GetTierForZone(zoneIndex);

            bool tierChanged = _lastTier == null || _lastTier.Value != tier;
            _lastTier = tier;

            _currentSlices.Clear();

            bool addBomb      = tier == ZoneTier.Standard;
            int   rewardCount  = addBomb ? rules.sliceCount - 1 : rules.sliceCount;
            float multiplier   = GameManager.Instance.GetMultiplierForZone(zoneIndex);

            // ── Build the reward pool (exclude any bomb-type items) ───────────
            List<RewardItemData> rewardPool = BuildRewardPool(rules);

            if (rewardPool == null || rewardPool.Count == 0)
            {
                Debug.LogError($"[WheelController] Zone {zoneIndex} rules have no possible rewards! " +
                               "Check WheelConfigData in the inspector.");
                return;
            }

            // ── Select DISTINCT rewards via weighted sampling ────────
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

            // ── Issue 3: Exactly ONE bomb on Standard zones, ZERO otherwise ──
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

            RebuildSliceVisuals(tierChanged);
            UpdateWheelVisuals(tier);
            OnWheelGenerated?.Invoke(_currentSlices);
        }

        /// <summary>
        /// Picks a winner slice using weighted probability then starts the clockwise DOTween spin.
        /// No-ops if already spinning or slices haven't been generated.
        /// </summary>
        public void Spin()
        {
            if (_isSpinning || _currentSlices.Count == 0) return;

            int targetIndex = GetWeightedRandomSliceIndex();
            _isSpinning = true;
            GameManager.Instance.SetState(GameState.Spinning);
            SpinClockwise(targetIndex);
        }

        // ------------------------------------------------------------------ Wheel Population Helpers (Issue 3)

        /// <summary>
        /// Returns the reward pool to draw from, filtering out any Bomb-type items.
        /// Bombs are injected separately — never drawn from the pool.
        /// </summary>
        private static List<RewardItemData> BuildRewardPool(ZoneRules rules)
        {
            if (rules.possibleRewards == null || rules.possibleRewards.Count == 0)
                return null;

            return rules.possibleRewards
                .Where(r => r != null && r.rewardType != RewardType.Bomb)
                .ToList();
        }

        /// <summary>
        /// Selects <paramref name="count"/> distinct items from <paramref name="pool"/>
        /// using weighted-random sampling WITHOUT replacement (Issue 3).
        /// If pool has fewer items than count, it refills to allow reuse.
        /// </summary>
        private static List<RewardItemData> SelectDistinctRewards(List<RewardItemData> pool, int count)
        {
            var result    = new List<RewardItemData>(count);
            var remaining = new List<RewardItemData>(pool);

            for (int i = 0; i < count; i++)
            {
                if (remaining.Count == 0)
                    remaining = new List<RewardItemData>(pool); // refill if exhausted

                var chosen = GetWeightedRandom(remaining);
                result.Add(chosen);
                remaining.Remove(chosen); // enforce distinctness
            }
            return result;
        }

        // ------------------------------------------------------------------ Slice Visuals

        private void RebuildSliceVisuals(bool tierChanged)
        {
            // Destroy existing slice visuals
            foreach (var v in _sliceVisuals)
                if (v != null) Destroy(v.gameObject);
            _sliceVisuals.Clear();

            if (_sliceVisualPrefab == null || _rt_slices_root == null)
            {
                Debug.LogWarning("[WheelController] sliceVisualPrefab or slicesRoot is not assigned.");
                return;
            }

            int   count        = _currentSlices.Count;
            float sliceAngleDeg = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // Slice 0 starts at the top (90°), each step clockwise subtracts sliceAngleDeg.
                float angleDeg = 90f - i * sliceAngleDeg;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                var obj = Instantiate(_sliceVisualPrefab, _rt_slices_root);
                var rt  = obj.GetComponent<RectTransform>();

                rt.sizeDelta = new Vector2(_sliceIconSize, _sliceIconSize);
                rt.anchoredPosition = new Vector2(
                    Mathf.Cos(angleRad) * _sliceIconRadius,
                    Mathf.Sin(angleRad) * _sliceIconRadius
                );
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);

                // CRITICAL: Remove white background — the root Image should be invisible.
                var rootImg = obj.GetComponent<Image>();
                if (rootImg != null) rootImg.color = Color.clear;

                var visual = obj.GetComponent<WheelSliceVisual>();
                if (visual != null)
                    visual.Setup(_currentSlices[i].reward, _currentSlices[i].amount);

                _sliceVisuals.Add(visual);
            }

            // Polish: Pop-in animation
            _rt_slices_root.DOKill();
            
            if (tierChanged)
            {
                // Wheel tier changed (e.g. Bronze -> Silver) -> Pop the entire wheel in
                _rt_slices_root.localScale = Vector3.zero;
                _rt_slices_root.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
            }
            else
            {
                // Staying in the same tier -> Keep wheel static, pop in the individual slice visuals
                _rt_slices_root.localScale = Vector3.one;
                
                // Add a small delay between each slice popping in for a nice visual cascade
                for (int i = 0; i < _sliceVisuals.Count; i++)
                {
                    if (_sliceVisuals[i] == null) continue;
                    var rt = _sliceVisuals[i].GetComponent<RectTransform>();
                    rt.localScale = Vector3.zero;
                    rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.02f);
                }
            }
        }

        // ------------------------------------------------------------------ DOTween Clockwise Spin (Issue 5)

        /// <summary>
        /// DOTween-based clockwise spin.
        ///
        /// Unity's Z-rotation increases COUNTER-CLOCKWISE.
        /// Clockwise = DECREASING Z.  We subtract the delta from the current angle.
        ///
        /// Slice[i] sits at local angle (90° - i * sliceAngle).
        /// After CW rotation by Δ (euler Z decreases), the world angle of slice[i] is:
        ///   currentZ - Δ + (90 - i * sliceAngle)
        /// We want slice[target] at indicator (top = 90°):
        ///   currentZ - Δ + 90 - target * sliceAngle = 90  (mod 360)
        ///   Δ = currentZ - target * sliceAngle  (mod 360)  +  full spins
        /// </summary>
        private void SpinClockwise(int targetIndex)
        {
            float sliceAngleDeg = 360f / _currentSlices.Count;
            float currentZ      = _rt_slices_root.eulerAngles.z;

            // Normalise into [0, 360)
            float normZ = ((currentZ % 360f) + 360f) % 360f;

            float targetLocalAngle = ((targetIndex * sliceAngleDeg) % 360f + 360f) % 360f;

            // CW delta = how far clockwise from current to target alignment
            float cwDelta = ((normZ - targetLocalAngle) % 360f + 360f) % 360f;
            if (cwDelta < 1f) cwDelta += 360f; // avoid near-zero spin

            // Add minimum full clockwise rotations
            cwDelta += 360f * _minFullSpins;

            float finalZ = currentZ - cwDelta; // CW = decreasing Z

            // Kill any existing tween on the wheel
            _rt_slices_root.DOKill();

            _rt_slices_root
                .DORotate(new Vector3(0f, 0f, finalZ), _spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    _isSpinning = false;
                    OnSpinComplete(targetIndex);
                });
        }

        private void OnSpinComplete(int targetIndex)
        {
            var result = _currentSlices[targetIndex];

            // DOTween feedback animation, then report to GameManager
            if (result.reward != null && result.reward.rewardType == RewardType.Bomb)
                PlayBombAnimation(targetIndex, result);
            else
                PlayRewardAnimation(targetIndex, result);
        }

        // ------------------------------------------------------------------ DOTween Post-Spin Animations (Issue 3 Bonus)

        /// <summary>
        /// Scale-up + golden colour flash on the winning reward slice, then report result.
        /// </summary>
        private void PlayRewardAnimation(int winIndex, WheelSliceData result)
        {
            if (winIndex < 0 || winIndex >= _sliceVisuals.Count)
            {
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
                return;
            }

            var visual = _sliceVisuals[winIndex];
            if (visual == null)
            {
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
                return;
            }

            var rt  = visual.GetComponent<RectTransform>();
            var img = visual.GetComponentInChildren<Image>();
            Color originalColor = img != null ? img.color : Color.white;

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));

            if (img != null)
            {
                seq.Insert(0f,    img.DOColor(new Color(1f, 0.9f, 0.3f), 0.15f));
                seq.Insert(0.35f, img.DOColor(originalColor, 0.15f));
            }

            seq.AppendInterval(0.15f);
            seq.Append(rt.DOScale(1f, 0.15f).SetEase(Ease.InBack));
            seq.OnComplete(() =>
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount));
        }

        /// <summary>
        /// Shake + red flash on the bomb slice, then report result.
        /// </summary>
        private void PlayBombAnimation(int bombIndex, WheelSliceData result)
        {
            if (bombIndex < 0 || bombIndex >= _sliceVisuals.Count)
            {
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
                return;
            }

            var visual = _sliceVisuals[bombIndex];
            if (visual == null)
            {
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
                return;
            }

            var rt  = visual.GetComponent<RectTransform>();
            var img = visual.GetComponentInChildren<Image>();
            Color originalColor = img != null ? img.color : Color.white;

            Sequence seq = DOTween.Sequence();

            // Scale pulse
            seq.Append(rt.DOScale(1.6f, 0.1f).SetEase(Ease.OutBounce));
            seq.Append(rt.DOScale(1f, 0.1f));

            // Shake
            seq.Append(rt.DOShakeAnchorPos(0.4f, strength: 12f, vibrato: 20, randomness: 90f));

            // Red flash overlay
            if (img != null)
            {
                seq.Insert(0f,   img.DOColor(Color.red, 0.1f));
                seq.Insert(0.1f, img.DOColor(originalColor, 0.1f));
                seq.Insert(0.2f, img.DOColor(Color.red, 0.1f));
                seq.Insert(0.3f, img.DOColor(originalColor, 0.1f));
                seq.Insert(0.4f, img.DOColor(Color.red, 0.1f));
                seq.Insert(0.5f, img.DOColor(originalColor, 0.1f));
            }

            seq.OnComplete(() =>
                GameManager.Instance.ProcessSpinResult(result.reward, result.amount));
        }

        // ------------------------------------------------------------------ Wheel Visual Update

        private void UpdateWheelVisuals(ZoneTier tier)
        {
            Sprite baseSprite = tier == ZoneTier.Super ? _sprite_wheel_super  :
                                tier == ZoneTier.Safe  ? _sprite_wheel_safe   :
                                                         _sprite_wheel_standard;

            if (_img_wheel_base != null && baseSprite != null)
                _img_wheel_base.sprite = baseSprite;
            else if (_img_wheel_base != null && baseSprite == null)
                Debug.LogWarning($"[WheelController] No base sprite assigned for tier {tier}. " +
                                 "Assign sprites in the Wheel Tier Visuals section of the Inspector.");

            Sprite indicatorSprite = tier == ZoneTier.Super ? _sprite_indicator_super  :
                                     tier == ZoneTier.Safe  ? _sprite_indicator_safe   :
                                                              _sprite_indicator_standard;

            if (_img_indicator != null && indicatorSprite != null)
                _img_indicator.sprite = indicatorSprite;
            else if (_img_indicator != null && indicatorSprite == null)
                Debug.LogWarning($"[WheelController] No indicator sprite assigned for tier {tier}. " +
                                 "Assign sprites in the Indicator section of the Inspector.");
        }

        // ------------------------------------------------------------------ Weighted Random

        private static RewardItemData GetWeightedRandom(List<RewardItemData> pool)
        {
            float total = pool.Sum(r => r.baseWeight);
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
