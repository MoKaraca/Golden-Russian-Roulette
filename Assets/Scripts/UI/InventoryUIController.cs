using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Shows collected rewards as a compact column on the RIGHT edge of the screen.
    /// This placement avoids overlapping the wheel (centre) and the spin/leave buttons (bottom centre).
    ///
    /// Each item is a small icon (48x48) with an amount badge — no white backgrounds.
    /// Items stack vertically from the top down; a ScrollRect handles overflow.
    ///
    /// Attach to: any empty child of Canvas_Gameplay — the script positions itself.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Tooltip("Prefab with a WheelSliceUI component. The root Image must exist but " +
                 "its color will be cleared to transparent at runtime.")]
        [SerializeField] private GameObject _inventoryItemPrefab;

        [Tooltip("Size (pixels) of each reward icon square. Default 48 = compact sidebar.")]
        [SerializeField] private float _itemSize = 48f;

        [Tooltip("Vertical gap between items.")]
        [SerializeField] private float _itemSpacing = 6f;

        // ------------------------------------------------------------------ State

        private readonly Dictionary<RewardItemData, InventoryItemUI> _activeItems
            = new Dictionary<RewardItemData, InventoryItemUI>();

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            SetupLayout();
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

        // ------------------------------------------------------------------ Layout (programmatic)

        private void SetupLayout()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("[InventoryUIController] No RectTransform found! Make sure this is on a UI object.");
                return;
            }

            // Right sidebar: full height, anchored to right edge
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            float panelWidth = _itemSize + 20f;
            rt.offsetMin = new Vector2(-panelWidth, 80f);   // 80px clearance from bottom (buttons)
            rt.offsetMax = new Vector2(0f, -70f);            // 70px clearance from top (zone strip)

            // Container is fully transparent
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.clear;

            // ── Fix: remove any conflicting LayoutGroup type before adding VerticalLayoutGroup ──
            // Unity only allows ONE LayoutGroup per GameObject. AddComponent returns null and
            // throws if a different LayoutGroup type already exists.
            var wrongLayouts = GetComponents<HorizontalLayoutGroup>();
            foreach (var l in wrongLayouts) DestroyImmediate(l);
            var gridLayouts = GetComponents<GridLayoutGroup>();
            foreach (var l in gridLayouts) DestroyImmediate(l);

            // Now safe to get-or-add VerticalLayoutGroup
            var layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();

            layout.childAlignment         = TextAnchor.UpperCenter;
            layout.spacing                = _itemSpacing;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = false;
            layout.childControlHeight     = false;
            layout.padding                = new RectOffset(4, 4, 8, 8);
        }


        // ------------------------------------------------------------------ Events

        private void HandleRewardCollected(RewardItemData reward, int totalAmount)
        {
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
                Destroy(child.gameObject);
            _activeItems.Clear();
        }
    }

    // ============================================================
    // Inner helper — one inventory slot
    // ============================================================

    /// <summary>
    /// Controls a single reward entry in the inventory sidebar.
    /// Icon fills a square; amount badge floats in the bottom-right corner.
    /// No white background — container is completely transparent.
    /// </summary>
    public class InventoryItemUI : MonoBehaviour
    {
        private Image           _iconImage;
        private TextMeshProUGUI _amountText;
        private RectTransform   _rt;

        public void Setup(RewardItemData reward, int amount, float size)
        {
            _rt = GetComponent<RectTransform>();
            _rt.sizeDelta = new Vector2(size, size);

            // Root background → transparent
            var rootImg = GetComponent<Image>();
            if (rootImg != null) rootImg.color = Color.clear;

            // Build icon image as a child (fills the whole slot)
            _iconImage = CreateIconImage(size);
            if (reward.icon != null)
            {
                _iconImage.sprite         = reward.icon;
                _iconImage.preserveAspect = true;
            }

            // Build tiny amount badge in bottom-right corner
            _amountText = CreateAmountBadge(size);

            UpdateAmount(amount);
        }

        public void UpdateAmount(int amount)
        {
            if (_amountText != null)
                _amountText.text = $"x{amount}";
        }

        // ── Factory helpers ────────────────────────────────────────

        private Image CreateIconImage(float size)
        {
            var go = new GameObject("img_icon_value", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;   // Per spec: no RaycastTarget on non-interactive images
            img.maskable      = false;   // Per spec: no Maskable on unnecessary images
            return img;
        }

        private TextMeshProUGUI CreateAmountBadge(float size)
        {
            // Small badge pinned to bottom-right of the icon
            float badgeSize = size * 0.45f;

            var go = new GameObject("txt_amount_value", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(badgeSize * 2f, badgeSize);
            rt.anchoredPosition = new Vector2(4f, -2f); // slight overlap with icon edge

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize          = Mathf.Clamp(size * 0.22f, 8f, 14f);
            tmp.color             = Color.white;
            tmp.fontStyle         = FontStyles.Bold;
            tmp.alignment         = TextAlignmentOptions.Right;
            tmp.raycastTarget     = false;
            tmp.enableWordWrapping = false;

            // Dark outline so the count reads on any background
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);

            return tmp;
        }
    }
}
