using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using MiniGameDemo.Core;

namespace MiniGameDemo.UI
{

    public class GameOverPanelController : MonoBehaviour
    {
    
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _btn_revive;
        [SerializeField] private Button _btn_giveUp;
        [SerializeField] private TextMeshProUGUI _txt_revive_cost;
        [SerializeField] private TextMeshProUGUI _txt_title;

        private const int REVIVE_COST = 25;

        private void Awake()
        {
            // Ensure the panel starts hidden
            SetPanelVisible(false);

            // Wire buttons via code — satisfies "no Editor onClick" constraint
            if (_btn_revive != null) _btn_revive.onClick.AddListener(OnReviveClicked);
            if (_btn_giveUp != null) _btn_giveUp.onClick.AddListener(OnGiveUpClicked);
        }

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameOverPanelController] GameManager.Instance is null. " +
                               "Make sure GameManager is in the scene before this script runs.");
                return;
            }

            GameManager.Instance.OnStateChanged += HandleStateChanged;

            // Sync with current state in case we missed an early transition
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }


#if UNITY_EDITOR
    
        private void OnValidate()
        {
            if (_gameOverPanel != null && _btn_revive != null && _btn_giveUp != null && _txt_revive_cost != null && _txt_title != null) return;

            _gameOverPanel = FindChildByName("ui_panel_gameover");

            FindChildButton(ref _btn_revive, "ui_btn_revive");
            FindChildButton(ref _btn_giveUp, "ui_btn_give_up");

            FindChildTMP(ref _txt_revive_cost, "ui_txt_revive_cost_value");
            FindChildTMP(ref _txt_title, "ui_txt_bomb_title");
        }

        private GameObject FindChildByName(string goName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name == goName) return t.gameObject;
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
#endif

        private void HandleStateChanged(GameState state)
        {
            bool isGameOver = state == GameState.GameOver;
            SetPanelVisible(isGameOver);

            if (!isGameOver) return;

            // Update revive cost label
            if (_txt_revive_cost != null)
                _txt_revive_cost.text = REVIVE_COST.ToString();

            // Disable Revive if player can't afford it
            if (_btn_revive != null)
                _btn_revive.interactable = PlayerProfile.HasEnoughCurrency(REVIVE_COST);
        }

    
        private void SetPanelVisible(bool visible)
        {
            if (_gameOverPanel == null)
            {
                Debug.LogWarning("[GameOverPanelController] _gameOverPanel is not assigned! " +
                                 "Drag ui_panel_gameover from the Hierarchy into this field.");
                return;
            }
            if (visible)
            {
                _gameOverPanel.SetActive(true);
                _gameOverPanel.transform.localScale = Vector3.zero;
                _gameOverPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void OnReviveClicked()
        {
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null)
                GameManager.Instance.TryRevive(REVIVE_COST);
        }

        private void OnGiveUpClicked()
        {
            SoundManager.Instance?.PlayClick();
            if (GameManager.Instance != null)
                GameManager.Instance.GiveUp();
        }
    }
}
