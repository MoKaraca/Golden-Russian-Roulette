using UnityEngine;
using System;

namespace MiniGameDemo.Core
{

    public static class PlayerProfile
    {
        private const string CURRENCY_KEY = "Player_Currency";
        public static event Action<int> OnCurrencyChanged;
        private static int _cachedCurrency = -1;

        public static int Currency
        {
            get
            {
                if (_cachedCurrency < 0) _cachedCurrency = PlayerPrefs.GetInt(CURRENCY_KEY, 1000);
                return _cachedCurrency;
            }
            set
            {
                _cachedCurrency = value;
                OnCurrencyChanged?.Invoke(value);
            }
        }

        public static void ResetCurrency()
        { Currency = 1000;}


        public static void PersistToDisk()
        {
            if (_cachedCurrency >= 0)
            {
                PlayerPrefs.SetInt(CURRENCY_KEY, _cachedCurrency);
                PlayerPrefs.Save();
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
