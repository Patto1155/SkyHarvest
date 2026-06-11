// DebrisObject: falls onto island edge over ~1.5s with scaling shadow.
// Sits 60-120 real seconds, then crumbles (destroyed).
// Interact scavenges: rolls 1-3 loot entries, adds to player inventory,
// publishes DebrisScavengedEvent, sparkle fx event, destroys self.
// Uses DebrisLootTable / StormDebrisLootTable from GameDatabase.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.Debris
{
    public class DebrisObject : MonoBehaviour, IInteractable
    {
        private enum Phase { Falling, Waiting, Crumbling }

        private Phase _phase = Phase.Falling;
        private Vector3 _startPos;
        private Vector3 _landingPos;
        private float _fallElapsed;
        private const float FallDuration = 1.5f;

        private float _waitTimer;
        private System.Random _rng;

        // Shadow child object
        private GameObject _shadowGo;
        private SpriteRenderer _shadowSr;

        private bool _scavenged;
        private bool _registered;

        public string InteractionPrompt => "Scavenge Debris";

        public void InitiateFall(Vector3 landingPos, Vector2Int landingGridPos, System.Random rng)
        {
            _startPos    = transform.position;
            _landingPos  = landingPos;
            _rng         = rng;
            _phase       = Phase.Falling;
            _fallElapsed = 0f;

            // Create shadow
            _shadowGo = new GameObject("DebrisShadow");
            _shadowGo.transform.SetParent(transform, false);
            _shadowSr = _shadowGo.AddComponent<SpriteRenderer>();
            _shadowSr.sortingOrder = -1;
            _shadowSr.color = new Color(0f, 0f, 0f, 0.4f);
            _shadowGo.transform.localPosition = Vector3.zero;
            _shadowGo.transform.localScale    = Vector3.zero;
        }

        // Real-time Update — deliberate per spec hybrid clock
        private void Update()
        {
            switch (_phase)
            {
                case Phase.Falling:
                    TickFall();
                    break;
                case Phase.Waiting:
                    _waitTimer -= Time.deltaTime;
                    if (_waitTimer <= 0f) StartCrumbling();
                    break;
                case Phase.Crumbling:
                    Destroy(gameObject);
                    break;
            }
        }

        private void TickFall()
        {
            _fallElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fallElapsed / FallDuration);

            // Arc: fall with slight upward arc
            float baseY = Mathf.Lerp(_startPos.y, _landingPos.y, t);
            float arc   = Mathf.Sin(t * Mathf.PI) * 0.8f;

            transform.position = new Vector3(
                Mathf.Lerp(_startPos.x, _landingPos.x, t),
                baseY + arc,
                0f);

            if (_shadowGo != null)
                _shadowGo.transform.localScale = Vector3.one * Mathf.Lerp(0f, 0.5f, t);

            if (t >= 1f)
                OnLanded();
        }

        private void OnLanded()
        {
            _phase = Phase.Waiting;
            transform.position = _landingPos;
            _waitTimer = 60f + (float)_rng.NextDouble() * 60f; // 60–120 real seconds

            if (_shadowGo != null)
                _shadowGo.transform.localScale = new Vector3(0.5f, 0.25f, 1f);

            // Register with InteractableRegistry (simple signature, no position getter)
            if (!_registered)
            {
                InteractableRegistry.Register(this);
                _registered = true;
            }

            EventBus.Publish(new DebrisLandedEvent { X = _landingPos.x, Y = _landingPos.y });
        }

        private void StartCrumbling()
        {
            _phase = Phase.Crumbling;
        }

        private void OnDestroy()
        {
            if (_registered)
            {
                InteractableRegistry.Unregister(this);
                _registered = false;
            }
        }

        public void Interact(PlayerController player)
        {
            if (_scavenged || _phase == Phase.Falling) return;
            _scavenged = true;

            // Get inventory from PlayerInventoryComponent
            var invComp = player.GetComponent<PlayerInventoryComponent>();
            if (invComp == null) return;
            var inv = invComp.Inventory;

            // Choose loot table based on current weather
            var weather = Weather.WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;
            bool useStorm = weather == WeatherType.HeavyStorm || weather == WeatherType.GaleWinds;

            var lootTable = useStorm
                ? GameDatabase.StormDebrisLootTable
                : GameDatabase.DebrisLootTable;

            if (lootTable != null)
            {
                int rolls = _rng.Next(1, 4);
                for (int i = 0; i < rolls; i++)
                {
                    var (itemId, amount) = lootTable.Roll(_rng);
                    inv.TryAdd(itemId, amount);
                }
            }

            EventBus.Publish(new SparkleEvent { X = _landingPos.x, Y = _landingPos.y });
            EventBus.Publish(new DebrisScavengedEvent { X = _landingPos.x, Y = _landingPos.y });

            Destroy(gameObject);
        }
    }

    /// <summary>Published when a collectible sparkle should play.</summary>
    public struct SparkleEvent { public float X; public float Y; }
}
