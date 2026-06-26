using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.UI
{
 
    [RequireComponent(typeof(RectTransform))]
    public class InventoryUIController : MonoBehaviour
    {

        [SerializeField] private GameObject _inventoryItemPrefab;
        [SerializeField] private float _itemSize = 48f;
        [SerializeField] private float _itemSpacing = 6f;

        private readonly Dictionary<RewardItemData, InventoryItemUI> _activeItems
            = new Dictionary<RewardItemData, InventoryItemUI>();

        private void Awake()
        {
            SetupGridLayout();
        }

        private void OnEnable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnRewardCollected += HandleRewardCollected;
            GameManager.Instance.OnRewardsCleared  += HandleRewardsCleared;
        }

        private void OnDisable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnRewardCollected -= HandleRewardCollected;
            GameManager.Instance.OnRewardsCleared  -= HandleRewardsCleared;
        }

        private void SetupGridLayout()
        {
            // Remove any conflicting layout group types first
            var hLayout = GetComponent<HorizontalLayoutGroup>();
            if (hLayout != null) DestroyImmediate(hLayout);
            var vLayout = GetComponent<VerticalLayoutGroup>();
            if (vLayout != null) DestroyImmediate(vLayout);

            // Now safe to get-or-add GridLayoutGroup
            var layout = GetComponent<GridLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<GridLayoutGroup>();

            // These values come from the Inspector fields on this component
            layout.startAxis       = GridLayoutGroup.Axis.Vertical;
            layout.constraint      = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = 11;
            layout.childAlignment  = TextAnchor.UpperLeft;
            layout.cellSize        = new Vector2(_itemSize, _itemSize);
            layout.spacing         = new Vector2(_itemSpacing, _itemSpacing);
            layout.padding         = new RectOffset(4, 4, 8, 8);
        }


        // Events
        private void HandleRewardCollected(RewardItemData reward, int totalAmount)
        {
            // If we already have this reward in the inventory, just update the counter
            if (_activeItems.TryGetValue(reward, out var existing))
            {
                existing.UpdateAmount(totalAmount);
                return;
            }

            if (_inventoryItemPrefab == null)
            {
                Debug.LogWarning("[InventoryUIController] _inventoryItemPrefab is not assigned.");
                return;
            }

            var obj  = Instantiate(_inventoryItemPrefab, transform);
            var item = obj.GetComponent<InventoryItemUI>();
            if (item == null)
                item = obj.AddComponent<InventoryItemUI>();

            item.Setup(reward, totalAmount, _itemSize);
            _activeItems[reward] = item;
        }

        private void HandleRewardsCleared()
        {
            foreach (Transform child in transform)
            {
                DOTween.Kill(child);
                Destroy(child.gameObject);
            }
            _activeItems.Clear();
        }
    }

    
    public class InventoryItemUI : MonoBehaviour
    {
        private Image           _iconImage;
        private TextMeshProUGUI _amountText;
        private RectTransform   _rt;

        public void Setup(RewardItemData reward, int amount, float size)
        {
            _rt = GetComponent<RectTransform>();
            _rt.sizeDelta = new Vector2(size, size);

            // Clear the ROOT Image background (white -> transparent)
            var rootImg = GetComponent<Image>();
            if (rootImg != null) rootImg.color = Color.clear;

            // Find the EXISTING icon Image child by name
            // The prefab already has a child called "img_reward_icon_value" with an Image.
            // We use it instead of creating a duplicate.
            _iconImage = FindChildImage("img_reward_icon_value");

            if (_iconImage != null)
            {
                // Clear the icon child's default white background
                // (Unity Images with no sprite show solid white by default)
                if (reward.icon != null)
                {
                    _iconImage.sprite         = reward.icon;
                    _iconImage.preserveAspect = true;
                    _iconImage.color          = Color.white;  // tint = white so sprite colours show correctly
                }
                else
                {
                    // No icon sprite — hide this Image entirely
                    _iconImage.color = Color.clear;
                }

                _iconImage.raycastTarget = false;

                // Make icon fill its parent fully
                var iconRt = _iconImage.GetComponent<RectTransform>();
                if (iconRt != null)
                {
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.offsetMin = Vector2.zero;
                    iconRt.offsetMax = Vector2.zero;
                }
            }

            // Find the EXISTING TMP child by name 
            // The prefab already has a child called "txt_reward_amount_value" with TMP.
            // We reuse it for the counter instead of creating a duplicate.
            _amountText = FindChildTMP("txt_reward_amount_value");

            if (_amountText != null)
            {
                // Configure the counter badge: small, bold, bottom-right corner
                _amountText.fontSize           = Mathf.Clamp(size * 0.28f, 10f, 18f);
                _amountText.color              = Color.white;
                _amountText.fontStyle           = FontStyles.Bold;
                _amountText.alignment           = TextAlignmentOptions.BottomRight;
                _amountText.raycastTarget       = false;
                _amountText.enableWordWrapping  = false;

                // Dark outline so the count reads clearly on any icon
                _amountText.outlineWidth = 0.3f;
                _amountText.outlineColor = new Color32(0, 0, 0, 200);

                // Position badge in bottom-right corner of the icon
                var textRt = _amountText.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin        = new Vector2(0f, 0f);
                    textRt.anchorMax        = new Vector2(1f, 0.4f);
                    textRt.offsetMin        = Vector2.zero;
                    textRt.offsetMax        = Vector2.zero;
                }
            }

            UpdateAmount(amount);
        }


        /// Updates the "x2", "x3" etc. counter text. Called when the same reward is collected again.
        public void UpdateAmount(int amount)
        {
            if (_amountText != null)
            {
                _amountText.text = $"x{amount}";
                
                // Add a small scale bounce for polish when updating
                if (amount > 1 && _rt != null)
                {
                    _rt.DOKill();
                    _rt.localScale = Vector3.one;
                    _rt.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f);
                }
            }
        }

        // Find helpers — searches existing prefab children by name
        private Image FindChildImage(string childName)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == childName)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) return img;
                }
            }
            // Fallback: any child Image
            return GetComponentInChildren<Image>();
        }

        private TextMeshProUGUI FindChildTMP(string childName)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == childName)
                {
                    var tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) return tmp;
                }
            }
            // Fallback: any child TMP
            return GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
