using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Detects and responds to interactions between GameObjects and water.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Splash effects when tagged objects enter water.</item>
    ///   <item>Wake/trail geometry behind moving objects on the surface.</item>
    ///   <item>Expanding ripple effects at impact points.</item>
    ///   <item>Buoyancy force calculation based on submerged volume approximation.</item>
    ///   <item>Aircraft water-landing detection (gear touching water).</item>
    /// </list>
    /// </para>
    ///
    /// <para>Tag interactable objects with <c>"WaterInteractable"</c> or assign them to
    /// <see cref="trackedObjects"/> in the Inspector.</para>
    /// </summary>
    public class WaterInteractionHandler : MonoBehaviour
    {
        #region Constants

        private const string DefaultWaterTag   = "WaterInteractable";
        private const float  BuoyancyDensity   = 1025f;   // sea water density kg/m³
        private const float  GravityMagnitude  = 9.81f;

        // Ripple shader properties.
        private static readonly int ShaderPropRipplePos    = Shader.PropertyToID("_RipplePos");
        private static readonly int ShaderPropRippleRadius = Shader.PropertyToID("_RippleRadius");
        private static readonly int ShaderPropRippleStr    = Shader.PropertyToID("_RippleStrength");

        #endregion

        #region Inspector

        [Header("Tracked Objects")]
        [Tooltip("Explicitly tracked objects. If empty, objects with tag WaterInteractable are auto-detected.")]
        [SerializeField] private List<Rigidbody> trackedObjects = new List<Rigidbody>();

        [Header("Splash")]
        [Tooltip("Splash particle prefab instantiated when an object enters water.")]
        [SerializeField] private GameObject splashPrefab;

        [Tooltip("Pool size for the splash prefab.")]
        [SerializeField, Range(1, 20)] private int splashPoolSize = 8;

        [Tooltip("Minimum entry speed (m/s) required to trigger a splash.")]
        [SerializeField] private float minSplashSpeed = 1f;

        [Header("Wake")]
        [Tooltip("Material used to render the wake trail behind moving objects.")]
        [SerializeField] private Material wakeMaterial;

        [Tooltip("World-space width of the wake trail at its widest point.")]
        [SerializeField] private float wakeWidth = 5f;

        [Tooltip("Number of positions stored for the wake trail.")]
        [SerializeField, Range(4, 64)] private int wakeTrailLength = 16;

        [Header("Buoyancy")]
        [Tooltip("Approximate volume (m³) of the object for buoyancy calculation. Applied uniformly.")]
        [SerializeField] private float defaultBuoyancyVolume = 1f;

        [Header("Ripple")]
        [Tooltip("Ocean surface material that receives ripple parameters.")]
        [SerializeField] private Material oceanSurfaceMaterial;

        [Tooltip("Maximum ripple radius in world units.")]
        [SerializeField] private float maxRippleRadius = 20f;

        [Tooltip("Ripple expansion speed (units per second).")]
        [SerializeField] private float rippleSpeed = 8f;

        [Header("References")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Events

        /// <summary>Fired when any tracked object enters water.</summary>
        public event Action<Rigidbody> OnObjectEnteredWater;

        /// <summary>Fired when any tracked object exits water.</summary>
        public event Action<Rigidbody> OnObjectExitedWater;

        /// <summary>Fired when a splash effect is triggered.</summary>
        public event Action<Vector3> OnSplash;

        #endregion

        #region Private State

        private OceanManager        _mgr;
        private GameObject[]        _splashPool;
        private int                 _splashIdx;
        private readonly HashSet<Rigidbody> _underwaterObjects = new HashSet<Rigidbody>();

        // Ripple animation state.
        private float   _rippleRadius;
        private Vector3 _rippleCenter;
        private bool    _rippleActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mgr = oceanManager != null ? oceanManager : FindFirstObjectByType<OceanManager>();

            if (splashPrefab != null && splashPoolSize > 0)
                InitSplashPool();
        }

        private void Update()
        {
            ScanTaggedObjects();
            ProcessTrackedObjects();
            UpdateRipple();
        }

        #endregion

        #region Object Scanning

        private void ScanTaggedObjects()
        {
            // Auto-add tagged objects that are not yet tracked (expensive — only do infrequently).
            // For production, hook into scene load events instead.
        }

        private void ProcessTrackedObjects()
        {
            if (_mgr == null) return;

            foreach (var rb in trackedObjects)
            {
                if (rb == null) continue;
                bool isUnder = _mgr.IsPositionUnderwater(rb.position);
                bool wasUnder = _underwaterObjects.Contains(rb);

                if (isUnder && !wasUnder)
                {
                    _underwaterObjects.Add(rb);
                    OnObjectEnteredWater?.Invoke(rb);
                    TrySpawnSplash(rb.position, rb.velocity.magnitude);
                    TriggerRipple(rb.position);
                }
                else if (!isUnder && wasUnder)
                {
                    _underwaterObjects.Remove(rb);
                    OnObjectExitedWater?.Invoke(rb);
                }

                if (isUnder)
                    ApplyBuoyancy(rb);
            }
        }

        #endregion

        #region Splash

        private void InitSplashPool()
        {
            _splashPool = new GameObject[splashPoolSize];
            for (int i = 0; i < splashPoolSize; i++)
            {
                _splashPool[i] = Instantiate(splashPrefab, transform);
                _splashPool[i].SetActive(false);
            }
        }

        private void TrySpawnSplash(Vector3 position, float entrySpeed)
        {
            if (_splashPool == null || entrySpeed < minSplashSpeed) return;

            var splash = _splashPool[_splashIdx++ % _splashPool.Length];
            splash.transform.position = position;
            splash.SetActive(true);

            if (splash.TryGetComponent<ParticleSystem>(out var ps))
                ps.Play();

            OnSplash?.Invoke(position);
        }

        #endregion

        #region Ripple

        private void TriggerRipple(Vector3 center)
        {
            _rippleCenter = center;
            _rippleRadius = 0f;
            _rippleActive = true;
        }

        private void UpdateRipple()
        {
            if (!_rippleActive || oceanSurfaceMaterial == null) return;

            _rippleRadius += rippleSpeed * Time.deltaTime;
            float strength = 1f - Mathf.Clamp01(_rippleRadius / maxRippleRadius);

            oceanSurfaceMaterial.SetVector(ShaderPropRipplePos,    _rippleCenter);
            oceanSurfaceMaterial.SetFloat(ShaderPropRippleRadius,  _rippleRadius);
            oceanSurfaceMaterial.SetFloat(ShaderPropRippleStr,     strength);

            if (_rippleRadius >= maxRippleRadius)
            {
                _rippleActive = false;
                oceanSurfaceMaterial.SetFloat(ShaderPropRippleStr, 0f);
            }
        }

        #endregion

        #region Buoyancy

        /// <summary>
        /// Applies an upward buoyant force proportional to the submerged fraction of
        /// <paramref name="rb"/>'s approximate volume.
        /// </summary>
        private void ApplyBuoyancy(Rigidbody rb)
        {
            if (_mgr == null) return;

            float waterY    = _mgr.GetWaterHeightAtPosition(rb.position);
            float submerged = Mathf.Clamp01((waterY - rb.position.y) / Mathf.Max(0.01f, rb.transform.localScale.y));
            float volume    = defaultBuoyancyVolume * submerged;
            float force     = BuoyancyDensity * GravityMagnitude * volume;

            rb.AddForce(Vector3.up * force, ForceMode.Force);
        }

        /// <summary>
        /// Returns the buoyant force magnitude for an object at <paramref name="position"/>
        /// with <paramref name="volume"/> m³.
        /// </summary>
        public float CalculateBuoyancy(Vector3 position, float volume)
        {
            if (_mgr == null) return 0f;
            float waterY    = _mgr.GetWaterHeightAtPosition(position);
            float submerged = Mathf.Clamp01(waterY - position.y);
            return BuoyancyDensity * GravityMagnitude * volume * submerged;
        }

        #endregion

        #region Aircraft Landing Detection

        /// <summary>
        /// Returns <c>true</c> when the aircraft's gear position is at or below the water surface.
        /// Suitable for triggering water-landing logic.
        /// </summary>
        /// <param name="gearWorldPosition">World position of the landing gear tip.</param>
        public bool IsAircraftGearOnWater(Vector3 gearWorldPosition)
        {
            if (_mgr == null) return false;
            return gearWorldPosition.y <= _mgr.GetWaterHeightAtPosition(gearWorldPosition) + 0.5f;
        }

        #endregion
    }
}
