using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameDemo.Data;

namespace MiniGameDemo.Core
{
    /// <summary>
    /// Central game state machine and single source of truth for the session.
    /// Responsibilities: state transitions, reward tracking, zone progression.
    /// Does NOT handle scene loading or UI — those listen to its events.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ------------------------------------------------------------------ Singleton
        public static GameManager Instance { get; private set; }

        // ------------------------------------------------------------------ Inspector
        [SerializeField] private WheelConfigData _wheelConfig;

        // ------------------------------------------------------------------ Public State

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public int CurrentZone { get; private set; } = 1;
        public ZoneTier CurrentZoneTier => _wheelConfig != null
            ? _wheelConfig.GetTierForZone(CurrentZone)
            : ZoneTier.Standard;

        /// <summary>
        /// True only when the wheel is idle AND the current zone is Safe or Super.
        /// This is the only condition under which the Leave button should be interactive.
        /// </summary>
        public bool CanLeave => CurrentState == GameState.Playing &&
                                (CurrentZoneTier == ZoneTier.Safe || CurrentZoneTier == ZoneTier.Super);

        public IReadOnlyDictionary<RewardItemData, int> CollectedRewards => _collectedRewards;
        private readonly Dictionary<RewardItemData, int> _collectedRewards = new Dictionary<RewardItemData, int>();

        // ------------------------------------------------------------------ Events

        public event Action<GameState> OnStateChanged;
        public event Action<int>       OnZoneChanged;
        public event Action<RewardItemData, int> OnRewardCollected;
        public event Action OnRewardsCleared;

        // ------------------------------------------------------------------ Constants

        public const int ENTRY_FEE        = 50;
        public const int SKIP_ENTRY_FEE   = 500;
        public const int SKIP_ZONES_COUNT = 20;

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            // Single-scene singleton — no DontDestroyOnLoad needed
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            PlayerProfile.ResetForDemo();
            SetState(GameState.MainMenu);
        }

        // ------------------------------------------------------------------ Public API

        /// <summary>
        /// Deducts entry fee and begins a new game session.
        /// <param name="skipZones">If true, starts at zone 21 (costs 500).</param>
        /// </summary>
        public void TryStartGame(bool skipZones)
        {
            int fee = skipZones ? SKIP_ENTRY_FEE : ENTRY_FEE;
            if (!PlayerProfile.SpendCurrency(fee))
            {
                Debug.LogWarning("[GameManager] Not enough currency to start.");
                return;
            }

            CurrentZone = skipZones ? SKIP_ZONES_COUNT + 1 : 1;
            ClearRewards();
            SetState(GameState.Playing);
            OnZoneChanged?.Invoke(CurrentZone);
        }

        /// <summary>
        /// Called by WheelController after the spin animation ends.
        /// Bomb → clears rewards and triggers GameOver.
        /// Reward → adds to collection and advances to the next zone.
        /// </summary>
        public void ProcessSpinResult(RewardItemData result, int amount)
        {
            if (result == null) return;

            if (result.rewardType == RewardType.Bomb)
            {
                ClearRewards();
                SetState(GameState.GameOver);
                return;
            }

            if (_collectedRewards.ContainsKey(result))
                _collectedRewards[result] += amount;
            else
                _collectedRewards.Add(result, amount);

            OnRewardCollected?.Invoke(result, _collectedRewards[result]);

            CurrentZone++;
            SetState(GameState.Playing);
            OnZoneChanged?.Invoke(CurrentZone);
        }

        /// <summary>
        /// Grants collected currency rewards to the player profile and returns to main menu.
        /// Only callable when CanLeave is true — enforced here in addition to UI layer.
        /// </summary>
        public void LeaveGame()
        {
            if (!CanLeave)
            {
                Debug.LogWarning("[GameManager] LeaveGame called when CanLeave is false.");
                return;
            }

            foreach (var kvp in _collectedRewards)
            {
                if (kvp.Key.rewardType == RewardType.Currency || kvp.Key.rewardType == RewardType.Gold)
                    PlayerProfile.AddCurrency(kvp.Value);
            }

            ClearRewards();
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// Spends revive currency, restores Playing state and re-triggers wheel generation.
        /// </summary>
        public void TryRevive(int reviveCost)
        {
            if (!PlayerProfile.SpendCurrency(reviveCost))
            {
                Debug.LogWarning("[GameManager] Not enough currency to revive.");
                return;
            }
            // Re-trigger OnZoneChanged so WheelController generates a new wheel for the same zone
            SetState(GameState.Playing);
            OnZoneChanged?.Invoke(CurrentZone);
        }

        /// <summary>Abandons the run and returns to the main menu without granting rewards.</summary>
        public void GiveUp()
        {
            ClearRewards();
            SetState(GameState.MainMenu);
        }

        /// <summary>Transitions to a new state and fires the OnStateChanged event.</summary>
        public void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public WheelConfigData GetConfig() => _wheelConfig;

        // ------------------------------------------------------------------ Private

        private void ClearRewards()
        {
            _collectedRewards.Clear();
            OnRewardsCleared?.Invoke();
        }
    }
}
