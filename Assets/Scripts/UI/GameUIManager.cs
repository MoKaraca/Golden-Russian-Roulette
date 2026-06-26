using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MiniGameDemo.Core;
using MiniGameDemo.Wheel;

namespace MiniGameDemo.UI
{
    
    public class GameUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _panel_gameplay;
        [SerializeField] private Button _btn_spin;
        [SerializeField] private Button _btn_leave;
        [SerializeField] private TextMeshProUGUI _txt_zone_value;
        [SerializeField] private WheelController _wheelController;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (_panel_gameplay != null)
            {
                _canvasGroup = _panel_gameplay.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = _panel_gameplay.AddComponent<CanvasGroup>();
            }

            WireButtonListeners();
            ClearPanelBackgrounds();
        }


        /// Unity Panels have a default semi-transparent white Image that creates a gray wash.
        /// We clear it here so panels are invisible containers — only their children show.
        private void ClearPanelBackgrounds()
        {
            ClearImageBackground(_panel_gameplay);
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



#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_btn_spin != null && _btn_leave != null && _txt_zone_value != null && _panel_gameplay != null && _wheelController != null) return;

            FindChildButton(ref _btn_spin, "ui_btn_spin");
            FindChildButton(ref _btn_leave, "ui_btn_leave");

            FindChildTMP(ref _txt_zone_value, "ui_txt_zone_value");

            FindChildGameObject(ref _panel_gameplay, "ui_panel_gameplay");

            FindChildComponent(ref _wheelController, "ui_panel_wheel_root");
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
                if (tr.gameObject.name == goName) { field = tr.gameObject; break; }
        }

        private void FindChildComponent<T>(ref T field, string goName) where T : Component
        {
            if (field != null) return;
            foreach (var c in GetComponentsInChildren<T>(true))
                if (c.gameObject.name == goName) { field = c; break; }
        }
#endif

        private void WireButtonListeners()
        {
            if (_btn_spin  != null) _btn_spin.onClick.AddListener(OnSpinClicked);
            if (_btn_leave != null) _btn_leave.onClick.AddListener(OnLeaveClicked);
        }

        private void HandleStateChanged(GameState state)
        {
            bool showGameplay = state == GameState.Playing || state == GameState.Spinning || state == GameState.GameOver;

            if (_panel_gameplay != null && _canvasGroup != null)
            {
                if (showGameplay) _panel_gameplay.SetActive(true);

                _canvasGroup.blocksRaycasts = showGameplay;
                _canvasGroup.interactable = showGameplay;

                _canvasGroup.DOFade(showGameplay ? 1f : 0f, 0.3f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (!showGameplay) _panel_gameplay.SetActive(false);
                    });
            }

            // Spin: only when wheel is idle (Playing)
            if (_btn_spin != null) _btn_spin.interactable = state == GameState.Playing;

            // Leave: available whenever the wheel is idle (Playing state), regardless of zone tier.
            RefreshLeaveButton();
        }

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


        private void OnSpinClicked()
        {
            if (_wheelController != null) _wheelController.Spin();
        }

        private void OnLeaveClicked() => GameManager.Instance.LeaveGame();


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
