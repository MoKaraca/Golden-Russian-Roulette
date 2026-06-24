using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniGameDemo.Core;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Displays a horizontal strip of compact zone badges anchored to the top of the screen.
    ///
    /// Layout rules set at runtime (so editor defaults don't matter):
    ///   - Strip is anchored top-centre, height 60px
    ///   - Each badge is 40x50 with spacing 4px
    ///   - Non-current badges have transparent backgrounds
    ///   - Current badge has a coloured highlight
    ///   - Safe zones = silver  |  Super zones = gold  |  Standard = dim grey
    ///
    /// Attach to: any child of Canvas_Gameplay (it repositions itself in Awake)
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ZoneMapUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Tooltip("Prefab with a ZoneItemUI component (Image bg + TMP number).")]
        [SerializeField] private GameObject _zoneItemPrefab;

        [Tooltip("Number of zone badges visible at once.")]
        [SerializeField] private int _visibleZoneCount = 10;

        // ------------------------------------------------------------------ Colours

        private static readonly Color COLOR_STANDARD = new Color(0.40f, 0.40f, 0.45f, 1f);
        private static readonly Color COLOR_SAFE      = new Color(0.75f, 0.80f, 0.90f, 1f);
        private static readonly Color COLOR_SUPER     = new Color(0.98f, 0.80f, 0.15f, 1f);
        private static readonly Color COLOR_CURRENT   = new Color(0.92f, 0.42f, 0.08f, 1f);

        // ------------------------------------------------------------------ State

        private int _currentZone = 1;
        private readonly List<ZoneItemUI> _badges = new List<ZoneItemUI>();
        private HorizontalLayoutGroup _layoutGroup;

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            SetupStripLayout();
        }

        private void Start()
        {
            GameManager.Instance.OnZoneChanged  += OnZoneChanged;
            GameManager.Instance.OnStateChanged += OnStateChanged;
            RebuildStrip();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnZoneChanged  -= OnZoneChanged;
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        // ------------------------------------------------------------------ Layout setup (programmatic)

        /// <summary>
        /// Forces the strip to sit at the TOP of its parent canvas, full-width, height 60px.
        /// This overrides whatever RectTransform values were set in the editor.
        /// </summary>
        private void SetupStripLayout()
        {
            var rt = GetComponent<RectTransform>();

            // Anchor: stretch horizontally across the top, fixed height
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.offsetMin        = new Vector2(182.492f, -42f);  // left, bottom
            rt.offsetMax        = new Vector2(-163.0576f, -38.84299f);  // right, top

            // Ensure we have a HorizontalLayoutGroup
            _layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (_layoutGroup == null)
                _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

            _layoutGroup.childAlignment          = TextAnchor.MiddleCenter;
            _layoutGroup.spacing                 = 4f;
            _layoutGroup.childForceExpandWidth   = false;
            _layoutGroup.childForceExpandHeight  = false;
            _layoutGroup.childControlWidth       = false;
            _layoutGroup.childControlHeight      = false;
            _layoutGroup.padding                 = new RectOffset(8, 8, 4, 4);

            // No background on the strip itself — it should be invisible
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.clear;
        }

        // ------------------------------------------------------------------ Events

        private void OnZoneChanged(int newZone)
        {
            _currentZone = newZone;
            RebuildStrip();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                _currentZone = 1;
                RebuildStrip();
            }
        }

        // ------------------------------------------------------------------ Strip builder

        private void RebuildStrip()
        {
            // Destroy old badges
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _badges.Clear();

            if (_zoneItemPrefab == null) return;

            var config = GameManager.Instance.GetConfig();

            // Show a window: 1 zone before current, then up to _visibleZoneCount ahead
            int windowStart = Mathf.Max(1, _currentZone - 1);
            int windowEnd   = windowStart + _visibleZoneCount - 1;

            for (int z = windowStart; z <= windowEnd; z++)
            {
                var tier      = config.GetTierForZone(z);
                bool isCurrent = z == _currentZone;

                Color color = isCurrent          ? COLOR_CURRENT  :
                              tier == ZoneTier.Super ? COLOR_SUPER   :
                              tier == ZoneTier.Safe  ? COLOR_SAFE    :
                                                       COLOR_STANDARD;

                var obj  = Instantiate(_zoneItemPrefab, transform);
                var item = obj.GetComponent<ZoneItemUI>();
                if (item == null)
                    item = obj.AddComponent<ZoneItemUI>();

                item.Setup(z, color, isCurrent);
                _badges.Add(item);
            }
        }
    }
}
