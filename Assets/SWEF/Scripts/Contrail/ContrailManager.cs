// ContrailManager.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Singleton central manager for the Contrail &amp; Exhaust Trail system.
    ///
    /// <para>Maintains references to all <see cref="ContrailEmitter"/> instances,
    /// the <see cref="ExhaustEffect"/>, and the <see cref="WingTipVortex"/>
    /// controller.  Each update interval it samples current atmospheric conditions
    /// (via <see cref="ContrailConditions"/>) and distributes the results to all
    /// registered subsystems.</para>
    ///
    /// <para>Place one instance in the scene.  Other components call
    /// <see cref="Instance"/> to access it or let <see cref="ContrailEmitter"/>
    /// register / unregister itself automatically.</para>
    /// </summary>
    [AddComponentMenu("SWEF/Contrail/Contrail Manager")]
    public class ContrailManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Singleton instance. Null if no manager is present in the current scene.</summary>
        public static ContrailManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Atmospheric Conditions")]
        [Tooltip("ScriptableObject that defines the altitude / temperature / humidity model for contrail formation.")]
        /// <summary>ScriptableObject that models atmospheric contrail conditions.</summary>
        public ContrailConditions conditions;

        [Header("Subsystems")]
        [Tooltip("Engine exhaust visual controller. May be null if not used.")]
        /// <summary>Engine exhaust visual controller. Optional.</summary>
        public ExhaustEffect exhaustEffect;

        [Tooltip("Wingtip vortex trail controller. May be null if not used.")]
        /// <summary>Wingtip vortex trail controller. Optional.</summary>
        public WingTipVortex wingTipVortex;

        [Header("Master Toggles")]
        [Tooltip("Globally enables or disables condensation contrail emitters.")]
        /// <summary>Master toggle for condensation contrail emitters.</summary>
        public bool contrailsEnabled = true;

        [Tooltip("Globally enables or disables engine exhaust effects.")]
        /// <summary>Master toggle for engine exhaust effects.</summary>
        public bool exhaustEnabled = true;

        [Tooltip("Globally enables or disables wingtip vortex trails.")]
        /// <summary>Master toggle for wingtip vortex trails.</summary>
        public bool vorticesEnabled = true;

        [Header("Quality")]
        [Tooltip("Global trail quality / performance preset applied to all emitters and particle counts.")]
        /// <summary>Global trail quality preset.</summary>
        public TrailIntensity globalIntensity = TrailIntensity.Medium;

        [Header("Update Interval")]
        [Tooltip("Seconds between atmospheric condition recalculations. Lower = more responsive but higher CPU cost.")]
        /// <summary>Seconds between atmospheric condition recalculations.</summary>
        [Min(0.016f)]
        public float updateInterval = 0.1f;

        #endregion

        #region Events

        /// <summary>Raised whenever <see cref="SetGlobalIntensity"/> changes the global intensity level.</summary>
        public event Action<TrailIntensity> OnIntensityChanged;

        #endregion

        #region Private State

        private readonly List<ContrailEmitter> _emitters = new List<ContrailEmitter>();
        private Coroutine _updateCoroutine;

        // Cached flight state — updated externally via SetFlightState.
        private float _currentAltitude;
        private float _currentSpeed;
        private float _currentThrottle;
        private float _currentGForce;
        private float _currentBankAngle;
        private bool  _afterburnerActive;

        #endregion

        #region Public Read-only Access

        /// <summary>Read-only view of all registered <see cref="ContrailEmitter"/> instances.</summary>
        public IReadOnlyList<ContrailEmitter> Emitters => _emitters;

        /// <summary>Exposes the <see cref="WingTipVortex"/> reference for use by <see cref="TrailPersistence"/>.</summary>
        public WingTipVortex WingTipVortex => wingTipVortex;

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
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            _updateCoroutine = StartCoroutine(UpdateLoop());
        }

        #endregion

        #region Public API — Flight State

        /// <summary>
        /// Updates the manager's cached flight state used on the next update tick.
        /// Call this every frame from your flight controller or physics integration.
        /// </summary>
        /// <param name="altitude">Aircraft altitude in metres.</param>
        /// <param name="speed">Aircraft speed in m/s.</param>
        /// <param name="throttle">Normalised throttle in [0, 1].</param>
        /// <param name="gForce">Current G-force magnitude.</param>
        /// <param name="bankAngle">Bank angle in degrees (positive = right-wing down).</param>
        /// <param name="afterburner"><c>true</c> if the afterburner is currently engaged.</param>
        public void SetFlightState(float altitude, float speed, float throttle, float gForce, float bankAngle, bool afterburner = false)
        {
            _currentAltitude    = altitude;
            _currentSpeed       = speed;
            _currentThrottle    = throttle;
            _currentGForce      = gForce;
            _currentBankAngle   = bankAngle;
            _afterburnerActive  = afterburner;
        }

        #endregion

        #region Public API — Emitter Registration

        /// <summary>Registers a <see cref="ContrailEmitter"/> so it receives condition updates.</summary>
        /// <param name="emitter">Emitter to register.</param>
        public void RegisterEmitter(ContrailEmitter emitter)
        {
            if (emitter != null && !_emitters.Contains(emitter))
                _emitters.Add(emitter);
        }

        /// <summary>Removes a <see cref="ContrailEmitter"/> from the update list.</summary>
        /// <param name="emitter">Emitter to unregister.</param>
        public void UnregisterEmitter(ContrailEmitter emitter)
        {
            _emitters.Remove(emitter);
        }

        #endregion

        #region Public API — Toggles &amp; Quality

        /// <summary>
        /// Sets the global quality / performance preset, scales particle counts on all
        /// managed emitters, and raises <see cref="OnIntensityChanged"/>.
        /// </summary>
        /// <param name="intensity">New global intensity level.</param>
        public void SetGlobalIntensity(TrailIntensity intensity)
        {
            if (globalIntensity == intensity) return;

            globalIntensity = intensity;
            float multiplier = ContrailConfig.GetParticleMultiplier(intensity);

            foreach (ContrailEmitter emitter in _emitters)
            {
                if (emitter?.trailParticles == null) continue;

                var main = emitter.trailParticles.main;
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(1000 * multiplier));
            }

            OnIntensityChanged?.Invoke(intensity);
        }

        /// <summary>Stops all active trails immediately (both emitters and vortices).</summary>
        public void DisableAllTrails()
        {
            contrailsEnabled = false;
            exhaustEnabled   = false;
            vorticesEnabled  = false;

            foreach (ContrailEmitter emitter in _emitters)
                emitter?.StopEmitting();

            exhaustEffect?.gameObject.SetActive(false);
            wingTipVortex?.gameObject.SetActive(false);
        }

        /// <summary>Re-enables all trail subsystems that were stopped via <see cref="DisableAllTrails"/>.</summary>
        public void EnableAllTrails()
        {
            contrailsEnabled = true;
            exhaustEnabled   = true;
            vorticesEnabled  = true;

            if (exhaustEffect != null)
                exhaustEffect.gameObject.SetActive(true);

            if (wingTipVortex != null)
                wingTipVortex.gameObject.SetActive(true);
        }

        #endregion

        #region Update Loop

        private IEnumerator UpdateLoop()
        {
            var wait = new WaitForSeconds(updateInterval);

            while (true)
            {
                TickUpdate();
                yield return wait;
            }
        }

        private void TickUpdate()
        {
            float temperature;
            float humidity;

            if (conditions != null)
            {
                temperature = conditions.GetTemperatureAtAltitude(_currentAltitude);
                humidity    = conditions.GetHumidityAtAltitude(_currentAltitude);
            }
            else
            {
                // Fallback: use config constants as a rough model.
                float t = Mathf.InverseLerp(0f, ContrailConfig.MaxContrailAltitude, _currentAltitude);
                temperature = Mathf.Lerp(15f, -56f, t);
                humidity    = Mathf.Lerp(0.8f, 0.3f, t);
            }

            // ── Contrail Emitters ────────────────────────────────────────────
            if (contrailsEnabled && globalIntensity != TrailIntensity.None)
            {
                foreach (ContrailEmitter emitter in _emitters)
                {
                    if (emitter == null) continue;
                    emitter.UpdateEmission(_currentSpeed, _currentThrottle, _currentAltitude, temperature);
                }
            }
            else
            {
                foreach (ContrailEmitter emitter in _emitters)
                    emitter?.StopEmitting();
            }

            // ── Exhaust Effect ────────────────────────────────────────────────
            if (exhaustEnabled && exhaustEffect != null)
                exhaustEffect.UpdateExhaust(_currentThrottle, _afterburnerActive, _currentAltitude);

            // ── Wingtip Vortices ──────────────────────────────────────────────
            if (vorticesEnabled && wingTipVortex != null)
                wingTipVortex.UpdateVortices(_currentGForce, _currentSpeed, humidity, _currentBankAngle);
        }

        #endregion
    }
}
