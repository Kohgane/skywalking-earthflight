// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/AICoPilotManager.cs
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Defines how actively the AI co-pilot intervenes during flight.
    /// </summary>
    public enum AssistanceLevel
    {
        /// <summary>AI co-pilot is disabled. No advisories or assistance.</summary>
        Off,

        /// <summary>Informational only — the AI observes and reports but never acts.</summary>
        Passive,

        /// <summary>The AI provides suggestions and asks before taking any automated action.</summary>
        Active,

        /// <summary>The AI may take automated control actions when safety requires it.</summary>
        Full
    }

    /// <summary>
    /// Core singleton manager for the AI Co-Pilot system (ARIA — Aerial Intelligence &amp; Routing Assistant).
    /// Monitors flight state continuously, coordinates sub-advisors, and dispatches advisories to the
    /// <see cref="AICoPilotDialogueManager"/>.
    /// </summary>
    /// <remarks>
    /// Attach to a persistent root GameObject. The manager survives scene loads via
    /// <c>DontDestroyOnLoad</c> and coordinates <see cref="FlightAdvisor"/>,
    /// <see cref="NavigationAssistant"/>, and <see cref="EmergencyAdvisor"/>.
    /// </remarks>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public class AICoPilotManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AICoPilotManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseSubAdvisors();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever the AI delivers an advisory message.
        /// </summary>
        /// <param name="category">Advisory category (e.g. "Navigation", "Emergency", "Flight").</param>
        /// <param name="message">Human-readable advisory text.</param>
        public event Action<string, string> OnAdviceGiven;

        /// <summary>
        /// Fired when the active <see cref="AssistanceLevel"/> changes.
        /// </summary>
        public event Action<AssistanceLevel> OnAssistanceLevelChanged;

        #endregion

        #region Inspector

        [Header("Assistance")]
        [Tooltip("Starting assistance level. Can be changed at runtime.")]
        [SerializeField] private AssistanceLevel _assistanceLevel = AssistanceLevel.Active;

        [Header("Monitoring")]
        [Tooltip("Seconds between each flight-state monitoring cycle.")]
        [SerializeField] private float _monitorInterval = 2f;

        [Header("Thresholds")]
        [Tooltip("Altitude (metres AGL) below which a low-altitude advisory is triggered.")]
        [SerializeField] private float _lowAltitudeThresholdM = 300f;

        [Tooltip("Airspeed (m/s) below which a stall-risk advisory is triggered.")]
        [SerializeField] private float _stallSpeedThresholdMs = 40f;

        [Tooltip("Fuel percentage below which a low-fuel advisory is triggered.")]
        [SerializeField][Range(0f, 1f)] private float _lowFuelThreshold = 0.15f;

        #endregion

        #region Private State

        private FlightAdvisor _flightAdvisor;
        private NavigationAssistant _navigationAssistant;
        private EmergencyAdvisor _emergencyAdvisor;
        private AICoPilotSettings _settings;

        private Coroutine _monitorCoroutine;

        #endregion

        #region Properties

        /// <summary>Current assistance level controlling how actively ARIA intervenes.</summary>
        public AssistanceLevel CurrentAssistanceLevel => _assistanceLevel;

        #endregion

        #region Initialisation

        private void Start()
        {
            _settings = AICoPilotSettings.Instance;
            if (_settings != null)
            {
                _assistanceLevel = _settings.AssistanceLevel;
            }

            SubscribeToSubAdvisors();
            _monitorCoroutine = StartCoroutine(MonitorFlightLoop());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AICoPilotManager] ARIA initialised.");
#endif
        }

        private void InitialiseSubAdvisors()
        {
            _flightAdvisor = GetComponentInChildren<FlightAdvisor>();
            _navigationAssistant = GetComponentInChildren<NavigationAssistant>();
            _emergencyAdvisor = GetComponentInChildren<EmergencyAdvisor>();
        }

        private void SubscribeToSubAdvisors()
        {
            if (_flightAdvisor != null)
                _flightAdvisor.OnFlightAdvisory += HandleFlightAdvisory;

            if (_navigationAssistant != null)
                _navigationAssistant.OnNavigationCallout += HandleNavigationCallout;

            if (_emergencyAdvisor != null)
            {
                _emergencyAdvisor.OnEmergencyDetected += HandleEmergencyDetected;
                _emergencyAdvisor.OnEmergencyResolved += HandleEmergencyResolved;
            }
        }

        private void OnDisable()
        {
            if (_flightAdvisor != null)
                _flightAdvisor.OnFlightAdvisory -= HandleFlightAdvisory;

            if (_navigationAssistant != null)
                _navigationAssistant.OnNavigationCallout -= HandleNavigationCallout;

            if (_emergencyAdvisor != null)
            {
                _emergencyAdvisor.OnEmergencyDetected -= HandleEmergencyDetected;
                _emergencyAdvisor.OnEmergencyResolved -= HandleEmergencyResolved;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Changes the AI assistance level at runtime and fires <see cref="OnAssistanceLevelChanged"/>.
        /// </summary>
        /// <param name="level">New assistance level.</param>
        public void SetAssistanceLevel(AssistanceLevel level)
        {
            if (_assistanceLevel == level) return;
            _assistanceLevel = level;

            if (_settings != null)
            {
                _settings.AssistanceLevel = level;
            }

            OnAssistanceLevelChanged?.Invoke(_assistanceLevel);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AICoPilotManager] Assistance level → {_assistanceLevel}");
#endif
        }

        /// <summary>
        /// Dispatches an advisory message from any category.
        /// Respects the current <see cref="AssistanceLevel"/> — if set to <see cref="AssistanceLevel.Off"/>
        /// the advisory is suppressed.
        /// </summary>
        /// <param name="category">Source category label.</param>
        /// <param name="message">Advisory text.</param>
        public void DispatchAdvisory(string category, string message)
        {
            if (_assistanceLevel == AssistanceLevel.Off) return;
            if (string.IsNullOrEmpty(message)) return;

            OnAdviceGiven?.Invoke(category, message);

            var dialogue = AICoPilotDialogueManager.Instance;
            if (dialogue != null)
                dialogue.EnqueueMessage(category, message);
        }

        #endregion

        #region Flight Monitoring Loop

        private IEnumerator MonitorFlightLoop()
        {
            var wait = new WaitForSeconds(_monitorInterval);
            while (true)
            {
                yield return wait;
                if (_assistanceLevel == AssistanceLevel.Off) continue;
                RunFlightChecks();
            }
        }

        private void RunFlightChecks()
        {
            // Delegate detailed checks to FlightAdvisor; this loop handles cross-cutting concerns.
            // Sub-advisors fire their own events when thresholds are breached.
            _flightAdvisor?.EvaluateFlightState();
            _navigationAssistant?.EvaluateNavigation();
            _emergencyAdvisor?.EvaluateEmergencies();
        }

        #endregion

        #region Sub-Advisor Handlers

        private void HandleFlightAdvisory(AdvisoryLevel level, string message)
        {
            if (_assistanceLevel == AssistanceLevel.Off) return;
            DispatchAdvisory("Flight", message);
        }

        private void HandleNavigationCallout(string message)
        {
            if (_assistanceLevel == AssistanceLevel.Off) return;
            DispatchAdvisory("Navigation", message);
        }

        private void HandleEmergencyDetected(EmergencyType type, string procedure)
        {
            // Emergencies always bypass the Off level — safety first.
            DispatchAdvisory("Emergency", procedure);
        }

        private void HandleEmergencyResolved(EmergencyType type)
        {
            DispatchAdvisory("Emergency", $"Emergency resolved: {type}. Resuming normal operations.");
        }

        #endregion
    }
}
