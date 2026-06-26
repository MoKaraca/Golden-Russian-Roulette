using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameDemo.Data;

namespace MiniGameDemo.Core
{
            public class GameManager : MonoBehaviour
        {
                public static GameManager Instance { get; private set; }
                [SerializeField] private WheelConfigData _wheelConfig;
                public WheelConfigData GetConfig() => _wheelConfig;
                //set the current state to main menu by default
                public GameState CurrentState { get; private set;} = GameState.MainMenu;      
                // set the current zone to 1 by default
                public int CurrentZone { get; private set;} = 1;  


                // get the current zone tier based on the current zone and the wheel config
                public ZoneTier CurrentZoneTier => _wheelConfig != null ? _wheelConfig.GetTierForZone(CurrentZone) : ZoneTier.Standard;

                // public constants for the multipliers and fees
                public const float SAFE_ZONE_MULTIPLIER = 3f;
                public const float SUPER_ZONE_MULTIPLIER = 10f;
                public const float ENTRY_FEE = 50;
                public const float SKIP_20_ZONES_FEE = 500;
                public const int SKIP_ZONES_COUNT = 20;

                public float GetMultiplierForZone(int zoneIndex)
                {
                        var tier = _wheelConfig != null ? _wheelConfig.GetTierForZone(zoneIndex) : ZoneTier.Standard;
                        return tier == ZoneTier.Super ? SUPER_ZONE_MULTIPLIER
                                : tier == ZoneTier.Safe  ? SAFE_ZONE_MULTIPLIER
                                : 1f;
                }
                // True whenever the wheel is idle (state == Playing), regardless of zone tier.
                // Per requirements: player can choose to cash out before ANY spin.
                public bool CanLeave => CurrentState == GameState.Playing;

                // collected rewards dictionary declarion and Ireadonly dictionary property
                private readonly Dictionary<RewardItemData, int> _collectedRewards = new Dictionary<RewardItemData, int>();
                public IReadOnlyDictionary<RewardItemData, int> CollectedRewards => _collectedRewards;


                // events for state change, reward collection, zone change and rewards clear
                public event Action<GameState> OnStateChanged;
                public event Action<RewardItemData, int> OnRewardCollected;
                public event Action<int> OnZoneChanged;
                public event Action OnRewardsCleared;

                


                private void ClearRewards()
                {
                _collectedRewards.Clear();
                OnRewardsCleared?.Invoke();
                }

                public void SetState(GameState newState)
                {
                CurrentState = newState;
                OnStateChanged?.Invoke(newState);
                }


                private void Awake()
                {
                if (Instance == null)
                        Instance = this;
                else
                        Destroy(gameObject);
                }

                private void Start()
                {
                        PlayerProfile.ResetCurrency();
                        SetState(GameState.MainMenu);      
                }  

                private void OnApplicationPause(bool pauseStatus)
                {
                        if (pauseStatus) PlayerProfile.PersistToDisk();
                }

                private void OnApplicationQuit()
                {
                        PlayerProfile.PersistToDisk();
                }

                // check if the player has enough currency to start the game in chosen zone
                // if yes, deduct the entry fee and set the state to playing and publish zone change event
                public void TryStartGame(bool skipZone)
                {
                        int fee = skipZone ? (int)SKIP_20_ZONES_FEE : (int)ENTRY_FEE;
                        if (!PlayerProfile.SpendCurrency(fee))
                        {
                                Debug.LogWarning("Not enough currency to start the game.");
                                return;
                        }

                        CurrentZone = skipZone ? SKIP_ZONES_COUNT + 1 : 1;
                        ClearRewards();
                        OnZoneChanged?.Invoke(CurrentZone);
                        SetState(GameState.Playing);
                }
                
                public void ProcessSpinResult(RewardItemData reward, int amount)
                {
                      if(reward == null)
                      {
                        Debug.LogWarning("Reward is null. Cannot process spin.");
                        return;
                      }
                      
                      // Safety net: Guarantee the bomb is never added to the inventory
                      if (reward.rewardType == RewardType.Bomb)
                      {
                          SetState(GameState.GameOver);
                          return;
                      }

                        if (_collectedRewards.ContainsKey(reward))
                        {
                                _collectedRewards[reward] += amount;
                        }
                        else
                        {
                                _collectedRewards.Add(reward, amount);
                        }
                        OnRewardCollected?.Invoke(reward, amount);

                        CurrentZone++;
                        OnZoneChanged?.Invoke(CurrentZone);
                        SetState(GameState.Playing);
                        
                }

                public void LeaveGame()
                {
                        if(!CanLeave)
                        {
                                Debug.LogWarning("Cannot leave the game at this time.");
                                return;
                        }
                        float multiplier = GetMultiplierForZone(CurrentZone);

                        foreach(var i in _collectedRewards)
                        {
                            if(i.Key.rewardType == RewardType.Currency || i.Key.rewardType == RewardType.Gold)
                            {
                                int totalAmount = Mathf.RoundToInt(i.Value * multiplier);
                                PlayerProfile.AddCurrency(totalAmount);
                            }
                           
                        }
                        ClearRewards();
                        SetState(GameState.MainMenu);
                }

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

                public void GiveUp()
                {
                ClearRewards();
                SetState(GameState.MainMenu);
                }

        }  
}
