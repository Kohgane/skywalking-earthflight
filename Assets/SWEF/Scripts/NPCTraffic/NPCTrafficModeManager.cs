// NPCTrafficModeManager.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Top-level DontDestroyOnLoad façade that owns all NPC Traffic sub-systems.
// Namespace: SWEF.NPCTraffic

using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Persistent scene façade for the NPC Traffic module.
    /// Attach this component to a single scene object marked with
    /// <c>DontDestroyOnLoad</c>; it owns and initialises every NPC Traffic
    /// sub-system, wires inter-component references, and exposes a single
    /// entry point for external modules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCTrafficModeManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NPCTrafficModeManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [Tooltip("NPC Traffic configuration ScriptableObject asset.")]
        [SerializeField] private NPCTrafficConfig _config;

        [Header("Sub-system References (auto-resolved if null)")]
        [SerializeField] private NPCTrafficManager     _trafficManager;
        [SerializeField] private NPCSpawnController    _spawnController;
        [SerializeField] private NPCRouteGenerator     _routeGenerator;
        [SerializeField] private AirportActivityManager _airportManager;
        [SerializeField] private NPCRadioController    _radioController;
        [SerializeField] private NPCFormationController _formationController;
        [SerializeField] private NPCEventBridge        _eventBridge;

        #endregion

        #region Public State

        /// <summary>Whether the NPC Traffic system is currently active.</summary>
        public bool IsActive { get; private set; }

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

            EnsureSubSystems();
        }

        private void Start()
        {
            IsActive = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activates the NPC Traffic system (resumes spawning and updates).
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            if (_trafficManager != null) _trafficManager.enabled = true;
        }

        /// <summary>
        /// Deactivates the NPC Traffic system (pauses spawning and clears NPCs).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            if (_trafficManager != null)
            {
                _trafficManager.ClearAllNPCs();
                _trafficManager.enabled = false;
            }
        }

        /// <summary>
        /// Sets the player transform on all sub-systems that require it.
        /// </summary>
        /// <param name="playerTransform">The player's Transform.</param>
        public void SetPlayerTransform(Transform playerTransform)
        {
            if (_trafficManager  != null) _trafficManager.SetPlayerTransform(playerTransform);
            if (_airportManager  != null) _airportManager.SetPlayerTransform(playerTransform);
        }

        #endregion

        #region Private — Sub-system Bootstrap

        private void EnsureSubSystems()
        {
            _trafficManager   = EnsureComponent<NPCTrafficManager>(_trafficManager);
            _spawnController  = EnsureComponent<NPCSpawnController>(_spawnController);
            _routeGenerator   = EnsureComponent<NPCRouteGenerator>(_routeGenerator);
            _airportManager   = EnsureComponent<AirportActivityManager>(_airportManager);
            _radioController  = EnsureComponent<NPCRadioController>(_radioController);
            _formationController = EnsureComponent<NPCFormationController>(_formationController);
            _eventBridge      = EnsureComponent<NPCEventBridge>(_eventBridge);
        }

        private T EnsureComponent<T>(T existing) where T : MonoBehaviour
        {
            if (existing != null) return existing;
            T found = GetComponentInChildren<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        #endregion
    }
}
