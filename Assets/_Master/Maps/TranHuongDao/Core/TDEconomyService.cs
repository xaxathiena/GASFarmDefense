using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    public class TDEconomyService : IStartable, ITickable
    {
        public event Action OnDataChanged;

        public int Gold { get; private set; } = 9999;
        public int Seeds { get; private set; } = 50;

        public Dictionary<string, int> Inventory { get; private set; } = new Dictionary<string, int>()
        {
            { "Carrot", 15 },
            { "Pumpkin", 8 },
            { "Grape", 3 }
        };

        public Dictionary<string, int> GrowingCrops { get; private set; } = new Dictionary<string, int>()
        {
            { "Carrot", 2 },
            { "Pumpkin", 1 },
            { "Grape", 0 }
        };

        public Dictionary<string, float> MarketPrices { get; private set; } = new Dictionary<string, float>()
        {
            { "Carrot", 120 },
            { "Pumpkin", 130 },
            { "Grape", 100 }
        };

        public Dictionary<string, float> PriceTrends { get; private set; } = new Dictionary<string, float>()
        {
            { "Carrot", 20 },
            { "Pumpkin", 30 },
            { "Grape", -10 }
        };

        private float _lastPriceUpdateTime;
        private const float PriceUpdateInterval = 30f; // Update every 30s

        public void Start()
        {
            _lastPriceUpdateTime = Time.time;
        }

        public void Tick()
        {
            if (Time.time - _lastPriceUpdateTime > PriceUpdateInterval)
            {
                UpdateMarket();
                _lastPriceUpdateTime = Time.time;
            }
        }

        private void UpdateMarket()
        {
            foreach (var key in new List<string>(MarketPrices.Keys))
            {
                float change = UnityEngine.Random.Range(-20f, 25f);
                PriceTrends[key] = (float)Math.Round(change, 1);
                MarketPrices[key] = Math.Max(20, MarketPrices[key] + change);
            }
            OnDataChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold >= amount)
            {
                Gold -= amount;
                OnDataChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnDataChanged?.Invoke();
        }

        public void PlantCrop(string type)
        {
            if (Seeds > 0)
            {
                Seeds--;
                GrowingCrops[type]++;
                OnDataChanged?.Invoke();
                // Simulate growth completion after 10s (for demo)
                WaitAndHarvest(type).Forget();
            }
        }

        private async Cysharp.Threading.Tasks.UniTask WaitAndHarvest(string type)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(10000);
            GrowingCrops[type]--;
            Inventory[type]++;
            OnDataChanged?.Invoke();
        }
    }
}
