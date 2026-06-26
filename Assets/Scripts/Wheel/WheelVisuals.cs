using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.Wheel
{
    /// Handles all visual aspects of the wheel layout, sprite swapping, and DOTween animations.
    public class WheelVisuals : MonoBehaviour
    {
        [SerializeField] private RectTransform _rt_slices_root;
        [SerializeField] private GameObject _sliceVisualPrefab;
        [SerializeField] private float _sliceIconRadius = 160f;
        [SerializeField] private float _sliceIconSize = 80f;
        [SerializeField] private float _spinDuration = 3f;
        [SerializeField] private int _minFullSpins = 5;
        [SerializeField] private Image _img_wheel_base;
        [SerializeField] private Image _img_indicator;
        [SerializeField] private Sprite _sprite_wheel_standard;
        [SerializeField] private Sprite _sprite_wheel_safe;
        [SerializeField] private Sprite _sprite_wheel_super;
        [SerializeField] private Sprite _sprite_indicator_standard;
        [SerializeField] private Sprite _sprite_indicator_safe;
        [SerializeField] private Sprite _sprite_indicator_super;

        private readonly List<WheelSliceVisual> _sliceVisuals = new List<WheelSliceVisual>();
        private bool _isSpinning;

        /// Updates the wheel base and indicator sprites based on the current zone tier.
        public void UpdateWheelSprites(ZoneTier tier)
        {
            Sprite baseSprite = tier == ZoneTier.Super ? _sprite_wheel_super  :
                                tier == ZoneTier.Safe  ? _sprite_wheel_safe   :
                                                         _sprite_wheel_standard;

            if (_img_wheel_base != null && baseSprite != null)
                _img_wheel_base.sprite = baseSprite;

            Sprite indicatorSprite = tier == ZoneTier.Super ? _sprite_indicator_super  :
                                     tier == ZoneTier.Safe  ? _sprite_indicator_safe   :
                                                              _sprite_indicator_standard;

            if (_img_indicator != null && indicatorSprite != null)
                _img_indicator.sprite = indicatorSprite;
        }

        /// Builds the slice visuals in a circular layout and handles pop-in animations.
        public void RebuildSliceVisuals(List<WheelSliceData> currentSlices, bool tierChanged)
        {
            if (_sliceVisualPrefab == null || _rt_slices_root == null)
            {
                Debug.LogWarning("[WheelVisuals] sliceVisualPrefab or slicesRoot is not assigned.");
                return;
            }

            int count = currentSlices.Count;
            
            // Ensure pool has enough visuals
            while (_sliceVisuals.Count < count)
            {
                var obj = Instantiate(_sliceVisualPrefab, _rt_slices_root);
                _sliceVisuals.Add(obj.GetComponent<WheelSliceVisual>());
            }

            float sliceAngleDeg = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angleDeg = 90f - i * sliceAngleDeg;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                var visual = _sliceVisuals[i];
                visual.gameObject.SetActive(true);
                var obj = visual.gameObject;
                var rt  = obj.GetComponent<RectTransform>();

                rt.sizeDelta = new Vector2(_sliceIconSize, _sliceIconSize);
                rt.anchoredPosition = new Vector2(
                    Mathf.Cos(angleRad) * _sliceIconRadius,
                    Mathf.Sin(angleRad) * _sliceIconRadius
                );
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);

                var rootImg = obj.GetComponent<Image>();
                if (rootImg != null) rootImg.color = Color.clear;

                if (visual != null)
                    visual.Setup(currentSlices[i].reward, currentSlices[i].amount);
            }

            for (int i = count; i < _sliceVisuals.Count; i++)
                _sliceVisuals[i].gameObject.SetActive(false);

            _rt_slices_root.DOKill();
            
            if (tierChanged)
            {
                _rt_slices_root.localScale = Vector3.zero;
                _rt_slices_root.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetLink(gameObject);
            }
            else
            {
                _rt_slices_root.localScale = Vector3.one;
                
                for (int i = 0; i < count; i++)
                {
                    if (_sliceVisuals[i] == null) continue;
                    var rt = _sliceVisuals[i].GetComponent<RectTransform>();
                    rt.localScale = Vector3.zero;
                    rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.02f).SetLink(_sliceVisuals[i].gameObject);
                }
            }
        }

        /// Executes the clockwise spin animation and fires the callback upon completion.>
        public void SpinClockwise(int targetIndex, int totalSlices, System.Action<int> onComplete)
        {
            if (_isSpinning) return;
            _isSpinning = true;

            float sliceAngleDeg = 360f / totalSlices;
            float currentZ      = _rt_slices_root.eulerAngles.z;
            float normZ         = ((currentZ % 360f) + 360f) % 360f;
            float targetLocalAngle = ((targetIndex * sliceAngleDeg) % 360f + 360f) % 360f;
            
            float cwDelta = ((normZ - targetLocalAngle) % 360f + 360f) % 360f;
            if (cwDelta < 1f) cwDelta += 360f; 

            cwDelta += 360f * _minFullSpins;
            float finalZ = currentZ - cwDelta; 

            _rt_slices_root.DOKill();
            _rt_slices_root
                .DORotate(new Vector3(0f, 0f, finalZ), _spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    _isSpinning = false;
                    onComplete?.Invoke(targetIndex);
                });
        }

        /// Plays the highlight animation for a standard reward.
        public void PlayRewardAnimation(int winIndex, System.Action onComplete)
        {
            if (winIndex < 0 || winIndex >= _sliceVisuals.Count)
            {
                onComplete?.Invoke();
                return;
            }

            var visual = _sliceVisuals[winIndex];
            if (visual == null)
            {
                onComplete?.Invoke();
                return;
            }

            var rt  = visual.GetComponent<RectTransform>();
            var img = visual.GetComponentInChildren<Image>();
            Color originalColor = img != null ? img.color : Color.white;

            Sequence seq = DOTween.Sequence();
            seq.SetLink(visual.gameObject);
            seq.Append(rt.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));

            if (img != null)
            {
                seq.Insert(0f,    img.DOColor(new Color(1f, 0.9f, 0.3f), 0.15f));
                seq.Insert(0.35f, img.DOColor(originalColor, 0.15f));
            }

            seq.AppendInterval(0.15f);
            seq.Append(rt.DOScale(1f, 0.15f).SetEase(Ease.InBack));
            seq.OnComplete(() => onComplete?.Invoke());
        }

        /// Plays the red shake animation for a bomb hit.
        public void PlayBombAnimation(int bombIndex, System.Action onComplete)
        {
            if (bombIndex < 0 || bombIndex >= _sliceVisuals.Count)
            {
                onComplete?.Invoke();
                return;
            }

            var visual = _sliceVisuals[bombIndex];
            if (visual == null)
            {
                onComplete?.Invoke();
                return;
            }

            var rt  = visual.GetComponent<RectTransform>();
            var img = visual.GetComponentInChildren<Image>();
            Color originalColor = img != null ? img.color : Color.white;

            Sequence seq = DOTween.Sequence();
            seq.SetLink(visual.gameObject);

            seq.Append(rt.DOScale(1.6f, 0.1f).SetEase(Ease.OutBounce));
            seq.Append(rt.DOScale(1f, 0.1f));
            seq.Append(rt.DOShakeAnchorPos(0.4f, strength: 12f, vibrato: 20, randomness: 90f));

            if (img != null)
            {
                seq.Insert(0f,   img.DOColor(Color.red, 0.1f));
                seq.Insert(0.1f, img.DOColor(originalColor, 0.1f));
                seq.Insert(0.2f, img.DOColor(Color.red, 0.1f));
                seq.Insert(0.3f, img.DOColor(originalColor, 0.1f));
                seq.Insert(0.4f, img.DOColor(Color.red, 0.1f));
                seq.Insert(0.5f, img.DOColor(originalColor, 0.1f));
            }

            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
}
