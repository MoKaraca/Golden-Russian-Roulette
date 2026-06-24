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

        // ------------------------------------------------------------------ Multipliers (Issue 6)

        /// <summary>Reward multiplier applied in Silver (Safe) zones — every 5th zone.</summary>
        public const float SILVER_ZONE_MULTIPLIER = 3f;

        /// <summary>Reward multiplier applied in Gold (Super) zones — every 30th zone.</summary>
        public const float GOLD_ZONE_MULTIPLIER = 10f;

        /// <summary>
        /// Returns the reward multiplier appropriate for the given zone index.
        /// Super → 10x | Safe → 3x | Standard → 1x.
        /// </summary>
        public float GetMultiplierForZone(int zoneIndex)
        {
            var tier = _wheelConfig != null ? _wheelConfig.GetTierForZone(zoneIndex) : ZoneTier.Standard;
            return tier == ZoneTier.Super ? GOLD_ZONE_MULTIPLIER
                 : tier == ZoneTier.Safe  ? SILVER_ZONE_MULTIPLIER
                 : 1f;
        }

        /// <summary>
        /// True whenever the wheel is idle (state == Playing), regardless of zone tier.
        /// Per requirements: player can choose to cash out before ANY spin.
        /// </summary>
        public bool CanLeave => CurrentState == GameState.Playing;

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
        /// Bomb → triggers GameOver but does NOT clear rewards yet.
        ///   - If the player revives, they keep their collected rewards.
        ///   - If the player gives up, GiveUp() clears the rewards.
        /// Reward → adds to collection and advances to the next zone.
        /// </summary>
        public void ProcessSpinResult(RewardItemData result, int amount)
        {
            if (result == null) return;

            if (result.rewardType == RewardType.Bomb)
            {
                // Do NOT clear rewards here — player may revive and keep them.
                // Rewards are cleared in GiveUp() if the player chooses not to revive.
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
        /// Grants collected rewards (with zone multiplier applied) to the player profile
        /// and returns to main menu. Callable whenever CanLeave is true (Playing state).
        /// </summary>
        public void LeaveGame()
        {
            if (!CanLeave)
            {
                Debug.LogWarning("[GameManager] LeaveGame called when CanLeave is false.");
                return;
            }

            float multiplier = GetMultiplierForZone(CurrentZone);

            foreach (var kvp in _collectedRewards)
            {
                if (kvp.Key.rewardType == RewardType.Currency || kvp.Key.rewardType == RewardType.Gold)
                    PlayerProfile.AddCurrency(Mathf.RoundToInt(kvp.Value * multiplier));
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
