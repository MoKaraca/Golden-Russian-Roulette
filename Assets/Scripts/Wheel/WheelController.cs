using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.Wheel
{
    /// <summary>
    /// Manages:
    ///   1. Generating wheel slices for the current zone (rewards + optional bomb).
    ///   2. Instantiating and positioning WheelSliceVisual prefabs on the wheel image.
    ///   3. Animating the spin coroutine and reporting the result to GameManager.
    ///
    /// Attach to: the GameObject containing ui_animator_wheel.
    /// </summary>
    public class WheelController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Header("Wheel Transforms")]
        [Tooltip("The RectTransform that physically rotates during the spin (ui_animator_wheel).")]
        [SerializeField] private RectTransform _rt_wheel_spin;

        [Tooltip("The RectTransform that is the parent for slice visual prefabs (ui_image_wheel_base_value).")]
        [SerializeField] private RectTransform _rt_slices_root;

        [Header("Slice Visuals")]
        [Tooltip("Prefab containing WheelSliceVisual (icon Image + amount TMP).")]
        [SerializeField] private GameObject _sliceVisualPrefab;

        [Tooltip("Distance from the wheel centre at which each slice icon is placed (pixels).")]
        [SerializeField] private float _sliceIconRadius = 160f;

        [Header("Spin Settings")]
        [Tooltip("Total duration of one spin animation in seconds.")]
        [SerializeField] private float _spinDuration = 3f;

        [Tooltip("Minimum number of full 360° rotations before the wheel settles.")]
        [SerializeField] private int _minFullSpins = 5;

        // ------------------------------------------------------------------ State

        private readonly List<WheelSliceData>   _currentSlices  = new List<WheelSliceData>();
        private readonly List<WheelSliceVisual> _sliceVisuals   = new List<WheelSliceVisual>();
        private bool _isSpinning;

        // ------------------------------------------------------------------ Events

        /// <summary>Fired after the wheel is generated. Passes the slice list for other systems.</summary>
        public event System.Action<List<WheelSliceData>> OnWheelGenerated;

        // ------------------------------------------------------------------ Public API

        /// <summary>
        /// Builds the slice list for the given zone and rebuilds the visual wheel.
        /// Called by GameUIManager when OnZoneChanged fires.
        /// </summary>
        public void GenerateWheelForZone(int zoneIndex)
        {
            var config = GameManager.Instance.GetConfig();
            var rules  = config.GetRulesForZone(zoneIndex);
            var tier   = config.GetTierForZone(zoneIndex);

            _currentSlices.Clear();

            bool addBomb      = tier == ZoneTier.Standard;
            int  rewardCount  = addBomb ? rules.sliceCount - 1 : rules.sliceCount;

            if (rules.possibleRewards == null || rules.possibleRewards.Count == 0)
            {
                Debug.LogError($"[WheelController] Zone {zoneIndex} rules have no possible rewards! " +
                               "Check WheelConfigData in the inspector.");
                return;
            }

            for (int i = 0; i < rewardCount; i++)
            {
                var reward = GetWeightedRandom(rules.possibleRewards);
                _currentSlices.Add(new WheelSliceData
                {
                    reward = reward,
                    amount = reward.GetAmountForZone(zoneIndex),
                    weight = reward.baseWeight
                });
            }

            if (addBomb)
            {
                // Place bomb at a random position so the player can't predict its slot
                int insertAt = Random.Range(0, _currentSlices.Count + 1);
                _currentSlices.Insert(insertAt, new WheelSliceData
                {
                    reward = config.bombReward,
                    amount = 0,
                    weight = 1f  // Equal weight to one reward slice
                });
            }

            RebuildSliceVisuals();
            OnWheelGenerated?.Invoke(_currentSlices);
        }

        /// <summary>
        /// Picks a winner slice using weighted probability then starts the spin animation.
        /// No-ops if already spinning or slices haven't been generated.
        /// </summary>
        public void Spin()
        {
            if (_isSpinning || _currentSlices.Count == 0) return;

            int targetIndex = GetWeightedRandomSliceIndex();
            _isSpinning = true;
            GameManager.Instance.SetState(GameState.Spinning);
            StartCoroutine(SpinCoroutine(targetIndex));
        }

        // ------------------------------------------------------------------ Slice Visuals

        private void RebuildSliceVisuals()
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
                // ── Position ──────────────────────────────────────────────────
                // Slice 0 starts at the top (90°), subsequent slices go clockwise.
                // In Unity's 2D space: right = 0°, up = 90°, so top = 90°.
                // Each step clockwise subtracts sliceAngleDeg.
                float angleDeg = 90f - i * sliceAngleDeg;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                var obj = Instantiate(_sliceVisualPrefab, _rt_slices_root);
                var rt  = obj.GetComponent<RectTransform>();

                // Fixed size for each slice icon cell
                rt.sizeDelta = new Vector2(_sliceIconRadius * 0.9f, _sliceIconRadius * 0.9f);

                // Place icon at the slice's midpoint radius from centre
                rt.anchoredPosition = new Vector2(
                    Mathf.Cos(angleRad) * _sliceIconRadius,
                    Mathf.Sin(angleRad) * _sliceIconRadius
                );

                // Rotate the visual so text/icon reads radially outward
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);

                // CRITICAL: Remove white background — the root Image should be invisible.
                // Only the icon child Image (img_icon_value) should display the sprite.
                var rootImg = obj.GetComponent<UnityEngine.UI.Image>();
                if (rootImg != null) rootImg.color = Color.clear;

                var visual = obj.GetComponent<WheelSliceVisual>();
                if (visual != null)
                    visual.Setup(_currentSlices[i].reward, _currentSlices[i].amount);

                _sliceVisuals.Add(visual);
            }
        }

        // ------------------------------------------------------------------ Spin Coroutine

        private IEnumerator SpinCoroutine(int targetIndex)
        {
            // ── Angle calculation ───────────────────────────────────────────
            // Slice[i] is placed at (90° - i * sliceAngleDeg) in the wheel's local space.
            // When the wheel has Z-rotation R (positive = CCW in Unity), slice[i] sits at:
            //   world angle = R + (90 - i * sliceAngle)
            // We want slice[targetIndex] directly under the indicator (at top = 90°):
            //   R + 90 - targetIndex * sliceAngle = 90
            //   R = targetIndex * sliceAngle
            float sliceAngleDeg  = 360f / _currentSlices.Count;
            float startAngleZ    = _rt_wheel_spin.eulerAngles.z;
            float targetLocalAngle = targetIndex * sliceAngleDeg;

            // Normalise start angle into [0, 360)
            float normalisedStart  = startAngleZ % 360f;
            float normalisedTarget = targetLocalAngle % 360f;

            // Calculate shortest forward (CCW = positive) delta
            float delta = normalisedTarget - normalisedStart;
            if (delta < 0f) delta += 360f;
            if (delta < 1f) delta += 360f;  // Avoid near-zero spin if already aligned

            // Guarantee at least _minFullSpins full rotations
            delta += 360f * _minFullSpins;

            float absoluteTarget = startAngleZ + delta;

            // ── Animation loop ──────────────────────────────────────────────
            float elapsed = 0f;
            while (elapsed < _spinDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / _spinDuration);
                float eased = EaseOutCubic(t);
                _rt_wheel_spin.eulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngleZ, absoluteTarget, eased));
                yield return null;
            }

            // Snap to exact final angle
            _rt_wheel_spin.eulerAngles = new Vector3(0f, 0f, absoluteTarget);
            _isSpinning = false;
            OnSpinComplete(targetIndex);
        }

        private void OnSpinComplete(int targetIndex)
        {
            var result = _currentSlices[targetIndex];
            GameManager.Instance.ProcessSpinResult(result.reward, result.amount);
        }

        // ------------------------------------------------------------------ Weighted Random

        /// <summary>Selects a reward from the list using baseWeight as probability.</summary>
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

        /// <summary>
        /// Picks a winning slice index using the slice weight list.
        /// Bomb gets a weight equal to 1 (one reward slot equivalent).
        /// </summary>
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

        // ------------------------------------------------------------------ Easing

        /// <summary>Cubic ease-out: fast start, slow finish — perfect for a wheel deceleration.</summary>
        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
