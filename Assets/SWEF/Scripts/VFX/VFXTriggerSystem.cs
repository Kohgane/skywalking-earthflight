// VFXTriggerSystem.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;

// Integration stubs — guarded so the file compiles without the target packages present.
#if SWEF_FLIGHT_AVAILABLE
using SWEF.Flight;
#endif
#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif
#if SWEF_BIOME_AVAILABLE
using SWEF.Biome;
#endif

namespace SWEF.VFX
{
    /// <summary>
    /// Singleton event-driven VFX spawner.
    ///
    /// <para>Trigger rules are registered via <see cref="RegisterTrigger"/> and evaluated
    /// whenever a relevant game event fires (altitude milestone, speed threshold, stall,
    /// sonic boom, weather change, biome transition, landing, takeoff).</para>
    ///
    /// <para>Each trigger has an independent cooldown, numeric priority (higher fires first),
    /// and an optional condition predicate. Spawning is delegated to
    /// <see cref="VFXPoolManager"/>.</para>
    /// </summary>
    public sealed class VFXTriggerSystem : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────────

        /// <summary>Active singleton instance.</summary>
        public static VFXTriggerSystem Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Spawn Anchor")]
        [Tooltip("Transform used as the default spawn origin when no override is provided.")]
        [SerializeField] private Transform defaultSpawnAnchor;

        [Header("Integration References")]
        [Tooltip("Optional reference to the scene FlightController for velocity/throttle data.")]
        [SerializeField] private MonoBehaviour flightControllerRef;

        [Tooltip("Optional reference to the scene WeatherManager for weather events.")]
        [SerializeField] private MonoBehaviour weatherManagerRef;

        [Tooltip("Optional reference to the scene AltitudeController.")]
        [SerializeField] private MonoBehaviour altitudeControllerRef;

        // ── Trigger Configuration ─────────────────────────────────────────────────

        /// <summary>
        /// Describes a single trigger rule that maps a flight or world event to a VFX spawn.
        /// </summary>
        [Serializable]
        public sealed class VFXTriggerConfig
        {
            /// <summary>Human-readable name for editor identification.</summary>
            [Tooltip("Human-readable name for identification.")]
            public string name = "Unnamed Trigger";

            /// <summary>VFX type to spawn when this trigger fires.</summary>
            [Tooltip("VFX type spawned when this trigger fires.")]
            public VFXType vfxType;

            /// <summary>Minimum seconds between successive fires of this trigger.</summary>
            [Tooltip("Cooldown in seconds between successive fires.")]
            [Min(0f)] public float cooldownSeconds = 2f;

            /// <summary>Priority value; higher priority triggers are evaluated first when multiple fire simultaneously.</summary>
            [Tooltip("Higher values fire before lower values when multiple triggers activate at the same time.")]
            public int priority;

            /// <summary>Uniform scale applied to spawned instances.</summary>
            [Tooltip("Scale applied to the spawned VFX instance.")]
            [Min(0.01f)] public float spawnScale = 1f;

            /// <summary>Duration override passed to the spawn request (≤ 0 = profile default).</summary>
            [Tooltip("Duration override in seconds; ≤ 0 uses the VFX profile default.")]
            public float durationOverride;

            /// <summary>Whether the trigger is currently active.</summary>
            [Tooltip("Enable or disable this trigger without removing it.")]
            public bool enabled = true;

            /// <summary>Timestamp of the last successful fire (uses <c>Time.time</c>).</summary>
            [NonSerialized] public float LastFireTime = -999f;
        }

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly List<VFXTriggerConfig>  _triggers         = new List<VFXTriggerConfig>();
        private readonly Dictionary<string, float> _cooldownTracker = new Dictionary<string, float>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()  => SubscribeEvents();
        private void OnDisable() => UnsubscribeEvents();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a new trigger rule with the system.
        /// Duplicate names are allowed; each is tracked independently.
        /// </summary>
        /// <param name="config">Trigger configuration to register.</param>
        public void RegisterTrigger(VFXTriggerConfig config)
        {
            if (config == null) return;
            _triggers.Add(config);
            // Sort by priority descending
            _triggers.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        /// <summary>Removes all trigger rules whose <c>name</c> matches <paramref name="triggerName"/>.</summary>
        /// <param name="triggerName">Name of the trigger to remove.</param>
        public void UnregisterTrigger(string triggerName)
        {
            _triggers.RemoveAll(t => t.name == triggerName);
        }

        /// <summary>
        /// Manually fires a VFX trigger of the given type at the provided world position,
        /// bypassing cooldown and condition checks.
        /// </summary>
        /// <param name="type">VFX type to spawn.</param>
        /// <param name="position">World-space position for the effect.</param>
        /// <param name="scale">Optional uniform scale override.</param>
        public VFXInstanceHandle FireImmediate(VFXType type, Vector3 position, float scale = 1f)
        {
            if (VFXPoolManager.Instance == null) return VFXInstanceHandle.Invalid;
            return VFXPoolManager.Instance.Spawn(new VFXSpawnRequest
            {
                type     = type,
                position = position,
                rotation = Quaternion.identity,
                scale    = scale,
                parent   = null,
                durationOverride = 0f
            });
        }

        // ── Event Integration ─────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
#if SWEF_FLIGHT_AVAILABLE
            if (FlightController.Instance != null)
            {
                FlightController.Instance.OnSonicBoom        += HandleSonicBoom;
                FlightController.Instance.OnStall            += HandleStall;
                FlightController.Instance.OnLanding          += HandleLanding;
                FlightController.Instance.OnTakeoff          += HandleTakeoff;
            }
            if (AltitudeController.Instance != null)
                AltitudeController.Instance.OnAltitudeMilestone += HandleAltitudeMilestone;
#endif
#if SWEF_WEATHER_AVAILABLE
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.OnWeatherChanged += HandleWeatherChanged;
#endif
#if SWEF_BIOME_AVAILABLE
            // BiomeClassifier is static; biome transitions would be forwarded by the scene controller
#endif
        }

        private void UnsubscribeEvents()
        {
#if SWEF_FLIGHT_AVAILABLE
            if (FlightController.Instance != null)
            {
                FlightController.Instance.OnSonicBoom        -= HandleSonicBoom;
                FlightController.Instance.OnStall            -= HandleStall;
                FlightController.Instance.OnLanding          -= HandleLanding;
                FlightController.Instance.OnTakeoff          -= HandleTakeoff;
            }
            if (AltitudeController.Instance != null)
                AltitudeController.Instance.OnAltitudeMilestone -= HandleAltitudeMilestone;
#endif
#if SWEF_WEATHER_AVAILABLE
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.OnWeatherChanged -= HandleWeatherChanged;
#endif
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleSonicBoom()        => TryFireCategory(VFXType.SonicBoom,    GetAnchorPosition());
        private void HandleStall()            => TryFireCategory(VFXType.EngineExhaust, GetAnchorPosition());
        private void HandleLanding()          => TryFireCategory(VFXType.LandingDust,  GetAnchorPosition());
        private void HandleTakeoff()          => TryFireCategory(VFXType.EngineExhaust, GetAnchorPosition());
        private void HandleAltitudeMilestone(float altitude) => TryFireCategory(VFXType.AltitudeGlow, GetAnchorPosition());

#if SWEF_WEATHER_AVAILABLE
        private void HandleWeatherChanged(WeatherConditionData weather)
        {
            switch (weather.weatherType)
            {
                case WeatherType.Rain:
                    TryFireCategory(VFXType.RainSplash,    GetAnchorPosition()); break;
                case WeatherType.Snow:
                    TryFireCategory(VFXType.SnowFlurry,    GetAnchorPosition()); break;
                case WeatherType.Thunderstorm:
                    TryFireCategory(VFXType.LightningStrike, GetAnchorPosition()); break;
            }
        }
#endif

        /// <summary>
        /// Called by external controllers (e.g., <see cref="EnvironmentVFXController"/>)
        /// to signal a biome transition.
        /// </summary>
        /// <param name="newBiome">The biome the aircraft has entered.</param>
        public void NotifyBiomeTransition(int newBiome)
        {
            // Map biome index to relevant VFX; callers can use the raw int to avoid
            // a hard dependency on SWEF.Biome.BiomeType.
            switch (newBiome)
            {
                case 11: TryFireCategory(VFXType.VolcanicAsh, GetAnchorPosition()); break; // Volcanic
                case 0:  TryFireCategory(VFXType.SandStorm,   GetAnchorPosition()); break; // Desert
                case 4:  TryFireCategory(VFXType.SnowFlurry,  GetAnchorPosition()); break; // Tundra
            }
        }

        // ── Core Evaluation ───────────────────────────────────────────────────────

        private void TryFireCategory(VFXType type, Vector3 position)
        {
            foreach (VFXTriggerConfig trigger in _triggers)
            {
                if (!trigger.enabled || trigger.vfxType != type) continue;
                if (Time.time - trigger.LastFireTime < trigger.cooldownSeconds) continue;

                trigger.LastFireTime = Time.time;
                if (VFXPoolManager.Instance != null)
                {
                    VFXPoolManager.Instance.Spawn(new VFXSpawnRequest
                    {
                        type             = type,
                        position         = position,
                        rotation         = Quaternion.identity,
                        scale            = trigger.spawnScale,
                        parent           = null,
                        durationOverride = trigger.durationOverride
                    });
                }
                break; // Only fire highest-priority matching trigger
            }
        }

        private Vector3 GetAnchorPosition()
        {
            return defaultSpawnAnchor != null ? defaultSpawnAnchor.position : Vector3.zero;
        }

#if UNITY_EDITOR
        [ContextMenu("Log Registered Triggers")]
        private void EditorLogTriggers()
        {
            Debug.Log($"[VFXTriggerSystem] {_triggers.Count} triggers registered:");
            foreach (var t in _triggers)
                Debug.Log($"  [{t.priority}] {t.name} → {t.vfxType} (cd={t.cooldownSeconds}s, enabled={t.enabled})");
        }
#endif
    }
}
