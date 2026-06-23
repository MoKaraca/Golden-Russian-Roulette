using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Core;
using MiniGameDemo.Wheel;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Controls visibility and interactability of all gameplay UI panels.
    ///
    /// Rules:
    ///  - Buttons are wired in Awake() — NO Unity Editor onClick references (per spec).
    ///  - OnValidate() auto-finds references by GameObject name in children (per spec).
    ///  - Leave button is only interactable when GameManager.CanLeave is true.
    ///  - Revive triggers wheel re-generation for the current zone.
    ///
    /// Attach to: Canvas_Gameplay
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Header("Panels")]
        [SerializeField] private GameObject _panel_gameplay;
        [SerializeField] private GameObject _panel_gameover;

        [Header("Gameplay Buttons")]
        [SerializeField] private Button _btn_spin;
        [SerializeField] private Button _btn_leave;

        [Header("Zone Display")]
        [SerializeField] private TextMeshProUGUI _txt_zone_value;

        [Header("Game Over")]
        [SerializeField] private Button          _btn_revive;
        [SerializeField] private Button          _btn_givup;
        [SerializeField] private TextMeshProUGUI _txt_revive_cost_value;

        [Header("References")]
        [SerializeField] private WheelController _wheelController;

        private const int REVIVE_COST = 25;

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            WireButtonListeners();
            ClearPanelBackgrounds();
        }

        /// <summary>
        /// Unity Panels have a default semi-transparent white Image that creates a gray wash.
        /// We clear it here so panels are invisible containers — only their children show.
        /// </summary>
        private void ClearPanelBackgrounds()
        {
            ClearImageBackground(_panel_gameplay);
            ClearImageBackground(_panel_gameover);
        }

        private static void ClearImageBackground(GameObject go)
        {
            if (go == null) return;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = Color.clear;
        }

        private void Start()
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnZoneChanged  += HandleZoneChanged;

            // Sync UI to current state on start
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnZoneChanged  -= HandleZoneChanged;
        }

        // ------------------------------------------------------------------ OnValidate — auto reference wiring

#if UNITY_EDITOR
        /// <summary>
        /// Automatically finds serialized fields by child GameObject name in the Editor.
        /// This satisfies the spec requirement: "Button references should be automatically
        /// set from OnValidate Editor codes".
        /// </summary>
        private void OnValidate()
        {
            FindChildButton(ref _btn_spin,   "btn_spin");
            FindChildButton(ref _btn_leave,  "btn_leave");
            FindChildButton(ref _btn_revive, "btn_revive");
            FindChildButton(ref _btn_givup,  "btn_GiveUp");

            FindChildTMP(ref _txt_zone_value,        "txt_zone_value");
            // NOTE: looks for 'txt_revive_cost_value' (with _value suffix per spec).
            // The existing static 'revive_cost' label in the scene will NOT be touched.
            // To show the cost dynamically, add a TMP text named 'txt_revive_cost_value' inside ui_panel_gameover.
            FindChildTMP(ref _txt_revive_cost_value, "txt_revive_cost_value");

            FindChildGameObject(ref _panel_gameplay,  "ui_panel_gameplay");
            FindChildGameObject(ref _panel_gameover,  "ui_panel_gameover");

            FindChildComponent(ref _wheelController, "ui_panel_wheel_root");
        }

        private void FindChildButton(ref Button field, string goName)
        {
            if (field != null) return;
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.gameObject.name == goName) { field = b; break; }
        }

        private void FindChildTMP(ref TextMeshProUGUI field, string goName)
        {
            if (field != null) return;
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.gameObject.name == goName) { field = t; break; }
        }

        private void FindChildGameObject(ref GameObject field, string goName)
        {
            if (field != null) return;
            foreach (var tr in GetComponentsInChildren<Transform>(true))
                if (tr.gameObject.name == goName) { field = tr.gameObject; break; }
        }

        private void FindChildComponent<T>(ref T field, string goName) where T : Component
        {
            if (field != null) return;
            foreach (var c in GetComponentsInChildren<T>(true))
                if (c.gameObject.name == goName) { field = c; break; }
        }
#endif

        // ------------------------------------------------------------------ Button wiring (no Editor onClick)

        private void WireButtonListeners()
        {
            if (_btn_spin  != null) _btn_spin.onClick.AddListener(OnSpinClicked);
            if (_btn_leave != null) _btn_leave.onClick.AddListener(OnLeaveClicked);
            if (_btn_revive!= null) _btn_revive.onClick.AddListener(OnReviveClicked);
            if (_btn_givup != null) _btn_givup.onClick.AddListener(OnGiveUpClicked);
        }

        // ------------------------------------------------------------------ State event handler

        private void HandleStateChanged(GameState state)
        {
            bool showGameplay = state == GameState.Playing || state == GameState.Spinning || state == GameState.GameOver;
            bool showGameOver = state == GameState.GameOver;

            SetActive(_panel_gameplay, showGameplay);
            SetActive(_panel_gameover, showGameOver);

            // Spin: only when wheel is idle (Playing)
            if (_btn_spin  != null) _btn_spin.interactable = state == GameState.Playing;

            // Leave: only when CanLeave (Playing + Safe/Super zone)
            RefreshLeaveButton();

            // Game over panel
            if (showGameOver)
            {
                if (_txt_revive_cost_value != null)
                    _txt_revive_cost_value.text = REVIVE_COST.ToString();

                if (_btn_revive != null)
                    _btn_revive.interactable = PlayerProfile.HasEnoughCurrency(REVIVE_COST);
            }
        }

        // ------------------------------------------------------------------ Zone event handler

        private void HandleZoneChanged(int newZone)
        {
            if (_txt_zone_value != null)
                _txt_zone_value.text = $"ZONE {newZone}";

            // Tell the wheel to generate slices for the new zone
            if (_wheelController != null)
                _wheelController.GenerateWheelForZone(newZone);

            // Zone tier may have changed — refresh leave button state
            RefreshLeaveButton();
        }

        // ------------------------------------------------------------------ Button callbacks

        private void OnSpinClicked()
        {
            if (_wheelController != null) _wheelController.Spin();
        }

        private void OnLeaveClicked()  => GameManager.Instance.LeaveGame();
        private void OnReviveClicked() => GameManager.Instance.TryRevive(REVIVE_COST);
        private void OnGiveUpClicked() => GameManager.Instance.GiveUp();

        // ------------------------------------------------------------------ Helpers

        private void RefreshLeaveButton()
        {
            if (_btn_leave != null)
                _btn_leave.interactable = GameManager.Instance.CanLeave;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
