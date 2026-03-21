using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Events
{
    /// <summary>
    /// Singleton MonoBehaviour that acts as the brain of the Dynamic Event System.
    /// Loads all <see cref="WorldEventData"/> assets from <c>Resources/Events/</c>,
    /// then periodically evaluates spawn conditions and manages active
    /// <see cref="WorldEventInstance"/> objects.
    /// </summary>
    public class EventScheduler : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static EventScheduler Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Scheduling")]
        [Tooltip("How often (in seconds) the scheduler evaluates spawn conditions.")]
        [SerializeField] private float evaluationIntervalSeconds = 60f;

        [Tooltip("Hour of the day [0–24] before which events are suppressed.")]
        [SerializeField] private float dayStartHour = 6f;

        [Tooltip("Hour of the day [0–24] after which events are suppressed.")]
        [SerializeField] private float dayEndHour = 22f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new event instance is spawned.</summary>
        public event Action<WorldEventInstance> OnEventSpawned;

        /// <summary>Fired when an event instance has expired and been removed.</summary>
        public event Action<WorldEventInstance> OnEventExpired;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<WorldEventData>     _catalog      = new List<WorldEventData>();
        private readonly List<WorldEventInstance> _activeEvents = new List<WorldEventInstance>();
        private readonly Dictionary<string, float> _cooldownMap = new Dictionary<string, float>();

        private SWEF.Weather.WeatherManager _weatherManager;
        private Coroutine _schedulerCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCatalog();
        }

        private void Start()
        {
            _weatherManager = FindFirstObjectByType<SWEF.Weather.WeatherManager>();
            _schedulerCoroutine = StartCoroutine(SchedulerLoop());
        }

        private void Update()
        {
            TickActiveEvents();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Forces the immediate spawn of the specified event, bypassing all scheduling
        /// rules (probability, cooldown, season). Useful for testing or scripted sequences.
        /// </summary>
        /// <param name="data">Event template to spawn.</param>
        /// <returns>The newly created instance, or <c>null</c> if data is null.</returns>
        public WorldEventInstance ForceSpawnEvent(WorldEventData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SWEF] EventScheduler.ForceSpawnEvent: data is null.");
                return null;
            }
            return SpawnEvent(data);
        }

        /// <summary>
        /// Returns a snapshot list of all currently active (non-ended) event instances.
        /// </summary>
        public List<WorldEventInstance> GetActiveEvents()
        {
            return new List<WorldEventInstance>(_activeEvents);
        }

        /// <summary>
        /// Returns the subset of catalog entries that have not yet hit their cooldown
        /// and satisfy seasonal constraints — i.e. events that could spawn soon.
        /// </summary>
        public List<WorldEventData> GetUpcomingEvents()
        {
            var upcoming = new List<WorldEventData>();
            int currentMonth = DateTime.Now.Month;
            SeasonalConstraint currentSeason = MonthToSeason(currentMonth);

            foreach (var data in _catalog)
            {
                if (!PassesSeasonFilter(data, currentSeason)) continue;
                if (_cooldownMap.TryGetValue(data.eventId, out float cooldownEnd) && Time.time < cooldownEnd) continue;
                upcoming.Add(data);
            }
            return upcoming;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void LoadCatalog()
        {
            var assets = Resources.LoadAll<WorldEventData>("Events");
            _catalog.Clear();
            _catalog.AddRange(assets);
            Debug.Log($"[SWEF] EventScheduler: loaded {_catalog.Count} event definition(s) from Resources/Events/.");
        }

        private IEnumerator SchedulerLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(evaluationIntervalSeconds);
                EvaluateSpawns();
            }
        }

        private void EvaluateSpawns()
        {
            int currentMonth = DateTime.Now.Month;
            SeasonalConstraint currentSeason = MonthToSeason(currentMonth);
            float currentHour = DateTime.Now.Hour + DateTime.Now.Minute / 60f;
            bool isNight = currentHour < dayStartHour || currentHour > dayEndHour;

            foreach (var data in _catalog)
            {
                // Season gate
                if (!PassesSeasonFilter(data, currentSeason)) continue;

                // Cooldown gate
                if (_cooldownMap.TryGetValue(data.eventId, out float cooldownEnd) && Time.time < cooldownEnd) continue;

                // Concurrent instances cap
                int activeCount = CountActiveInstancesOf(data.eventId);
                if (activeCount >= data.maxConcurrentInstances) continue;

                // Time of day gate (only restrict aurora / meteor to night)
                if (isNight && (data.eventType == WorldEventType.AirShow || data.eventType == WorldEventType.Festival))
                    continue;

                // Weather-dependency (optional null-safe)
                if (!PassesWeatherFilter(data)) continue;

                // Probability roll
                if (UnityEngine.Random.value > data.spawnProbability) continue;

                SpawnEvent(data);

                // Non-recurring events go on an indefinite cooldown
                if (!data.isRecurring)
                    _cooldownMap[data.eventId] = float.MaxValue;
            }
        }

        private WorldEventInstance SpawnEvent(WorldEventData data)
        {
            Vector3 spawnPos = data.spawnRegion.center +
                               UnityEngine.Random.insideUnitSphere * data.spawnRegion.radius;
            float durationSec = UnityEngine.Random.Range(data.minDurationMinutes, data.maxDurationMinutes) * 60f;

            var instance = new WorldEventInstance(data, spawnPos, durationSec);
            instance.Activate();
            _activeEvents.Add(instance);

            // Reset cooldown timer
            _cooldownMap[data.eventId] = Time.time + data.cooldownMinutes * 60f;

            Debug.Log($"[SWEF] EventScheduler: spawned event '{data.eventId}' (instance {instance.instanceId}).");
            OnEventSpawned?.Invoke(instance);
            return instance;
        }

        private void TickActiveEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var inst = _activeEvents[i];

                if (inst.state == WorldEventState.Active && inst.RemainingTime <= 0f)
                    inst.Expire();

                if (inst.state == WorldEventState.Ended)
                {
                    _activeEvents.RemoveAt(i);
                    OnEventExpired?.Invoke(inst);
                    Debug.Log($"[SWEF] EventScheduler: event instance {inst.instanceId} ended.");
                }
            }
        }

        private int CountActiveInstancesOf(string eventId)
        {
            int count = 0;
            foreach (var inst in _activeEvents)
            {
                if (inst.eventData != null && inst.eventData.eventId == eventId &&
                    inst.state != WorldEventState.Ended)
                    count++;
            }
            return count;
        }

        private bool PassesSeasonFilter(WorldEventData data, SeasonalConstraint current)
        {
            return data.seasonalConstraint == SeasonalConstraint.Any ||
                   data.seasonalConstraint == current;
        }

        private bool PassesWeatherFilter(WorldEventData data)
        {
            if (_weatherManager == null) return true;

            var weather = _weatherManager.CurrentWeather;

            // Auroras require clear or partially cloudy skies
            if (data.eventType == WorldEventType.Aurora && weather.cloudCover > 0.5f)
                return false;

            // Rare weather events prefer stormy conditions
            if (data.eventType == WorldEventType.RareWeather &&
                weather.type != SWEF.Weather.WeatherType.Thunderstorm &&
                weather.type != SWEF.Weather.WeatherType.Hail &&
                weather.type != SWEF.Weather.WeatherType.Sandstorm)
                return false;

            return true;
        }

        private static SeasonalConstraint MonthToSeason(int month)
        {
            return month switch
            {
                3 or 4 or 5   => SeasonalConstraint.Spring,
                6 or 7 or 8   => SeasonalConstraint.Summer,
                9 or 10 or 11 => SeasonalConstraint.Autumn,
                _             => SeasonalConstraint.Winter
            };
        }
    }
}
