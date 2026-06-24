using UnityEngine;
using TMPro;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using MiniGameDemo.Core;

namespace MiniGameDemo.UI
{
    /// <summary>
    /// Issue 1 — GameOver Panel Override Fix.
    ///
    /// PROBLEM DIAGNOSIS:
    /// ─────────────────
    /// If editor changes to ui_panel_gameover are not reflected at runtime, the most common causes are:
    ///
    ///   A) A stale prefab override:
    ///      The panel is a Prefab Instance. The Inspector shows the prefab's saved values,
    ///      not the overridden scene values, unless you explicitly "Apply Overrides" or "Unpack Prefab".
    ///      FIX: Right-click ui_panel_gameover in Hierarchy → Prefab → Unpack Completely.
    ///
    ///   B) A script is re-instantiating the panel at runtime via Resources.Load / Addressables:
    ///      Search the entire project for "ui_panel_gameover" or "GameOver" with Ctrl+Shift+F.
    ///      If found, update the instantiation call to use the serialized reference below.
    ///
    ///   C) An additive scene loaded at runtime containing a duplicate Canvas:
    ///      Open Window → Scene Manager (or check SceneManager.LoadSceneAsync calls with LoadSceneMode.Additive).
    ///      The additive scene's Canvas renders on top and hides your edits.
    ///      FIX: Remove the duplicate Canvas from the additive scene, or set its Sort Order lower.
    ///
    ///   D) Duplicate Canvas objects in the same scene:
    ///      Check the Hierarchy for more than one Canvas at the root level.
    ///      The higher Sort Order Canvas paints over the lower one.
    ///      FIX: Merge into one Canvas, or ensure Sort Orders are intentional.
    ///
    /// CORRECT SETUP (serialized reference — no Resources.Load):
    /// ──────────────────────────────────────────────────────────
    /// This component replaces any script that was doing:
    ///   Instantiate(Resources.Load("ui_panel_gameover"))
    /// with a clean serialized Inspector reference.
    ///
    /// Attach to: Canvas_Gameplay (or GameManager's GameObject).
    /// Wire _gameOverPanel in the Inspector to the existing ui_panel_gameover child.
    /// Wire _btn_revive and _btn_giveUp the same way.
    /// </summary>
    public class GameOverPanelController : MonoBehaviour
    {
        // ------------------------------------------------------------------ Inspector (serialized refs — no Resources.Load)

        [Header("Panel Reference (Scene Object — NOT a Prefab via Resources)")]
        [Tooltip("Drag the existing ui_panel_gameover from the Hierarchy here. " +
                 "This prevents stale prefab instantiation causing Issue 1.")]
        [SerializeField] private GameObject _gameOverPanel;

        [Header("Buttons (wired via script — no Editor onClick)")]
        [SerializeField] private Button          _btn_revive;
        [SerializeField] private Button          _btn_giveUp;

        [Header("Text Labels")]
        [SerializeField] private TextMeshProUGUI _txt_revive_cost;
        [SerializeField] private TextMeshProUGUI _txt_title;

        private const int REVIVE_COST = 25;

        // ------------------------------------------------------------------ Lifecycle

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

        // ------------------------------------------------------------------ Auto-wire (Editor only)

#if UNITY_EDITOR
        /// <summary>
        /// Automatically finds child references by name — satisfies the
        /// "Button references should be set from OnValidate Editor codes" spec requirement.
        /// </summary>
        private void OnValidate()
        {
            if (_gameOverPanel == null)
                _gameOverPanel = FindChildByName("ui_panel_gameover");

            if (_btn_revive == null) FindChildButton(ref _btn_revive, "btn_revive");
            if (_btn_giveUp == null) FindChildButton(ref _btn_giveUp, "btn_GiveUp");

            if (_txt_revive_cost == null) FindChildTMP(ref _txt_revive_cost, "txt_revive_cost_value");
            if (_txt_title == null)       FindChildTMP(ref _txt_title, "bomb_txt");
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
        }

        private void FindChildTMP(ref TextMeshProUGUI field, string goName)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.gameObject.name == goName) { field = t; return; }
        }
#endif

        // ------------------------------------------------------------------ State Handling

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

        // ------------------------------------------------------------------ Panel Visibility

        /// <summary>
        /// Shows or hides the panel using SetActive. Because this uses the SCENE reference
        /// (not a prefab Instantiate), editor changes are always reflected at runtime.
        /// </summary>
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

        // ------------------------------------------------------------------ Button Callbacks

        private void OnReviveClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TryRevive(REVIVE_COST);
        }

        private void OnGiveUpClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GiveUp();
        }
    }
}
