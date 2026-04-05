// SciFiEnvironmentController.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>
    /// Phase 106 — Controller that manages the catalogue of Sci-Fi environments and
    /// handles environment transitions (Earth → Space → Moon/Mars).
    ///
    /// <para>Maintains the active environment state and exposes events so that other
    /// SWEF subsystems (flight physics, VFX, audio) can react to environment changes.</para>
    /// </summary>
    public sealed class SciFiEnvironmentController
    {
        #region Singleton

        private static SciFiEnvironmentController _instance;

        /// <summary>Global singleton instance. Initialised on first access.</summary>
        public static SciFiEnvironmentController Instance =>
            _instance ?? (_instance = new SciFiEnvironmentController());

        #endregion

        #region Well-Known Environment IDs

        /// <summary>Environment ID: Earth (standard).</summary>
        public const string IdEarth = "earth";
        /// <summary>Environment ID: Low-Earth-Orbit / near-Space.</summary>
        public const string IdSpace = "space";
        /// <summary>Environment ID: Lunar surface.</summary>
        public const string IdMoon  = "moon_surface";
        /// <summary>Environment ID: Martian surface.</summary>
        public const string IdMars  = "mars_surface";

        #endregion

        #region Internal State

        private readonly Dictionary<string, SciFiEnvironmentData> _environments =
            new Dictionary<string, SciFiEnvironmentData>(StringComparer.Ordinal);

        private SciFiEnvironmentData _activeEnvironment;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the active environment changes.
        /// The first argument is the previous environment (may be <c>null</c> on init),
        /// the second is the newly active environment.
        /// </summary>
        public event Action<SciFiEnvironmentData, SciFiEnvironmentData> OnEnvironmentChanged;

        #endregion

        #region Constructor

        private SciFiEnvironmentController()
        {
            PopulateBuiltInEnvironments();
        }

        #endregion

        #region Public API

        /// <summary>The currently active <see cref="SciFiEnvironmentData"/>.</summary>
        public SciFiEnvironmentData ActiveEnvironment => _activeEnvironment;

        /// <summary>Returns all registered environments.</summary>
        public IReadOnlyCollection<SciFiEnvironmentData> All => _environments.Values;

        /// <summary>
        /// Looks up an environment by its unique ID.
        /// Returns <c>null</c> if no match is found.
        /// </summary>
        public SciFiEnvironmentData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _environments.TryGetValue(id, out var env);
            return env;
        }

        /// <summary>
        /// Transitions the simulation into the specified environment.
        /// </summary>
        /// <param name="environmentId">ID of the target environment.</param>
        /// <returns><c>true</c> if the transition was successful.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when no environment with the given ID is registered.
        /// </exception>
        public bool TransitionTo(string environmentId)
        {
            if (!_environments.TryGetValue(environmentId, out var target))
                throw new ArgumentException($"Unknown environment ID: '{environmentId}'", nameof(environmentId));

            if (_activeEnvironment == target) return false;

            var previous = _activeEnvironment;
            _activeEnvironment = target;
            OnEnvironmentChanged?.Invoke(previous, _activeEnvironment);
            return true;
        }

        /// <summary>
        /// Returns the effective gravity in m/s² for the active environment.
        /// Defaults to Earth gravity (9.81 m/s²) when no environment is active.
        /// </summary>
        public float GetActiveGravity() =>
            (_activeEnvironment != null ? _activeEnvironment.gravityMultiplier : 1f) * 9.81f;

        /// <summary>
        /// Registers a custom environment data record.  Overwrites any existing record
        /// with the same ID.
        /// </summary>
        public void Register(SciFiEnvironmentData env)
        {
            if (env == null)                throw new ArgumentNullException(nameof(env));
            if (string.IsNullOrEmpty(env.id))
                throw new ArgumentNullException(nameof(env), "Environment ID must not be null or empty.");

            _environments[env.id] = env;
        }

        #endregion

        #region Built-In Environments

        private void PopulateBuiltInEnvironments()
        {
            var envList = new[]
            {
                // ── Earth ─────────────────────────────────────────────────────
                SciFiEnvironmentData.Create(
                    id:                  IdEarth,
                    displayName:         "Earth",
                    celestialBody:       CelestialBody.Earth,
                    surfaceDescription:  "Home. Standard atmosphere and gravity. All historical aircraft are available here.",
                    gravityMultiplier:   1.0f,
                    atmosphereDensity:   1.0f,
                    maxWindSpeedKph:     200f,
                    availableMissionIds: new List<string> { "first_flight", "cross_the_atlantic", "mach3_recon" }),

                // ── Space ─────────────────────────────────────────────────────
                SciFiEnvironmentData.Create(
                    id:                  IdSpace,
                    displayName:         "Low Earth Orbit",
                    celestialBody:       CelestialBody.Space,
                    surfaceDescription:  "The vast silence of near-Earth space. No aerodynamic lift — only thruster control and inertia.",
                    gravityMultiplier:   0.0f,
                    atmosphereDensity:   0.0f,
                    maxWindSpeedKph:     0f,
                    availableMissionIds: new List<string> { "orbital_reentry" }),

                // ── Moon ──────────────────────────────────────────────────────
                SciFiEnvironmentData.Create(
                    id:                  IdMoon,
                    displayName:         "Lunar Surface",
                    celestialBody:       CelestialBody.Moon,
                    surfaceDescription:  "Barren grey regolith stretching to the horizon under an ink-black sky. Gravity is 1/6 of Earth's — no atmosphere.",
                    gravityMultiplier:   0.165f,
                    atmosphereDensity:   0.0f,
                    maxWindSpeedKph:     0f,
                    availableMissionIds: new List<string> { "lunar_survey" }),

                // ── Mars ──────────────────────────────────────────────────────
                SciFiEnvironmentData.Create(
                    id:                  IdMars,
                    displayName:         "Mars Surface",
                    celestialBody:       CelestialBody.Mars,
                    surfaceDescription:  "Red dust devils sweep across ancient rust-coloured plains. CO₂ atmosphere at ~1% Earth density — rotorcraft barely fly.",
                    gravityMultiplier:   0.376f,
                    atmosphereDensity:   0.016f,
                    maxWindSpeedKph:     100f,
                    availableMissionIds: new List<string> { "mars_colony_supply_run" }),
            };

            foreach (var env in envList)
                Register(env);

            // Start on Earth by default.
            _activeEnvironment = _environments[IdEarth];
        }

        #endregion
    }
}
