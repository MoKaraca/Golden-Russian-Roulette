using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Shows collected rewards as a compact column on the LEFT edge of the screen.
    ///
    /// RectTransform anchor setup (Issue 4A):
    ///   anchorMin = (0, 0)  anchorMax = (0, 1)  pivot = (0, 1)
    ///   This "left-stretch" anchor snaps the panel to the left side on all aspect ratios
    ///   (20:9, 16:9, 4:3) without manual repositioning.
    ///
    /// Each item uses the existing InventoryItemPrefab which already has:
    ///   - Child "img_reward_icon_value" (Image)  → used for the reward icon
    ///   - Child "txt_reward_amount_value" (TMP)  → used for the "x2" counter
    ///
    /// The code finds these children by name, clears all white backgrounds,
    /// sets the icon sprite, and writes the counter text.
    ///
    /// Attach to: ui_panel_inventory (child of Canvas_Gameplay).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Tooltip("Prefab with child 'img_reward_icon_value' (Image) and " +
                 "'txt_reward_amount_value' (TMP). Both are already in the prefab.")]
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

            // ── Issue 4A: Left sidebar — full height, anchored to LEFT edge ─────
            // anchorMin = (0,0) anchorMax = (0,1) pivot = (0,1)
            // Works on all aspect ratios (20:9, 16:9, 4:3) without hard-coded positions.
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            float panelWidth = _itemSize + 20f;
            rt.offsetMin = new Vector2(0f,          80f);  // left edge = screen left
            rt.offsetMax = new Vector2(panelWidth, -70f);  // 70px clearance from top

            // Container is fully transparent
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.clear;

            // ── Fix: remove any conflicting LayoutGroup type before adding VerticalLayoutGroup ──
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
                Destroy(child.gameObject);
            _activeItems.Clear();
        }
    }

    // ============================================================
    // Inner helper — one inventory slot
    // ============================================================

    /// <summary>
    /// Controls a single reward entry in the inventory sidebar.
    ///
    /// This component is added at runtime to each instantiated InventoryItemPrefab.
    /// It finds the EXISTING children in the prefab:
    ///   - "img_reward_icon_value" → Image for the reward icon
    ///   - "txt_reward_amount_value" → TMP for the "x2" counter
    ///
    /// It does NOT create new children — it uses what's already in the prefab,
    /// clears all white backgrounds, and properly populates icon + counter.
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

            // ── Clear the ROOT Image background (white → transparent) ─────────
            var rootImg = GetComponent<Image>();
            if (rootImg != null) rootImg.color = Color.clear;

            // ── Find the EXISTING icon Image child by name ────────────────────
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

            // ── Find the EXISTING TMP child by name ───────────────────────────
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

        /// <summary>
        /// Updates the "x2", "x3" etc. counter text. Called when the same reward is collected again.
        /// </summary>
        public void UpdateAmount(int amount)
        {
            if (_amountText != null)
                _amountText.text = $"x{amount}";
        }

        // ── Find helpers — searches existing prefab children by name ──────

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
