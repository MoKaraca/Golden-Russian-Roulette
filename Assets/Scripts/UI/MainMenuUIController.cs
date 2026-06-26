using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MiniGameDemo.Core;

namespace MiniGameDemo.UI
{
  
    public class MainMenuUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _panel_main_menu;
        [SerializeField] private Button _btn_play_standard;
        [SerializeField] private Button _btn_play_skip;
        [SerializeField] private TextMeshProUGUI _txt_currency_value;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (_panel_main_menu != null)
            {
                _canvasGroup = _panel_main_menu.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = _panel_main_menu.AddComponent<CanvasGroup>();
            }

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


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_panel_main_menu != null && _btn_play_standard != null && _btn_play_skip != null && _txt_currency_value != null) return;

            _panel_main_menu = FindChildByName("ui_panel_main_menu");

            FindChildButton(ref _btn_play_standard, "ui_btn_play_standard");
            FindChildButton(ref _btn_play_skip, "ui_btn_play_super");

            FindChildTMP(ref _txt_currency_value, "ui_txt_currency_value");
        }

        private GameObject FindChildByName(string goName)
        {
            foreach (var tr in GetComponentsInChildren<Transform>(true))
                if (tr.gameObject.name == goName) return tr.gameObject;
            return null;
        }

        private void FindChildButton(ref Button field, string goName)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.gameObject.name == goName) { field = b; return; }
            field = null;
        }

        private void FindChildTMP(ref TextMeshProUGUI field, string goName)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.gameObject.name == goName) { field = t; return; }
            field = null;
        }

        private void FindChildGameObject(ref GameObject field, string goName)
        {
            foreach (var tr in GetComponentsInChildren<Transform>(true))
                if (tr.gameObject.name == goName) { field = tr.gameObject; return; }
            field = null;
        }
#endif


        private void HandleStateChanged(GameState state)
        {
            if (_panel_main_menu == null || _canvasGroup == null) return;

            bool show = state == GameState.MainMenu;

            if (show) _panel_main_menu.SetActive(true);

            _canvasGroup.blocksRaycasts = show;
            _canvasGroup.interactable = show;

            _canvasGroup.DOFade(show ? 1f : 0f, 0.3f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (!show) _panel_main_menu.SetActive(false);
                });
        }

        private void UpdateCurrencyDisplay(int amount)
        {
            if (_txt_currency_value != null)
                _txt_currency_value.text = amount.ToString();
        }
    }
}
