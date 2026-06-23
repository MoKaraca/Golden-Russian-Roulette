using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniGameDemo.Core;
using MiniGameDemo.Data;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Manages the inventory panel showing rewards collected in the current run.
    /// Self-positions at the bottom of its parent canvas.
    /// Items have transparent backgrounds — only the reward icon and amount text show.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Tooltip("Prefab with a WheelSliceUI component (icon Image + amount TMP).")]
        [SerializeField] private GameObject _inventoryItemPrefab;

        // ------------------------------------------------------------------ State

        private readonly Dictionary<RewardItemData, WheelSliceUI> _activeItems
            = new Dictionary<RewardItemData, WheelSliceUI>();

        private HorizontalLayoutGroup _layoutGroup;

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            SetupPanelLayout();
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

        // ------------------------------------------------------------------ Layout

        private void SetupPanelLayout()
        {
            var rt = GetComponent<RectTransform>();

            // Anchor to bottom-centre, stretch horizontally, height 90px
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(10f, 10f);   // left, bottom
            rt.offsetMax = new Vector2(-10f, 100f);  // right, top

            // No background — the panel should be invisible
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.clear;

            // Horizontal layout for items
            _layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (_layoutGroup == null)
                _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

            _layoutGroup.childAlignment         = TextAnchor.MiddleCenter;
            _layoutGroup.spacing                = 8f;
            _layoutGroup.childForceExpandWidth  = false;
            _layoutGroup.childForceExpandHeight = false;
            _layoutGroup.childControlWidth      = false;
            _layoutGroup.childControlHeight     = false;
            _layoutGroup.padding                = new RectOffset(4, 4, 4, 4);
        }

        // ------------------------------------------------------------------ Events

        private void HandleRewardCollected(RewardItemData reward, int totalAmount)
        {
            if (_activeItems.TryGetValue(reward, out var existing))
            {
                existing.Setup(reward, totalAmount);
                return;
            }

            if (_inventoryItemPrefab == null)
            {
                Debug.LogWarning("[InventoryUIController] _inventoryItemPrefab is not assigned.");
                return;
            }

            var obj = Instantiate(_inventoryItemPrefab, transform);

            // Clear white background on spawned item root
            var rootImg = obj.GetComponent<Image>();
            if (rootImg != null) rootImg.color = Color.clear;

            // Fix item size to a consistent square
            var itemRt = obj.GetComponent<RectTransform>();
            if (itemRt != null) itemRt.sizeDelta = new Vector2(70f, 80f);

            var ui = obj.GetComponent<WheelSliceUI>();
            if (ui == null)
            {
                Debug.LogWarning("[InventoryUIController] inventoryItemPrefab is missing WheelSliceUI.");
                return;
            }

            ui.Setup(reward, totalAmount);
            _activeItems[reward] = ui;
        }

        private void HandleRewardsCleared()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _activeItems.Clear();
        }
    }
}
