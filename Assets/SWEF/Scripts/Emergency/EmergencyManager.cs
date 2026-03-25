using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Central singleton that manages all emergency scenario triggering,
    /// escalation, resolution, and cross-system integration for SWEF.
    /// Attach to a persistent GameObject in the bootstrap scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static EmergencyManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [SerializeField] private EmergencyConfig config = new EmergencyConfig();

        [Header("Scenario Database")]
        [Tooltip("All registered emergency scenarios. Populated at runtime with defaults.")]
        [SerializeField] private List<EmergencyScenario> scenarioDatabase = new List<EmergencyScenario>();

        #endregion

        #region Events

        /// <summary>Fired when a new emergency is triggered.</summary>
        public event Action<ActiveEmergency> OnEmergencyTriggered;

        /// <summary>Fired when an active emergency's severity escalates.</summary>
        public event Action<ActiveEmergency, EmergencySeverity> OnEmergencyEscalated;

        /// <summary>Fired when an active emergency transitions to a new phase.</summary>
        public event Action<ActiveEmergency, EmergencyPhase> OnEmergencyPhaseChanged;

        /// <summary>Fired when an emergency is resolved (successfully or otherwise).</summary>
        public event Action<EmergencyResolution> OnEmergencyResolved;

        /// <summary>Fired when a distress call is transmitted.</summary>
        public event Action<ActiveEmergency, DistressCallType> OnDistressCallMade;

        #endregion

        #region Public Properties

        /// <summary>Read-only view of all currently active emergencies.</summary>
        public IReadOnlyList<ActiveEmergency> ActiveEmergencies => _activeEmergencies;

        /// <summary>Exposes the runtime configuration object.</summary>
        public EmergencyConfig Config => config;

        #endregion

        #region Private State

        private readonly List<ActiveEmergency> _activeEmergencies = new List<ActiveEmergency>();
        private readonly Dictionary<string, EmergencyScenario> _scenarioLookup = new Dictionary<string, EmergencyScenario>();
        private Coroutine _randomEmergencyCoroutine;
        private static int _emergencyIdCounter;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefaultScenarios();
            BuildLookup();
        }

        private void Start()
        {
            if (config.enableRandomEmergencies)
                _randomEmergencyCoroutine = StartCoroutine(RandomEmergencyLoop());

            SubscribeCrossSystems();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _activeEmergencies.Count - 1; i >= 0; i--)
                TickEmergency(_activeEmergencies[i], dt);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Initialization

        private void RegisterDefaultScenarios()
        {
            scenarioDatabase.Clear();

            Register("engine_failure",         "emergency_type_engine_failure",         "emergency_desc_engine_failure",         EmergencyType.EngineFailure,          EmergencySeverity.Warning,   90f,  480f, false, true,  3, DistressCallType.PanPan,   "checklist_throttle_idle", "checklist_fuel_switch", "checklist_engine_restart", "checklist_squawk_7700", "checklist_mayday_call");
            Register("dual_engine_failure",    "emergency_type_dual_engine_failure",    "emergency_desc_dual_engine_failure",    EmergencyType.DualEngineFailure,      EmergencySeverity.Emergency, 45f,  240f, false, true,  5, DistressCallType.Mayday,   "checklist_throttle_idle", "checklist_fuel_both", "checklist_apu_start", "checklist_squawk_7700", "checklist_mayday_call", "checklist_glide_best");
            Register("fuel_starvation",        "emergency_type_fuel_starvation",        "emergency_desc_fuel_starvation",        EmergencyType.FuelStarvation,         EmergencySeverity.Warning,   60f,  360f, false, true,  3, DistressCallType.PanPan,   "checklist_fuel_switch", "checklist_cross_feed", "checklist_squawk_7700", "checklist_divert_nearest");
            Register("fuel_leak",              "emergency_type_fuel_leak",              "emergency_desc_fuel_leak",              EmergencyType.FuelLeak,               EmergencySeverity.Caution,   120f, 600f, true,  false, 2, DistressCallType.PanPan,   "checklist_fuel_quantity", "checklist_fuel_shutoff", "checklist_divert_nearest");
            Register("bird_strike",            "emergency_type_bird_strike",            "emergency_desc_bird_strike",            EmergencyType.BirdStrike,             EmergencySeverity.Caution,   180f, 600f, true,  false, 2, DistressCallType.PanPan,   "checklist_engine_check", "checklist_flight_controls", "checklist_assess_damage");
            Register("structural_damage",      "emergency_type_structural_damage",      "emergency_desc_structural_damage",      EmergencyType.StructuralDamage,       EmergencySeverity.Warning,   90f,  480f, false, true,  4, DistressCallType.Mayday,   "checklist_reduce_speed", "checklist_avoid_maneuvers", "checklist_squawk_7700", "checklist_mayday_call");
            Register("icing_critical",         "emergency_type_icing_critical",         "emergency_desc_icing_critical",         EmergencyType.IcingCritical,          EmergencySeverity.Warning,   60f,  360f, false, true,  3, DistressCallType.PanPan,   "checklist_anti_ice_on", "checklist_descend_warmer", "checklist_reduce_aoa");
            Register("electrical_failure",     "emergency_type_electrical_failure",     "emergency_desc_electrical_failure",     EmergencyType.ElectricalFailure,      EmergencySeverity.Warning,   120f, 480f, false, true,  3, DistressCallType.PanPan,   "checklist_bus_tie", "checklist_shed_load", "checklist_squawk_7700", "checklist_divert_nearest");
            Register("hydraulic_failure",      "emergency_type_hydraulic_failure",      "emergency_desc_hydraulic_failure",      EmergencyType.HydraulicFailure,       EmergencySeverity.Warning,   90f,  480f, false, true,  4, DistressCallType.PanPan,   "checklist_hyd_pump", "checklist_flight_controls", "checklist_gear_gravity", "checklist_divert_nearest");
            Register("fire_onboard",           "emergency_type_fire_onboard",           "emergency_desc_fire_onboard",           EmergencyType.FireOnboard,            EmergencySeverity.Emergency, 30f,  180f, false, true,  5, DistressCallType.Mayday,   "checklist_fire_agent_1", "checklist_fire_agent_2", "checklist_squawk_7700", "checklist_mayday_call", "checklist_rapid_descent");
            Register("depressurization",       "emergency_type_depressurization",       "emergency_desc_depressurization",       EmergencyType.Depressurization,       EmergencySeverity.Emergency, 20f,  120f, false, true,  4, DistressCallType.Mayday,   "checklist_oxygen_masks", "checklist_rapid_descent", "checklist_squawk_7700", "checklist_mayday_call");
            Register("navigation_failure",     "emergency_type_navigation_failure",     "emergency_desc_navigation_failure",     EmergencyType.NavigationFailure,      EmergencySeverity.Warning,   180f, 900f, true,  false, 2, DistressCallType.PanPan,   "checklist_nav_backup", "checklist_squawk_7700", "checklist_atc_contact");
            Register("communication_failure",  "emergency_type_communication_failure",  "emergency_desc_communication_failure",  EmergencyType.CommunicationFailure,   EmergencySeverity.Caution,   300f, 1200f, true, false, 1, DistressCallType.Squawk7600, "checklist_radio_backup", "checklist_squawk_7600");
            Register("control_surface_jam",    "emergency_type_control_surface_jam",    "emergency_desc_control_surface_jam",    EmergencyType.ControlSurfaceJam,      EmergencySeverity.Emergency, 60f,  360f, false, true,  5, DistressCallType.Mayday,   "checklist_autopilot_off", "checklist_differential_thrust", "checklist_squawk_7700", "checklist_mayday_call");
            Register("landing_gear_malfunction","emergency_type_landing_gear_malfunction","emergency_desc_landing_gear_malfunction",EmergencyType.LandingGearMalfunction,EmergencySeverity.Warning, 120f, 600f, true, true,  3, DistressCallType.PanPan,   "checklist_gear_gravity", "checklist_gear_manual", "checklist_low_pass", "checklist_squawk_7700");
        }

        private void Register(string id, string nameKey, string descKey, EmergencyType type,
            EmergencySeverity severity, float escalation, float limit,
            bool resolveInFlight, bool requiresLanding, int difficulty,
            DistressCallType call, params string[] steps)
        {
            scenarioDatabase.Add(new EmergencyScenario
            {
                scenarioId           = id,
                displayNameKey       = nameKey,
                descriptionKey       = descKey,
                type                 = type,
                defaultSeverity      = severity,
                escalationTime       = escalation,
                resolutionTimeLimit  = limit,
                canResolveInFlight   = resolveInFlight,
                requiresEmergencyLanding = requiresLanding,
                checklistStepKeys    = steps,
                difficultyRating     = difficulty,
                requiredCall         = call
            });
        }

        private void BuildLookup()
        {
            _scenarioLookup.Clear();
            foreach (var s in scenarioDatabase)
                _scenarioLookup[s.scenarioId] = s;
        }

        private void SubscribeCrossSystems()
        {
#if SWEF_WILDLIFE_AVAILABLE
            var wm = SWEF.Wildlife.WildlifeManager.Instance;
            if (wm != null)
                wm.OnBirdStrike += OnBirdStrikeReceived;
#endif
#if SWEF_FUEL_AVAILABLE
            var fm = SWEF.Fuel.FuelManager.Instance;
            if (fm != null)
                fm.OnFuelDepleted += OnFuelDepletedReceived;
#endif
#if SWEF_DAMAGE_AVAILABLE
            var dm = SWEF.Damage.DamageModel.Instance;
            if (dm != null)
                dm.OnCriticalDamage += OnCriticalDamageReceived;
#endif
        }

        #endregion

        #region Public API

        /// <summary>Trigger an emergency by scenario ID.</summary>
        /// <param name="scenarioId">ID matching a registered scenario.</param>
        /// <param name="triggerPosition">World position of the aircraft at trigger time.</param>
        /// <param name="triggerAltitude">Altitude in metres at trigger time.</param>
        /// <returns>The created <see cref="ActiveEmergency"/>, or null if limit reached.</returns>
        public ActiveEmergency TriggerEmergency(string scenarioId, Vector3 triggerPosition, float triggerAltitude)
        {
            if (_activeEmergencies.Count >= config.maxSimultaneousEmergencies)
                return null;

            if (!_scenarioLookup.TryGetValue(scenarioId, out var scenario))
            {
                Debug.LogWarning($"[EmergencyManager] Unknown scenario id: {scenarioId}");
                return null;
            }

            var emergency = new ActiveEmergency
            {
                emergencyId       = $"em_{++_emergencyIdCounter:D4}",
                scenario          = scenario,
                currentSeverity   = scenario.defaultSeverity,
                currentPhase      = EmergencyPhase.Detected,
                triggerTime       = Time.time,
                elapsedTime       = 0f,
                checklistProgress = 0,
                totalChecklistSteps = scenario.checklistStepKeys.Length,
                distressCallMade  = false,
                isEscalating      = true,
                triggerPosition   = triggerPosition,
                triggerAltitude   = triggerAltitude,
                divertAirportId   = string.Empty,
                damagePerSecond   = ComputeDamagePerSecond(scenario.defaultSeverity)
            };

            _activeEmergencies.Add(emergency);
            OnEmergencyTriggered?.Invoke(emergency);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EmergencyManager] Triggered '{scenarioId}' ({emergency.emergencyId}) severity={emergency.currentSeverity}");
#endif
            return emergency;
        }

        /// <summary>Trigger an emergency by EmergencyType (uses the default scenario for that type).</summary>
        public ActiveEmergency TriggerEmergencyByType(EmergencyType type, Vector3 position, float altitude)
        {
            foreach (var s in scenarioDatabase)
            {
                if (s.type == type)
                    return TriggerEmergency(s.scenarioId, position, altitude);
            }
            return null;
        }

        /// <summary>Advance an emergency to a new phase.</summary>
        public void SetPhase(ActiveEmergency emergency, EmergencyPhase phase)
        {
            if (emergency == null) return;
            emergency.currentPhase = phase;
            OnEmergencyPhaseChanged?.Invoke(emergency, phase);
        }

        /// <summary>Record a distress call for an active emergency.</summary>
        public void MakeDistressCall(ActiveEmergency emergency, DistressCallType callType)
        {
            if (emergency == null || emergency.distressCallMade) return;
            emergency.distressCallMade = true;
            OnDistressCallMade?.Invoke(emergency, callType);
        }

        /// <summary>Resolve an active emergency and write an outcome record.</summary>
        public EmergencyResolution ResolveEmergency(ActiveEmergency emergency, bool wasSuccessful,
            float landingDistance = 0f, float landingSpeed = 0f)
        {
            if (emergency == null) return null;
            _activeEmergencies.Remove(emergency);

            float score = ComputeScore(emergency, wasSuccessful, landingDistance, landingSpeed);
            var resolution = new EmergencyResolution
            {
                emergencyId             = emergency.emergencyId,
                type                    = emergency.scenario.type,
                finalPhase              = emergency.currentPhase,
                totalTime               = emergency.elapsedTime,
                checklistStepsCompleted = emergency.checklistProgress,
                checklistStepsTotal     = emergency.totalChecklistSteps,
                distressCallMade        = emergency.distressCallMade,
                landingDistance         = landingDistance,
                landingSpeed            = landingSpeed,
                wasSuccessful           = wasSuccessful,
                score                   = score
            };

            OnEmergencyResolved?.Invoke(resolution);
            return resolution;
        }

        /// <summary>Returns the registered scenario for the given type, or null.</summary>
        public EmergencyScenario GetScenario(EmergencyType type)
        {
            foreach (var s in scenarioDatabase)
                if (s.type == type) return s;
            return null;
        }

        #endregion

        #region Private Helpers

        private void TickEmergency(ActiveEmergency em, float dt)
        {
            em.elapsedTime += dt;

            // Apply continuous damage
            if (em.damagePerSecond > 0f)
            {
#if SWEF_DAMAGE_AVAILABLE
                SWEF.Damage.DamageModel.Instance?.ApplyDamage(em.damagePerSecond * dt);
#endif
            }

            // Escalation timer
            if (em.isEscalating && em.currentSeverity < EmergencySeverity.Mayday)
            {
                float timeSinceLastEscalation = em.elapsedTime % (em.scenario.escalationTime / config.emergencyEscalationRate);
                if (timeSinceLastEscalation < dt)
                    EscalateSeverity(em);
            }

            // Resolution time limit
            if (em.elapsedTime >= em.scenario.resolutionTimeLimit &&
                em.currentPhase != EmergencyPhase.Landed &&
                em.currentPhase != EmergencyPhase.Crashed &&
                em.currentPhase != EmergencyPhase.Rescued &&
                em.currentPhase != EmergencyPhase.Resolved)
            {
                SetPhase(em, EmergencyPhase.Crashed);
                ResolveEmergency(em, false);
            }
        }

        private void EscalateSeverity(ActiveEmergency em)
        {
            var prev = em.currentSeverity;
            em.currentSeverity = (EmergencySeverity)Mathf.Min((int)em.currentSeverity + 1, (int)EmergencySeverity.Mayday);
            em.damagePerSecond = ComputeDamagePerSecond(em.currentSeverity);
            OnEmergencyEscalated?.Invoke(em, prev);
        }

        private static float ComputeDamagePerSecond(EmergencySeverity severity)
        {
            return severity switch
            {
                EmergencySeverity.Caution    => 0f,
                EmergencySeverity.Warning    => 0.5f,
                EmergencySeverity.Emergency  => 1.5f,
                EmergencySeverity.Mayday     => 3f,
                _                            => 0f
            };
        }

        private static float ComputeScore(ActiveEmergency em, bool success, float dist, float speed)
        {
            if (!success) return 0f;

            float checklist = em.totalChecklistSteps > 0
                ? 40f * em.checklistProgress / em.totalChecklistSteps
                : 40f;
            float distress = em.distressCallMade ? 20f : 0f;
            float landing  = dist < 200f ? 20f : Mathf.Max(0f, 20f - (dist - 200f) / 50f);
            float speed_s  = speed < 60f ? 20f : Mathf.Max(0f, 20f - (speed - 60f) / 5f);
            return Mathf.Clamp(checklist + distress + landing + speed_s, 0f, 100f);
        }

        #endregion

        #region Cross-System Listeners

        private void OnBirdStrikeReceived(SWEF.Wildlife.WildlifeSpecies species, Vector3 position)
        {
            TriggerEmergency("bird_strike", position, position.y);
        }

        private void OnFuelDepletedReceived()
        {
            var pos = transform.position;
            TriggerEmergency("fuel_starvation", pos, pos.y);
        }

        private void OnCriticalDamageReceived(string partId)
        {
            var pos = transform.position;
            TriggerEmergency("structural_damage", pos, pos.y);
        }

        #endregion

        #region Random Emergency Coroutine

        private IEnumerator RandomEmergencyLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config.randomEmergencyInterval);
                if (UnityEngine.Random.value < config.randomEmergencyChance &&
                    _activeEmergencies.Count < config.maxSimultaneousEmergencies)
                {
                    int idx = UnityEngine.Random.Range(0, scenarioDatabase.Count);
                    var s = scenarioDatabase[idx];
                    var pos = transform.position;
                    TriggerEmergency(s.scenarioId, pos, pos.y);
                }
            }
        }

        #endregion
    }
}
