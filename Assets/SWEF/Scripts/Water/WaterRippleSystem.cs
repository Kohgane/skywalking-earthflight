// WaterRippleSystem.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>Pooled data for a single active ripple ring.</summary>
    [Serializable]
    internal class RippleInstance
    {
        public Vector3 position;
        public float radius;
        public float intensity;
        public float lifetime;
        public float elapsed;
        public float expansionSpeed;
        public bool active;
    }

    /// <summary>
    /// Phase 74 — Generates expanding surface ripple rings from aircraft proximity
    /// and water contact events.
    ///
    /// <para>Ripple sources:</para>
    /// <list type="bullet">
    ///   <item>Low-altitude flyover (&lt; skim threshold): circular ripple ring.</item>
    ///   <item>Water contact (splash): expanding ring from impact point.</item>
    ///   <item>Floating aircraft: continuous small ripples.</item>
    ///   <item>Engine wash: directional cone behind aircraft (future).</item>
    /// </list>
    ///
    /// <para>Rendering is projector/decal-based or shader normal-displacement;
    /// representation uses <see cref="LineRenderer"/> rings in this implementation.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class WaterRippleSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Quality")]
        [Tooltip("Maximum simultaneous active ripple instances (performance cap).")]
        [SerializeField] private int maxActiveRipples = 20;

        [Header("Flyover Ripples")]
        [Tooltip("Expansion speed (m/s) for flyover-triggered ripples.")]
        [SerializeField] private float flyoverExpansionSpeed = 8f;
        [Tooltip("Initial intensity of flyover ripples.")]
        [SerializeField] private float flyoverIntensity = 0.7f;
        [Tooltip("Interval (s) between flyover ripple spawns.")]
        [SerializeField] private float flyoverSpawnInterval = 0.5f;

        [Header("Contact Ripples")]
        [Tooltip("Expansion speed (m/s) for impact-triggered ripples.")]
        [SerializeField] private float contactExpansionSpeed = 12f;
        [Tooltip("Initial intensity of contact ripples.")]
        [SerializeField] private float contactIntensity = 1.0f;

        [Header("Floating Ripples")]
        [Tooltip("Interval (s) between small floating ripples.")]
        [SerializeField] private float floatRippleInterval = 1.5f;
        [Tooltip("Expansion speed (m/s) for floating ripples.")]
        [SerializeField] private float floatExpansionSpeed = 3f;
        [Tooltip("Initial intensity of floating ripples.")]
        [SerializeField] private float floatIntensity = 0.3f;

        [Header("Rendering")]
        [Tooltip("Prefab with a LineRenderer used to render each ripple ring. Leave null to skip rendering.")]
        [SerializeField] private LineRenderer rippleRingPrefab;
        [Tooltip("Number of line segments per ripple ring.")]
        [SerializeField] private int ringSegments = 32;
        [Tooltip("Maximum camera distance (m) at which ripples are rendered.")]
        [SerializeField] private float maxRenderDistance = 500f;

        #endregion

        #region Public Properties

        /// <summary>Current number of active (non-expired) ripple instances.</summary>
        public int ActiveRippleCount
        {
            get
            {
                int count = 0;
                foreach (var r in _pool) if (r.active) count++;
                return count;
            }
        }

        #endregion

        #region Private State

        private readonly List<RippleInstance> _pool = new List<RippleInstance>();
        private readonly List<LineRenderer>   _rings = new List<LineRenderer>();

        private BuoyancyController _buoyancy;
        private WaterConfig _config;
        private float _flyoverTimer;
        private float _floatTimer;
        private Camera _cam;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _buoyancy = GetComponent<BuoyancyController>();
        }

        private void Start()
        {
            _config = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.Config
                : new WaterConfig();

            _cam = Camera.main;

            // Pre-allocate pool
            for (int i = 0; i < maxActiveRipples; i++)
            {
                _pool.Add(new RippleInstance());
                if (rippleRingPrefab != null)
                {
                    var lr = Instantiate(rippleRingPrefab, transform);
                    lr.positionCount = ringSegments + 1;
                    lr.enabled = false;
                    _rings.Add(lr);
                }
                else
                {
                    _rings.Add(null);
                }
            }

            if (_buoyancy != null)
            {
                _buoyancy.OnWaterContact += OnWaterContact;
            }

            if (WaterSurfaceManager.Instance != null)
            {
                WaterSurfaceManager.Instance.OnWaterDetected += OnWaterDetected;
            }
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;

            float dt = Time.deltaTime;
            _flyoverTimer -= dt;
            _floatTimer   -= dt;

            HandleFlyoverRipples();
            HandleFloatingRipples();
            UpdateRipples(dt);
        }

        private void OnDestroy()
        {
            if (_buoyancy != null)
                _buoyancy.OnWaterContact -= OnWaterContact;

            if (WaterSurfaceManager.Instance != null)
                WaterSurfaceManager.Instance.OnWaterDetected -= OnWaterDetected;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns a new ripple at the specified world position.
        /// Oldest active ripple is recycled if the pool is full.
        /// </summary>
        /// <param name="position">World-space spawn position (XZ used; Y snapped to water surface).</param>
        /// <param name="initialIntensity">Starting intensity [0–1].</param>
        /// <param name="expansionSpeed">Outward expansion speed in m/s.</param>
        public void SpawnRipple(Vector3 position, float initialIntensity, float expansionSpeed)
        {
            float lifetime = _config != null ? _config.rippleLifetime : 3f;

            // Find a free slot or recycle the oldest
            int slot = FindFreeSlot();
            RippleInstance r = _pool[slot];
            r.position       = SnapToWater(position);
            r.radius         = 0f;
            r.intensity      = initialIntensity;
            r.lifetime       = lifetime;
            r.elapsed        = 0f;
            r.expansionSpeed = expansionSpeed;
            r.active         = true;
        }

        /// <summary>Immediately deactivates all active ripple instances.</summary>
        public void ClearAllRipples()
        {
            foreach (var r in _pool)
                r.active = false;
            foreach (var lr in _rings)
                if (lr != null) lr.enabled = false;
        }

        #endregion

        #region Private — Ripple Sources

        private void HandleFlyoverRipples()
        {
            if (_buoyancy == null) return;
            WaterContactState state = _buoyancy.State.contactState;
            if (state != WaterContactState.Skimming && state != WaterContactState.Airborne) return;

            float alt = transform.position.y
                - (WaterSurfaceManager.Instance != null
                    ? WaterSurfaceManager.Instance.GetWaterHeight(transform.position)
                    : 0f);

            float threshold = _config != null ? _config.skimAltitudeThreshold : 5f;
            if (alt > threshold || alt < 0f) return;

            if (_flyoverTimer <= 0f)
            {
                _flyoverTimer = flyoverSpawnInterval;
                float intensity = flyoverIntensity * (1f - alt / threshold);
                SpawnRipple(transform.position, intensity, flyoverExpansionSpeed);
            }
        }

        private void HandleFloatingRipples()
        {
            if (_buoyancy == null) return;
            if (_buoyancy.State.contactState != WaterContactState.Floating) return;

            if (_floatTimer <= 0f)
            {
                _floatTimer = floatRippleInterval;
                SpawnRipple(transform.position, floatIntensity, floatExpansionSpeed);
            }
        }

        private void OnWaterContact(SplashEvent evt)
        {
            float intensity = Mathf.Clamp01(contactIntensity * (evt.impactForce / 5000f + 0.3f));
            SpawnRipple(evt.position, intensity, contactExpansionSpeed);
        }

        private void OnWaterDetected(WaterBodyType _)
        {
            // Spawn a subtle ripple when we first detect water below
            SpawnRipple(transform.position, 0.4f, flyoverExpansionSpeed * 0.5f);
        }

        #endregion

        #region Private — Update & Render

        private void UpdateRipples(float dt)
        {
            float maxRadius = _config != null ? _config.rippleMaxRadius : 50f;
            bool tooFar = _cam != null
                && Vector3.Distance(_cam.transform.position, transform.position) > maxRenderDistance;

            for (int i = 0; i < _pool.Count; i++)
            {
                var r = _pool[i];
                if (!r.active) continue;

                r.elapsed += dt;
                r.radius  += r.expansionSpeed * dt;
                r.intensity = Mathf.Clamp01(1f - r.elapsed / r.lifetime);

                if (r.elapsed >= r.lifetime || r.radius >= maxRadius)
                {
                    r.active = false;
                    if (_rings[i] != null) _rings[i].enabled = false;
                    continue;
                }

                if (tooFar) continue;
                RenderRing(i, r);
            }
        }

        private void RenderRing(int index, RippleInstance r)
        {
            if (index >= _rings.Count || _rings[index] == null) return;
            LineRenderer lr = _rings[index];
            lr.enabled = true;

            float alpha = r.intensity;
            lr.startColor = new Color(1f, 1f, 1f, alpha);
            lr.endColor   = new Color(1f, 1f, 1f, 0f);
            lr.startWidth = 0.2f * r.intensity;
            lr.endWidth   = 0.05f;

            for (int s = 0; s <= ringSegments; s++)
            {
                float angle = (s / (float)ringSegments) * Mathf.PI * 2f;
                Vector3 point = r.position + new Vector3(
                    Mathf.Cos(angle) * r.radius,
                    0.01f,
                    Mathf.Sin(angle) * r.radius);
                lr.SetPosition(s, point);
            }
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].active) return i;

            // Pool full — recycle the ripple with the highest elapsed time (oldest)
            int oldestIndex = 0;
            float maxElapsed = -1f;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].elapsed > maxElapsed)
                {
                    maxElapsed  = _pool[i].elapsed;
                    oldestIndex = i;
                }
            }
            return oldestIndex;
        }

        private Vector3 SnapToWater(Vector3 pos)
        {
            float waterY = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.GetWaterHeight(pos)
                : (_config != null ? _config.waterLevel : 0f);
            return new Vector3(pos.x, waterY + 0.01f, pos.z);
        }

        #endregion
    }
}
