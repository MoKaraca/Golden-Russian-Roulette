using UnityEngine;
using System;

namespace MiniGameDemo.Core
{
    /// <summary>
    /// Handles persisting player data such as currency.
    /// In a real project, this might use JSON, binary formatting, or a backend service.
    /// </summary>
    public static class PlayerProfile
    {
        private const string CURRENCY_KEY = "Player_Currency";
        
        public static event Action<int> OnCurrencyChanged;

        public static void ResetForDemo()
        {
            Currency = 1000;
        }

        public static int Currency
        {
            get => PlayerPrefs.GetInt(CURRENCY_KEY, 1000);
            set
            {
                PlayerPrefs.SetInt(CURRENCY_KEY, value);
                PlayerPrefs.Save();
                OnCurrencyChanged?.Invoke(value);
            }
        }

        public static bool HasEnoughCurrency(int amount)
        {
            return Currency >= amount;
        }

        public static void AddCurrency(int amount)
        {
            Currency += amount;
        }

        public static bool SpendCurrency(int amount)
        {
            if (HasEnoughCurrency(amount))
            {
                Currency -= amount;
                return true;
            }
            return false;
        }
    }
}
