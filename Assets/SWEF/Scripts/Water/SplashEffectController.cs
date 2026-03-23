// SplashEffectController.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 55 — MonoBehaviour that triggers splash VFX and audio hooks whenever a
    /// tracked object crosses the water surface (either entry or exit).
    ///
    /// <para>Particle instances are managed through a simple object-pool to avoid
    /// runtime allocations.  Audio integration is handled via the
    /// <see cref="OnSplashTriggered"/> event so that any audio backend can subscribe
    /// without coupling this component to a specific audio system.</para>
    ///
    /// <para>Spray trails while skimming the surface are driven by a configurable
    /// <see cref="sprayTrailObject"/> child GameObject that is enabled/disabled based
    /// on skimming state detection.</para>
    ///
    /// <para>Analytics integration: each splash is forwarded to
    /// <see cref="WaterInteractionAnalytics.RecordSplash"/>.</para>
    /// </summary>
    public class SplashEffectController : MonoBehaviour
    {
        #region Inspector

        [Header("Interaction Profile")]
        [Tooltip("WaterInteractionProfile containing SplashEffectConfig values.")]
        [SerializeField] private WaterInteractionProfile profile;

        [Header("Spray Trail")]
        [Tooltip("Optional child GameObject representing the spray trail while skimming. Enabled/disabled at runtime.")]
        [SerializeField] private GameObject sprayTrailObject;

        [Header("Skimming Detection")]
        [Tooltip("Skim is active when the tracked point is within this distance above the water surface (m).")]
        [SerializeField] private float skimHeightThreshold = 0.3f;

        [Tooltip("Minimum horizontal speed (m/s) required for skim trail activation.")]
        [SerializeField] private float skimMinSpeed = 5f;

        [Header("Debug")]
        [Tooltip("Log splash events to the Unity console.")]
        [SerializeField] private bool debugLog;

        #endregion

        #region Events

        /// <summary>
        /// Fired every time a splash (entry or exit) is triggered.
        /// Subscribe to play audio, update UI, or feed analytics.
        /// </summary>
        public event Action<SplashEventData> OnSplashTriggered;

        #endregion

        #region Private State

        private SplashEffectConfig _config;
        private List<ParticleSystem> _pool;
        private bool _wasUnderwater;
        private float _lastSplashTime = -999f;
        private bool _isSkimming;
        private Rigidbody _rb;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            ResolveConfig();
            BuildPool();
        }

        private void Update()
        {
            if (WaterSurfaceManager.Instance == null) return;

            float waterY = WaterSurfaceManager.Instance.GetWaterHeightAt(transform.position);
            bool isUnderwater = transform.position.y < waterY;

            // Entry / exit detection
            if (isUnderwater != _wasUnderwater)
            {
                if (Time.time - _lastSplashTime >= _config.cooldown)
                {
                    TriggerSplash(waterY, isUnderwater);
                    _lastSplashTime = Time.time;
                }
                _wasUnderwater = isUnderwater;
            }

            // Skim detection
            UpdateSkimState(waterY);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually triggers a splash at a specific world position without waiting for
        /// the automatic surface-crossing detection.  Bypasses the cooldown timer.
        /// </summary>
        /// <param name="worldPos">World-space origin of the splash.</param>
        /// <param name="velocityMagnitude">Effective impact speed used to scale the VFX.</param>
        /// <param name="isEntry"><c>true</c> = water entry; <c>false</c> = water exit.</param>
        public void TriggerSplashManual(Vector3 worldPos, float velocityMagnitude, bool isEntry)
        {
            SpawnSplashParticle(worldPos, velocityMagnitude);

            var data = new SplashEventData
            {
                position          = worldPos,
                velocityMagnitude = velocityMagnitude,
                timestamp         = Time.time,
                isEntry           = isEntry
            };

            OnSplashTriggered?.Invoke(data);
            WaterInteractionAnalytics.RecordSplash(data);

            if (debugLog)
                Debug.Log($"[SplashEffect] Manual splash at {worldPos}, vel={velocityMagnitude:F1} m/s, entry={isEntry}");
        }

        #endregion

        #region Private Helpers

        private void ResolveConfig()
        {
            _config = profile != null ? profile.splash : new SplashEffectConfig();
        }

        private void BuildPool()
        {
            _pool = new List<ParticleSystem>(_config.poolSize);

            if (string.IsNullOrEmpty(_config.splashParticlePrefabPath)) return;

            var prefab = Resources.Load<GameObject>(_config.splashParticlePrefabPath);
            if (prefab == null)
            {
                if (debugLog)
                    Debug.LogWarning($"[SplashEffect] Prefab not found at Resources/{_config.splashParticlePrefabPath}");
                return;
            }

            for (int i = 0; i < _config.poolSize; i++)
            {
                var go = Instantiate(prefab);
                go.SetActive(false);
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) _pool.Add(ps);
            }
        }

        private void TriggerSplash(float waterY, bool isEntry)
        {
            Vector3 origin = new Vector3(transform.position.x, waterY, transform.position.z);
            float vel = _rb != null ? _rb.velocity.magnitude : 0f;

            SpawnSplashParticle(origin, vel);

            var data = new SplashEventData
            {
                position          = origin,
                velocityMagnitude = vel,
                timestamp         = Time.time,
                isEntry           = isEntry
            };

            OnSplashTriggered?.Invoke(data);
            WaterInteractionAnalytics.RecordSplash(data);

            if (debugLog)
                Debug.Log($"[SplashEffect] Splash at {origin}, vel={vel:F1} m/s, entry={isEntry}");
        }

        private void SpawnSplashParticle(Vector3 position, float velocityMagnitude)
        {
            if (_pool == null || _pool.Count == 0) return;

            // Find an inactive pooled instance
            ParticleSystem ps = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeInHierarchy)
                {
                    ps = _pool[i];
                    break;
                }
            }
            if (ps == null) ps = _pool[0]; // Recycle oldest

            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;
            ps.gameObject.SetActive(true);

            // Scale emission by velocity
            float t = _config.maxEntrySpeed > 0f
                ? Mathf.Clamp01(velocityMagnitude / _config.maxEntrySpeed)
                : 0.5f;
            float force = Mathf.Lerp(_config.splashForceMin, _config.splashForceMax, t);
            var main = ps.main;
            main.startSpeedMultiplier = force;

            ps.Play();
            StartCoroutine(ReturnToPoolAfterLifetime(ps));
        }

        private IEnumerator ReturnToPoolAfterLifetime(ParticleSystem ps)
        {
            yield return new WaitUntil(() => !ps.IsAlive(true));
            ps.gameObject.SetActive(false);
        }

        private void UpdateSkimState(float waterY)
        {
            float heightAboveWater = transform.position.y - waterY;
            float horizontalSpeed = _rb != null
                ? new Vector3(_rb.velocity.x, 0f, _rb.velocity.z).magnitude
                : 0f;

            bool shouldSkim = heightAboveWater >= 0f
                           && heightAboveWater <= skimHeightThreshold
                           && horizontalSpeed >= skimMinSpeed;

            if (shouldSkim != _isSkimming)
            {
                _isSkimming = shouldSkim;
                if (sprayTrailObject != null)
                    sprayTrailObject.SetActive(_isSkimming);
            }
        }

        #endregion
    }
}
