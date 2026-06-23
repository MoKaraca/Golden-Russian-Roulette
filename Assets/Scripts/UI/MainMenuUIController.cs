using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGameDemo.Core;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Controls the Main Menu panel: shows/hides based on game state,
    /// displays current currency and wires the two start buttons.
    ///
    /// Attach to: The Canvas (or any persistent root containing the main menu panel).
    /// </summary>
    public class MainMenuUIController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector

        [Header("Panel")]
        [SerializeField] private GameObject _panel_main_menu;

        [Header("Buttons")]
        [Tooltip("Start a game for 50 currency (btn_Play50).")]
        [SerializeField] private Button _btn_play_standard;

        [Tooltip("Start a game for 500 currency (btn_Play500) — skips to zone 21.")]
        [SerializeField] private Button _btn_play_skip;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI _txt_currency_value;

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            // Wire button listeners in code — no Editor onClick references (per spec)
            if (_btn_play_standard != null)
                _btn_play_standard.onClick.AddListener(() => GameManager.Instance.TryStartGame(false));
            if (_btn_play_skip != null)
                _btn_play_skip.onClick.AddListener(() => GameManager.Instance.TryStartGame(true));
        }

        private void Start()
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            PlayerProfile.OnCurrencyChanged     += UpdateCurrencyDisplay;

            // Initial sync
            UpdateCurrencyDisplay(PlayerProfile.Currency);
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            PlayerProfile.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }

        // ------------------------------------------------------------------ OnValidate — auto reference wiring

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindChildButton(ref _btn_play_standard, "btn_Play50");
            FindChildButton(ref _btn_play_skip,     "btn_Play500");
            FindChildTMP(ref _txt_currency_value,   "Currency_text");
            FindChildGameObject(ref _panel_main_menu, "Canvas");
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
#endif

        // ------------------------------------------------------------------ Event handlers

        private void HandleStateChanged(GameState state)
        {
            if (_panel_main_menu != null)
                _panel_main_menu.SetActive(state == GameState.MainMenu);
        }

        private void UpdateCurrencyDisplay(int amount)
        {
            if (_txt_currency_value != null)
                _txt_currency_value.text = amount.ToString();
        }
    }
}
