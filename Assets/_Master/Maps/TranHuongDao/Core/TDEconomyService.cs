using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Central service for the farming economic loop:
    ///   Seed grant → Plant → Wave countdown → Harvest → Sell → Gold.
    ///
    /// Wave integration (via IWaveManager events):
    ///   OnWaveStarted   → harvest ripe crops + roll new market prices
    ///   OnWaveCompleted → grant seeds to player
    ///
    /// Seeds accumulate across waves with no cap.
    /// Plants can be placed at any time (including during a wave).
    /// Price history (last 8 waves) is stored per crop for chart UI.
    /// </summary>
    public class TDEconomyService : IStartable, IDisposable
    {
        // ── Change notification (for UI panels that poll or subscribe) ────
        public event Action OnDataChanged;

        // ── Gold ──────────────────────────────────────────────────────────
        public int Gold { get; private set; } = 9999;

        // ── Seeds ─────────────────────────────────────────────────────────
        /// <summary>
        /// Seeds stockpile. Carries over every wave; never decreases except when planting.
        /// No maximum cap — the player can hoard as many seeds as desired.
        /// </summary>
        public int Seeds { get; private set; } = 0;

        // ── Crop Inventory (harvested, ready to sell) ──────────────────────
        public Dictionary<string, int> Inventory { get; private set; } = new Dictionary<string, int>();

        // ── Planted Crops (awaiting harvest) ──────────────────────────────
        private readonly List<PlantedCrop> _plantedCrops = new List<PlantedCrop>();

        /// <summary>Read-only view for UI: all batches currently growing in the field.</summary>
        public IReadOnlyList<PlantedCrop> PlantedCrops => _plantedCrops;

        // ── Market Prices (current wave) ───────────────────────────────────
        private readonly Dictionary<string, float> _currentPrices = new Dictionary<string, float>();

        // ── Price History (keyed by CropID, queue = oldest → newest) ──────
        private const int MaxPriceHistoryWaves = 8;
        private readonly Dictionary<string, Queue<float>> _priceHistory = new Dictionary<string, Queue<float>>();

        // ── Dependencies ──────────────────────────────────────────────────
        private readonly CropConfigSO _cropConfig;
        private readonly IWaveManager _waveManager;
        private readonly FD.IEventBus _eventBus;
        private readonly IConfigService _configService;

        // ── Constructor ───────────────────────────────────────────────────
        public TDEconomyService(IConfigService configService, IWaveManager waveManager, FD.IEventBus eventBus)
        {
            _configService = configService;
            _cropConfig    = _configService.GetConfig<CropConfigSO>();
            _waveManager   = waveManager;
            _eventBus      = eventBus;

            _waveManager.OnWaveStarted   += HandleWaveStarted;
            _waveManager.OnWaveCompleted += HandleWaveCompleted;

            InitializeCropData();
        }

        // ── IStartable ────────────────────────────────────────────────────
        public void Start()
        {
            // Roll initial prices so the UI has values before the first wave launches.
            RollMarketPrices();
            Debug.Log($"[TDEconomyService] Started. Gold={Gold}, Seeds={Seeds}.");
        }

        // ── IDisposable ───────────────────────────────────────────────────
        public void Dispose()
        {
            if (_waveManager != null)
            {
                _waveManager.OnWaveStarted   -= HandleWaveStarted;
                _waveManager.OnWaveCompleted -= HandleWaveCompleted;
            }
        }

        // ── Wave Handlers ─────────────────────────────────────────────────

        private void HandleWaveStarted(int waveIndex)
        {
            HarvestReadyCrops(waveIndex);
            RollMarketPrices();
            Debug.Log($"[TDEconomyService] Wave {waveIndex} started — harvest checked, prices rolled.");
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            GrantSeeds();
            Debug.Log($"[TDEconomyService] Wave {waveIndex} completed — {_cropConfig.SeedsPerWave} seeds granted.");
        }

        // ── Public API: Gold ──────────────────────────────────────────────

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnDataChanged?.Invoke();
            return true;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnDataChanged?.Invoke();
        }

        // ── Public API: Planting ──────────────────────────────────────────

        /// <summary>
        /// Plants <paramref name="seedCount"/> seeds of <paramref name="cropID"/>.
        /// May be called at any time — prep or mid-wave.
        /// Returns false if seeds are insufficient or the crop type is unknown.
        /// </summary>
        public bool TryPlantSeeds(string cropID, int seedCount)
        {
            if (seedCount <= 0)
            {
                Debug.LogWarning("[TDEconomyService] TryPlantSeeds: seedCount must be > 0.");
                return false;
            }
            if (Seeds < seedCount)
            {
                Debug.LogWarning($"[TDEconomyService] TryPlantSeeds: not enough seeds ({Seeds} < {seedCount}).");
                return false;
            }

            var def = _cropConfig.GetDefinition(cropID);
            if (def == null)
            {
                Debug.LogWarning($"[TDEconomyService] TryPlantSeeds: unknown cropID '{cropID}'.");
                return false;
            }

            // CurrentWaveIndex is the NEXT wave index during prep time
            // (WaveManager increments it after each wave starts).
            int currentWave   = _waveManager.CurrentWaveIndex;
            int harvestOnWave = currentWave + def.WavesToHarvest;

            Seeds -= seedCount;
            _plantedCrops.Add(new PlantedCrop
            {
                CropID        = cropID,
                PlantedOnWave = currentWave,
                HarvestOnWave = harvestOnWave,
                SeedCount     = seedCount
            });

            _eventBus?.Publish(new CropPlantedEvent(cropID, seedCount, harvestOnWave));
            OnDataChanged?.Invoke();

            Debug.Log($"[TDEconomyService] Planted {seedCount}x {cropID}. Ripens wave {harvestOnWave}.");
            return true;
        }

        // ── Public API: Selling ───────────────────────────────────────────

        /// <summary>
        /// Sells <paramref name="quantity"/> units of <paramref name="cropID"/> at the current
        /// market price. Returns false if the inventory is insufficient.
        /// </summary>
        public bool TrySellCrop(string cropID, int quantity, out int goldEarned)
        {
            goldEarned = 0;
            if (quantity <= 0) return false;

            int available = GetInventoryCount(cropID);
            if (available < quantity)
            {
                Debug.LogWarning($"[TDEconomyService] TrySellCrop: not enough {cropID} ({available} < {quantity}).");
                return false;
            }

            float pricePerUnit = GetCurrentPrice(cropID);
            goldEarned = Mathf.RoundToInt(pricePerUnit * quantity);

            Inventory[cropID] -= quantity;
            Gold += goldEarned;

            _eventBus?.Publish(new CropSoldEvent(cropID, quantity, goldEarned));
            OnDataChanged?.Invoke();

            Debug.Log($"[TDEconomyService] Sold {quantity}x {cropID} for {goldEarned}g (@ {pricePerUnit}g each).");
            return true;
        }

        /// <summary>Convenience: sells the entire inventory of one crop type.</summary>
        public bool TrySellAllCrop(string cropID, out int goldEarned)
            => TrySellCrop(cropID, GetInventoryCount(cropID), out goldEarned);

        // ── Public API: Queries ───────────────────────────────────────────

        /// <summary>Current market price per unit. Returns 0 if the crop is unknown.</summary>
        public float GetCurrentPrice(string cropID)
            => _currentPrices.TryGetValue(cropID, out float p) ? p : 0f;

        /// <summary>Inventory count for a crop. Returns 0 if unrecognised.</summary>
        public int GetInventoryCount(string cropID)
            => Inventory.TryGetValue(cropID, out int c) ? c : 0;

        /// <summary>
        /// Price history for a crop over the past N waves (oldest → newest).
        /// Used by PriceChartElement to render the polyline chart.
        /// </summary>
        public IReadOnlyList<float> GetPriceHistory(string cropID)
        {
            if (_priceHistory.TryGetValue(cropID, out var queue))
                return new List<float>(queue);
            return Array.Empty<float>();
        }

        /// <summary>
        /// All planted batches that will ripen on the NEXT wave.
        /// FarmPopup uses this to display the wave-ahead harvest preview.
        /// </summary>
        public List<PlantedCrop> GetRipeningNextWave()
        {
            int nextWave = _waveManager.CurrentWaveIndex + 1;
            var result   = new List<PlantedCrop>();
            foreach (var crop in _plantedCrops)
                if (crop.HarvestOnWave == nextWave)
                    result.Add(crop);
            return result;
        }

        // ── Private ───────────────────────────────────────────────────────

        private void InitializeCropData()
        {
            foreach (var def in _cropConfig.crops)
            {
                if (string.IsNullOrEmpty(def.CropID)) continue;

                if (!Inventory.ContainsKey(def.CropID))
                    Inventory[def.CropID] = 0;

                if (!_currentPrices.ContainsKey(def.CropID))
                    _currentPrices[def.CropID] = _cropConfig.MarketPriceMin;

                if (!_priceHistory.ContainsKey(def.CropID))
                    _priceHistory[def.CropID] = new Queue<float>(MaxPriceHistoryWaves);
            }
        }

        private void GrantSeeds()
        {
            int granted = _cropConfig.SeedsPerWave;
            Seeds += granted;
            _eventBus?.Publish(new SeedsGrantedEvent(granted, Seeds));
            OnDataChanged?.Invoke();
        }

        private void HarvestReadyCrops(int currentWave)
        {
            bool anyHarvested = false;

            // Iterate backwards so RemoveAt doesn't disturb unprocessed indices.
            for (int i = _plantedCrops.Count - 1; i >= 0; i--)
            {
                PlantedCrop batch = _plantedCrops[i];
                if (batch.HarvestOnWave > currentWave) continue;

                var def = _cropConfig.GetDefinition(batch.CropID);
                if (def == null)
                {
                    Debug.LogWarning($"[TDEconomyService] Harvest: unknown cropID '{batch.CropID}' — skipping.");
                    _plantedCrops.RemoveAt(i);
                    continue;
                }

                // Roll yield independently per seed for genuine randomness.
                int totalYield = 0;
                for (int s = 0; s < batch.SeedCount; s++)
                    totalYield += UnityEngine.Random.Range(def.YieldMin, def.YieldMax + 1);

                Inventory[batch.CropID] += totalYield;
                _plantedCrops.RemoveAt(i);
                anyHarvested = true;

                _eventBus?.Publish(new CropHarvestedEvent(batch.CropID, totalYield));
                Debug.Log($"[TDEconomyService] Harvested {batch.SeedCount}x {batch.CropID} → {totalYield} fruits.");
            }

            if (anyHarvested) OnDataChanged?.Invoke();
        }

        private void RollMarketPrices()
        {
            foreach (var def in _cropConfig.crops)
            {
                if (string.IsNullOrEmpty(def.CropID)) continue;

                float newPrice = Mathf.Round(UnityEngine.Random.Range(
                    _cropConfig.MarketPriceMin, _cropConfig.MarketPriceMax));

                _currentPrices[def.CropID] = newPrice;

                // Push to history queue, evicting the oldest entry when full.
                if (_priceHistory.TryGetValue(def.CropID, out var queue))
                {
                    queue.Enqueue(newPrice);
                    if (queue.Count > MaxPriceHistoryWaves)
                        queue.Dequeue();
                }
            }

            _eventBus?.Publish(new MarketPricesUpdatedEvent(_currentPrices));
            OnDataChanged?.Invoke();
        }
    }
}
