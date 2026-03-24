// SplashEffectController.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 74 — Manages visual and audio splash / wake effects triggered by
    /// <see cref="BuoyancyController"/> contact events.
    ///
    /// <para>Null-safe integration points:</para>
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Audio.AudioManager"/> — splash and wake sounds.</item>
    ///   <item><see cref="SWEF.Contrail.ContrailManager"/> — wake trail rendering pipeline.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public class SplashEffectController : MonoBehaviour
    {
        #region Inspector

        [Header("Splash Prefabs")]
        [Tooltip("Particle prefab for light spray (low-speed skim).")]
        [SerializeField] private ParticleSystem lightSprayPrefab;
        [Tooltip("Particle prefab for medium splash (normal contact).")]
        [SerializeField] private ParticleSystem mediumSplashPrefab;
        [Tooltip("Particle prefab for heavy splash (high-speed impact).")]
        [SerializeField] private ParticleSystem heavySplashPrefab;
        [Tooltip("Particle prefab for controlled touchdown.")]
        [SerializeField] private ParticleSystem touchdownPrefab;
        [Tooltip("Particle prefab for skip / bounce.")]
        [SerializeField] private ParticleSystem skipPrefab;
        [Tooltip("Particle prefab for nose-first dive entry.")]
        [SerializeField] private ParticleSystem diveEntryPrefab;
        [Tooltip("Particle prefab for belly-flop flat impact.")]
        [SerializeField] private ParticleSystem bellyFlopPrefab;
        [Tooltip("Particle prefab for the continuous wake trail.")]
        [SerializeField] private ParticleSystem wakeTrailPrefab;

        [Header("Pool")]
        [Tooltip("Number of particle instances to pre-allocate per type.")]
        [SerializeField] private int poolSizePerType = 4;

        [Header("Wake")]
        [Tooltip("Trail renderer used for the V-shaped surface wake.")]
        [SerializeField] private TrailRenderer wakeTrailRenderer;
        [Tooltip("Minimum wake trail width.")]
        [SerializeField] private float wakeWidthMin = 0.5f;
        [Tooltip("Maximum wake trail width at high speed.")]
        [SerializeField] private float wakeWidthMax = 8f;
        [Tooltip("Speed (m/s) at which wake reaches maximum width.")]
        [SerializeField] private float wakeMaxSpeed = 60f;

        [Header("Camera Shake")]
        [Tooltip("Shake magnitude per unit of impact force.")]
        [SerializeField] private float shakePerForce = 0.0005f;
        [Tooltip("Maximum camera shake magnitude.")]
        [SerializeField] private float maxShakeMagnitude = 0.3f;

        #endregion

        #region Events

        /// <summary>Fired each time a splash effect is triggered.</summary>
        public event Action<SplashEvent> OnSplashTriggered;

        /// <summary>Fired when the continuous wake trail begins.</summary>
        public event Action OnWakeStarted;

        /// <summary>Fired when the wake trail stops.</summary>
        public event Action OnWakeStopped;

        #endregion

        #region Private State

        private readonly Dictionary<SplashType, Queue<ParticleSystem>> _pool =
            new Dictionary<SplashType, Queue<ParticleSystem>>();

        private BuoyancyController _buoyancy;
        private Rigidbody _rb;
        private bool _wakeActive;
        private float _splashCooldown;
        private WaterConfig _config;

        // Null-safe cross-system references
        private Component _audioManager;
        private bool _crossSystemCacheDone;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _buoyancy  = GetComponent<BuoyancyController>();
            _rb        = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _config = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.Config
                : new WaterConfig();

            if (!_crossSystemCacheDone) CacheCrossSystemReferences();

            InitPool(SplashType.LightSpray,   lightSprayPrefab);
            InitPool(SplashType.MediumSplash, mediumSplashPrefab);
            InitPool(SplashType.HeavySplash,  heavySplashPrefab);
            InitPool(SplashType.Touchdown,    touchdownPrefab);
            InitPool(SplashType.Skip,         skipPrefab);
            InitPool(SplashType.DiveEntry,    diveEntryPrefab);
            InitPool(SplashType.BellyFlop,    bellyFlopPrefab);
            InitPool(SplashType.WakeTrail,    wakeTrailPrefab);

            if (_buoyancy != null)
            {
                _buoyancy.OnWaterContact  += TriggerSplash;
                _buoyancy.OnStateChanged  += HandleStateChanged;
            }

            if (wakeTrailRenderer != null)
                wakeTrailRenderer.enabled = false;
        }

        private void Update()
        {
            _splashCooldown -= Time.deltaTime;
            UpdateWake();
        }

        private void OnDestroy()
        {
            if (_buoyancy != null)
            {
                _buoyancy.OnWaterContact -= TriggerSplash;
                _buoyancy.OnStateChanged -= HandleStateChanged;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns the appropriate splash particle effect at the event position,
        /// scaled by impact force. Respects the splash cooldown.
        /// </summary>
        /// <param name="evt">Splash event payload from <see cref="BuoyancyController"/>.</param>
        public void TriggerSplash(SplashEvent evt)
        {
            if (_splashCooldown > 0f) return;
            _splashCooldown = _config != null ? _config.splashCooldown : 0.3f;

            ParticleSystem ps = GetFromPool(evt.type);
            if (ps != null)
            {
                ps.transform.position = evt.position;
                // Align spray direction with incoming velocity
                if (evt.velocity.sqrMagnitude > 0.01f)
                    ps.transform.forward = evt.velocity.normalized;

                // Scale by impact force
                float scale = Mathf.Clamp(1f + evt.impactForce * 0.0005f, 0.5f, 4f);
                ps.transform.localScale = Vector3.one * scale;
                ps.Play();
                StartCoroutine(ReturnToPoolAfterPlay(ps, evt.type));
            }

            PlaySplashAudio(evt);
            ApplyCameraShake(evt.impactForce);
            OnSplashTriggered?.Invoke(evt);
        }

        #endregion

        #region Wake

        private void UpdateWake()
        {
            if (_buoyancy == null) return;
            WaterContactState state = _buoyancy.State.contactState;
            bool shouldWake = state == WaterContactState.Floating || state == WaterContactState.Skimming;

            if (shouldWake && !_wakeActive)
            {
                _wakeActive = true;
                if (wakeTrailRenderer != null) wakeTrailRenderer.enabled = true;
                OnWakeStarted?.Invoke();
            }
            else if (!shouldWake && _wakeActive)
            {
                _wakeActive = false;
                if (wakeTrailRenderer != null) wakeTrailRenderer.enabled = false;
                OnWakeStopped?.Invoke();
            }

            if (_wakeActive && wakeTrailRenderer != null && _rb != null)
            {
                float speed = _rb.linearVelocity.magnitude;
                float width = Mathf.Lerp(wakeWidthMin, wakeWidthMax, speed / wakeMaxSpeed);
                wakeTrailRenderer.startWidth = width;
                wakeTrailRenderer.endWidth   = 0f;
            }
        }

        private void HandleStateChanged(WaterContactState state)
        {
            // Mute or resume continuous water rush audio
            PlayWaterRushAudio(state == WaterContactState.Floating || state == WaterContactState.Skimming);
        }

        #endregion

        #region Pool

        private void InitPool(SplashType type, ParticleSystem prefab)
        {
            var queue = new Queue<ParticleSystem>();
            if (prefab != null)
            {
                for (int i = 0; i < poolSizePerType; i++)
                {
                    var ps = Instantiate(prefab, transform);
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                    queue.Enqueue(ps);
                }
            }
            _pool[type] = queue;
        }

        private ParticleSystem GetFromPool(SplashType type)
        {
            if (_pool.TryGetValue(type, out var queue) && queue.Count > 0)
            {
                var ps = queue.Dequeue();
                ps.gameObject.SetActive(true);
                return ps;
            }
            return null;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterPlay(ParticleSystem ps, SplashType type)
        {
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            if (_pool.TryGetValue(type, out var queue))
                queue.Enqueue(ps);
        }

        #endregion

        #region Audio

        private void PlaySplashAudio(SplashEvent evt)
        {
            if (_audioManager == null) return;
            try
            {
                string clipName = evt.type switch
                {
                    SplashType.LightSpray   => "SplashLight",
                    SplashType.HeavySplash  => "SplashHeavy",
                    SplashType.BellyFlop    => "SplashBellyFlop",
                    SplashType.DiveEntry    => "SplashDive",
                    _                       => "SplashMedium",
                };
                var method = _audioManager.GetType().GetMethod("PlayOneShot")
                             ?? _audioManager.GetType().GetMethod("Play");
                method?.Invoke(_audioManager, new object[] { clipName, evt.position });
            }
            catch { }
        }

        private void PlayWaterRushAudio(bool active)
        {
            if (_audioManager == null) return;
            try
            {
                string methodName = active ? "PlayLoop" : "StopLoop";
                var method = _audioManager.GetType().GetMethod(methodName);
                method?.Invoke(_audioManager, new object[] { "WaterRush" });
            }
            catch { }
        }

        #endregion

        #region Camera Shake

        private void ApplyCameraShake(float impactForce)
        {
            float magnitude = Mathf.Min(impactForce * shakePerForce, maxShakeMagnitude);
            if (magnitude < 0.01f) return;

            Camera cam = Camera.main;
            if (cam != null)
                StartCoroutine(ShakeCamera(cam, magnitude, 0.3f));
        }

        private System.Collections.IEnumerator ShakeCamera(Camera cam, float magnitude, float duration)
        {
            Vector3 originalPos = cam.transform.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float damping = 1f - (elapsed / duration);
                cam.transform.localPosition = originalPos + UnityEngine.Random.insideUnitSphere * magnitude * damping;
                elapsed += Time.deltaTime;
                yield return null;
            }
            cam.transform.localPosition = originalPos;
        }

        #endregion

        #region Cross-System Cache

        private void CacheCrossSystemReferences()
        {
            _crossSystemCacheDone = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var amType = assembly.GetType("SWEF.Audio.AudioManager");
                if (amType != null)
                {
                    _audioManager = FindObjectOfType(amType) as Component;
                    if (_audioManager != null) break;
                }
            }
        }

        #endregion
    }
}
