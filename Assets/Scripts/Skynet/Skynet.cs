// Skynet: cliff-edge structure that passively catches debris in real time.
// Every 90-180 real seconds, rolls StormDebrisLootTable into internal buffer (max 6 stacks).
// Swaps to 'has-catch' sprite frame when buffer non-empty.
// Interact transfers buffer to player inventory.
// Offline accrual: on Initialize(lastCollectedUnixTime), grants floor(elapsed/150s) rolls capped at 6.
// LastCollectedUnixTime get/set for save agent.
using System;
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Skynet
{
    public class Skynet : Building.Structure
    {
        private const int   MaxBufferStacks   = 6;
        private const float MinAccrualSeconds = 90f;
        private const float MaxAccrualSeconds = 180f;
        private const float OfflineRollPeriod = 150f;

        // Internal buffer: up to MaxBufferStacks entries
        private readonly List<(string itemId, int amount)> _buffer = new();

        private float   _accrualTimer;
        private System.Random _rng = new System.Random();

        // Sprites: [0] = empty, [1] = has-catch
        private SpriteRenderer _sr;
        private Sprite[] _frames;

        // Exposed for save agent
        public long LastCollectedUnixTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public System.Collections.Generic.IReadOnlyList<(string itemId, int amount)> GetBufferContents() =>
            _buffer.AsReadOnly();

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _frames = SpriteLoader.LoadStrip("Sprites/structures/skynet", 96);
            _accrualTimer = RollAccrualInterval();

            UpdateSprite();
        }

        // Real-time Update — deliberate per spec hybrid clock
        private void Update()
        {
            _accrualTimer -= Time.deltaTime;
            if (_accrualTimer <= 0f)
            {
                _accrualTimer = RollAccrualInterval();
                TryAccrue();
            }
        }

        /// <summary>Restore buffer contents and collection timestamp from save.</summary>
        public void RestoreFromSave(long lastCollectedUnixTime,
            System.Collections.Generic.IEnumerable<(string itemId, int amount)> buffer)
        {
            LastCollectedUnixTime = lastCollectedUnixTime;
            _buffer.Clear();

            if (buffer != null)
            {
                foreach (var (itemId, amount) in buffer)
                {
                    if (string.IsNullOrEmpty(itemId) || amount <= 0) continue;
                    if (_buffer.Count >= MaxBufferStacks) break;
                    _buffer.Add((itemId, amount));
                }
            }

            UpdateSprite();
        }

        /// <summary>
        /// Called by Bootstrap (or save restore) to grant offline accruals.
        /// </summary>
        public void InitializeOfflineAccrual(long lastCollectedUnixTime)
        {
            LastCollectedUnixTime = lastCollectedUnixTime;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int rolls = SkynetAccrual.OfflineRollCount(now - lastCollectedUnixTime, OfflineRollPeriod, MaxBufferStacks);
            for (int i = 0; i < rolls; i++)
                TryAccrue();
        }

        private void TryAccrue()
        {
            if (_buffer.Count >= MaxBufferStacks) return;

            var table = GameDatabase.StormDebrisLootTable;
            if (table == null) return;

            var (itemId, amount) = table.Roll(_rng);

            // Merge with existing stack if possible
            bool merged = false;
            for (int i = 0; i < _buffer.Count; i++)
            {
                if (_buffer[i].itemId == itemId)
                {
                    _buffer[i] = (itemId, _buffer[i].amount + amount);
                    merged = true;
                    break;
                }
            }
            if (!merged && _buffer.Count < MaxBufferStacks)
                _buffer.Add((itemId, amount));

            UpdateSprite();
        }

        private float RollAccrualInterval()
        {
            return MinAccrualSeconds + (float)_rng.NextDouble() * (MaxAccrualSeconds - MinAccrualSeconds);
        }

        public override string InteractionPrompt =>
            _buffer.Count > 0 ? $"Collect Skynet ({_buffer.Count} stacks)" : "Skynet (empty)";

        public override void Interact(PlayerController player)
        {
            if (TryDemolishWithHammer(player)) return;
            if (_buffer.Count == 0) return;

            var invComp = player.GetComponent<PlayerInventoryComponent>();
            if (invComp == null) return;
            var inv = invComp.Inventory;

            foreach (var (itemId, amount) in _buffer)
                inv.TryAdd(itemId, amount);

            _buffer.Clear();
            LastCollectedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            EventBus.Publish(new Debris.SparkleEvent
            {
                X = transform.position.x,
                Y = transform.position.y
            });

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (_sr == null || _frames == null || _frames.Length < 2) return;
            _sr.sprite = _frames[_buffer.Count > 0 ? 1 : 0];
        }
    }
}
