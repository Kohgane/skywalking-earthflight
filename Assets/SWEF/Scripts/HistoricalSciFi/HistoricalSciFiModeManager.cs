// HistoricalSciFiModeManager.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>
    /// Phase 106 — Central coordinator for the Historical &amp; Sci-Fi Flight Mode system.
    ///
    /// <para>Provides a single façade over:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="HistoricalAircraftRegistry"/> — aircraft catalogue and unlock state.</description></item>
    ///   <item><description><see cref="SciFiEnvironmentController"/> — environment catalogue and transitions.</description></item>
    ///   <item><description><see cref="SpecialMissionManager"/> — mission catalogue and lifecycle.</description></item>
    /// </list>
    ///
    /// <para>Attach to a persistent scene <see cref="GameObject"/> — uses
    /// <see cref="DontDestroyOnLoad"/> to survive scene transitions.</para>
    /// </summary>
    public sealed class HistoricalSciFiModeManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static HistoricalSciFiModeManager Instance { get; private set; }

        #endregion

        #region Inspector Fields

        [Header("Mode Settings")]
        [Tooltip("When true the Historical & Sci-Fi mode starts active on Awake.")]
        [SerializeField] private bool _activateOnAwake = false;

        #endregion

        #region Sub-System References

        /// <summary>Aircraft registry — read-only access for external systems.</summary>
        public HistoricalAircraftRegistry Registry => HistoricalAircraftRegistry.Instance;

        /// <summary>Environment controller — read-only access for external systems.</summary>
        public SciFiEnvironmentController EnvironmentController => SciFiEnvironmentController.Instance;

        /// <summary>Mission manager — read-only access for external systems.</summary>
        public SpecialMissionManager MissionManager => SpecialMissionManager.Instance;

        #endregion

        #region Runtime State

        private bool _modeActive;
        private HistoricalAircraftData _selectedAircraft;

        #endregion

        #region Events

        /// <summary>Raised when the Historical &amp; Sci-Fi mode is activated.</summary>
        public event Action OnModeActivated;

        /// <summary>Raised when the Historical &amp; Sci-Fi mode is deactivated.</summary>
        public event Action OnModeDeactivated;

        /// <summary>Raised when the player selects a different aircraft.</summary>
        public event Action<HistoricalAircraftData> OnAircraftSelected;

        #endregion

        #region Properties

        /// <summary>Whether the Historical &amp; Sci-Fi mode is currently active.</summary>
        public bool IsModeActive => _modeActive;

        /// <summary>The aircraft currently selected by the player (may be <c>null</c>).</summary>
        public HistoricalAircraftData SelectedAircraft => _selectedAircraft;

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

            if (_activateOnAwake)
                ActivateMode();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Public API — Mode

        /// <summary>
        /// Activates the Historical &amp; Sci-Fi Flight Mode.
        /// Unlocks default aircraft and available missions.
        /// </summary>
        public void ActivateMode()
        {
            if (_modeActive) return;

            _modeActive = true;
            InitialiseUnlocks();
            OnModeActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates the Historical &amp; Sci-Fi Flight Mode, restoring the standard
        /// Earth environment and clearing the selected aircraft.
        /// </summary>
        public void DeactivateMode()
        {
            if (!_modeActive) return;

            _modeActive = false;
            _selectedAircraft = null;

            // Return to Earth environment.
            try { EnvironmentController.TransitionTo(SciFiEnvironmentController.IdEarth); }
            catch { /* already on Earth or unknown error — ignore */ }

            OnModeDeactivated?.Invoke();
        }

        #endregion

        #region Public API — Aircraft

        /// <summary>
        /// Selects the aircraft with the given ID as the active aircraft.
        /// Automatically unlocks missions that require this aircraft.
        /// </summary>
        /// <param name="aircraftId">ID of the aircraft to select.</param>
        /// <returns>The selected <see cref="HistoricalAircraftData"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the mode is inactive, the aircraft ID is unknown, or the
        /// aircraft is not yet unlocked.
        /// </exception>
        public HistoricalAircraftData SelectAircraft(string aircraftId)
        {
            if (!_modeActive)
                throw new InvalidOperationException("Historical & Sci-Fi mode is not active.");

            var data = Registry.GetById(aircraftId);
            if (data == null)
                throw new InvalidOperationException($"Unknown aircraft ID: '{aircraftId}'");

            if (!Registry.IsUnlocked(aircraftId))
                throw new InvalidOperationException($"Aircraft '{aircraftId}' is not yet unlocked.");

            _selectedAircraft = data;
            MissionManager.UnlockMissionsForAircraft(aircraftId);
            OnAircraftSelected?.Invoke(_selectedAircraft);
            return _selectedAircraft;
        }

        /// <summary>
        /// Unlocks and immediately selects the aircraft with the given ID.
        /// Useful for testing or cheat-unlock scenarios.
        /// </summary>
        public HistoricalAircraftData UnlockAndSelectAircraft(string aircraftId)
        {
            Registry.Unlock(aircraftId);
            return SelectAircraft(aircraftId);
        }

        #endregion

        #region Public API — Environment

        /// <summary>
        /// Transitions the simulation to the environment with the given ID.
        /// </summary>
        /// <param name="environmentId">ID of the target environment.</param>
        /// <returns><c>true</c> if the environment changed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the mode is inactive.</exception>
        public bool SwitchEnvironment(string environmentId)
        {
            if (!_modeActive)
                throw new InvalidOperationException("Historical & Sci-Fi mode is not active.");

            return EnvironmentController.TransitionTo(environmentId);
        }

        #endregion

        #region Private Helpers

        private void InitialiseUnlocks()
        {
            // Ensure all "unlocked by default" aircraft are available.
            foreach (var aircraft in Registry.All)
            {
                if (aircraft.unlockedByDefault)
                    Registry.Unlock(aircraft.id);
            }

            // Unlock missions for all currently-unlocked aircraft.
            foreach (var aircraft in Registry.GetUnlocked())
                MissionManager.UnlockMissionsForAircraft(aircraft.id);
        }

        #endregion
    }
}
