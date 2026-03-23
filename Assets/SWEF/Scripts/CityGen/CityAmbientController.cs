using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Ambient effects that bring cities to life.
    ///
    /// <para>Manages:
    /// <list type="bullet">
    ///   <item>Crowd noise AudioSources in city zones.</item>
    ///   <item>Chimney smoke particles on industrial buildings.</item>
    ///   <item>Flag/banner animation on landmark GameObjects.</item>
    ///   <item>Bird flock spawning over parks.</item>
    ///   <item>Water fountain particle effects in plazas.</item>
    /// </list>
    /// </para>
    ///
    /// <para>All effects are subject to an altitude cutoff and a global performance
    /// budget that limits total active effects.</para>
    /// </summary>
    public class CityAmbientController : MonoBehaviour
    {
        #region Constants

        private const float AltitudeCutoff     = 800f;   // metres — effects hidden above this
        private const float FlagWaveFrequency  = 1.2f;
        private const float FlagWaveAmplitude  = 8f;     // degrees

        #endregion

        #region Inspector

        [Header("Audio")]
        [Tooltip("Ambient crowd AudioClip played in city center zones.")]
        [SerializeField] private AudioClip crowdAudioClip;

        [Tooltip("Maximum simultaneous crowd audio sources.")]
        [SerializeField] private int maxAudioSources = 4;

        [Header("Particles")]
        [Tooltip("Smoke particle system prefab for chimneys.")]
        [SerializeField] private GameObject smokePrefab;

        [Tooltip("Water fountain particle system prefab for plazas.")]
        [SerializeField] private GameObject fountainPrefab;

        [Header("Flocks")]
        [Tooltip("Bird flock prefab for park areas.")]
        [SerializeField] private GameObject birdFlockPrefab;

        [Tooltip("Maximum simultaneous bird flocks.")]
        [SerializeField] private int maxBirdFlocks = 3;

        [Header("Budget")]
        [Tooltip("Total maximum active ambient effect GameObjects.")]
        [SerializeField] private int maxActiveEffects = 30;

        [Header("Altitude")]
        [Tooltip("Camera whose altitude gates the ambient system.")]
        [SerializeField] private Camera trackedCamera;

        #endregion

        #region Private State

        private readonly List<GameObject>   _activeEffects   = new List<GameObject>();
        private readonly List<AudioSource>  _audioSources    = new List<AudioSource>();
        private readonly List<Transform>    _flagTransforms  = new List<Transform>();

        private bool _effectsActive = true;
        private Coroutine _updateCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (trackedCamera == null) trackedCamera = Camera.main;
            _updateCoroutine = StartCoroutine(AmbientLoop());
        }

        private void Update()
        {
            if (trackedCamera == null) return;
            bool shouldBeActive = trackedCamera.transform.position.y < AltitudeCutoff;
            if (shouldBeActive != _effectsActive)
            {
                _effectsActive = shouldBeActive;
                SetEffectsActive(_effectsActive);
            }

            if (_effectsActive) AnimateFlags();
        }

        private void OnDestroy()
        {
            if (_updateCoroutine != null) StopCoroutine(_updateCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns a chimney smoke effect at <paramref name="position"/> parented to
        /// <paramref name="parent"/> if the effect budget permits.
        /// </summary>
        public void SpawnSmokeEffect(Vector3 position, Transform parent)
        {
            if (!CanSpawnEffect()) return;
            if (smokePrefab == null) return;
            var go = Instantiate(smokePrefab, position, Quaternion.identity, parent);
            go.name = "ChimneySmoke";
            RegisterEffect(go);
        }

        /// <summary>
        /// Spawns a water fountain effect at <paramref name="position"/>.
        /// </summary>
        public void SpawnFountainEffect(Vector3 position, Transform parent)
        {
            if (!CanSpawnEffect()) return;
            if (fountainPrefab == null) return;
            var go = Instantiate(fountainPrefab, position, Quaternion.identity, parent);
            go.name = "Fountain";
            RegisterEffect(go);
        }

        /// <summary>
        /// Spawns a bird flock near a park area if the flock budget permits.
        /// </summary>
        public void SpawnBirdFlock(Vector3 position, Transform parent)
        {
            int currentFlocks = CountActiveByName("BirdFlock");
            if (currentFlocks >= maxBirdFlocks) return;
            if (birdFlockPrefab == null) return;
            if (!CanSpawnEffect()) return;
            var go = Instantiate(birdFlockPrefab, position + Vector3.up * 30f, Quaternion.identity, parent);
            go.name = "BirdFlock";
            RegisterEffect(go);
        }

        /// <summary>
        /// Registers a flag <see cref="Transform"/> for wave animation.
        /// </summary>
        public void RegisterFlag(Transform flagTransform)
        {
            if (flagTransform != null) _flagTransforms.Add(flagTransform);
        }

        /// <summary>
        /// Spawns a crowd audio source at <paramref name="position"/> if the budget allows.
        /// </summary>
        public void SpawnCrowdAudio(Vector3 position, float radius)
        {
            if (crowdAudioClip == null) return;
            if (_audioSources.Count >= maxAudioSources) return;

            var go     = new GameObject("CrowdAudio");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip        = crowdAudioClip;
            source.loop        = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = radius;
            source.Play();

            _audioSources.Add(source);
            RegisterEffect(go);
        }

        #endregion

        #region Ambient Loop

        private IEnumerator AmbientLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                // Remove destroyed effects from registry.
                _activeEffects.RemoveAll(e => e == null);
                _audioSources.RemoveAll(s => s == null);
                _flagTransforms.RemoveAll(f => f == null);
            }
        }

        #endregion

        #region Flag Animation

        private void AnimateFlags()
        {
            float wave = Mathf.Sin(Time.time * FlagWaveFrequency) * FlagWaveAmplitude;
            foreach (var f in _flagTransforms)
            {
                if (f == null) continue;
                f.localRotation = Quaternion.Euler(0f, wave, 0f);
            }
        }

        #endregion

        #region Helpers

        private bool CanSpawnEffect()
        {
            // Clean up null entries first.
            _activeEffects.RemoveAll(e => e == null);
            return _activeEffects.Count < maxActiveEffects;
        }

        private void RegisterEffect(GameObject go)
        {
            go.SetActive(_effectsActive);
            _activeEffects.Add(go);
        }

        private void SetEffectsActive(bool active)
        {
            foreach (var e in _activeEffects)
                if (e != null) e.SetActive(active);
            foreach (var s in _audioSources)
                if (s != null)
                {
                    if (active) s.Play(); else s.Pause();
                }
        }

        private int CountActiveByName(string namePrefix)
        {
            int count = 0;
            foreach (var e in _activeEffects)
                if (e != null && e.name.StartsWith(namePrefix)) count++;
            return count;
        }

        #endregion
    }
}
