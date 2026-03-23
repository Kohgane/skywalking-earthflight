using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Smart crossfade controller for the In-Flight Music Player.
    /// <para>
    /// Manages smooth volume transitions between the current and next track using
    /// configurable crossfade curves (Linear, EaseInOut, EqualPower).  Supports
    /// auto-crossfade when a track nears its end, priority crossfades triggered by
    /// flight-phase changes from <see cref="MusicFlightSync"/>, and manual crossfades
    /// initiated by <see cref="MusicPlaylistController"/>.
    /// </para>
    /// <para>
    /// Exposes <see cref="OnCrossfadeStart"/> and <see cref="OnCrossfadeComplete"/>
    /// events (both C# and <see cref="UnityEvent"/> variants) for UI and analytics hooks.
    /// </para>
    /// </summary>
    public class MusicCrossfadeController : MonoBehaviour
    {
        // ── Crossfade curve enum ──────────────────────────────────────────────────
        /// <summary>Envelope curve used during a crossfade.</summary>
        public enum CrossfadeCurve
        {
            /// <summary>Constant-rate volume change.</summary>
            Linear,
            /// <summary>Smooth ease-in then ease-out (SmoothStep).</summary>
            EaseInOut,
            /// <summary>Constant-power crossfade — perceptually even loudness.</summary>
            EqualPower
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Crossfade Settings")]
        [Tooltip("Duration of the crossfade in seconds.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float crossfadeDuration = 3f;

        [Tooltip("Crossfade envelope curve.")]
        [SerializeField] private CrossfadeCurve crossfadeCurve = CrossfadeCurve.EqualPower;

        [Header("Auto-Crossfade")]
        [Tooltip("When true, automatically starts a crossfade when the current track has this many seconds remaining.")]
        [SerializeField] private bool autoCrossfadeEnabled = true;

        [Tooltip("Seconds before track end to begin the auto-crossfade overlap.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float autoOverlapWindow = 3f;

        [Header("Priority Crossfade")]
        [Tooltip("Duration used for priority crossfades triggered by flight-phase changes.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float priorityCrossfadeDuration = 1.5f;

        [Header("References (auto-found if null)")]
        [Tooltip("MusicFlightSync reference. Auto-found if null.")]
        [SerializeField] private MusicFlightSync flightSync;

        [Header("Events")]
        [Tooltip("UnityEvent fired when a crossfade begins.")]
        public UnityEvent onCrossfadeStartUnity;

        [Tooltip("UnityEvent fired when a crossfade completes.")]
        public UnityEvent onCrossfadeCompleteUnity;

        // ── C# Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired when a crossfade begins. Parameter is the crossfade duration in seconds.</summary>
        public event Action<float> OnCrossfadeStart;

        /// <summary>Fired when a crossfade completes.</summary>
        public event Action OnCrossfadeComplete;

        // ── Private state ─────────────────────────────────────────────────────────
        private AudioSource  _sourceA;           // currently fading out
        private AudioSource  _sourceB;           // fading in (next track)
        private bool         _isCrossfading;
        private Coroutine    _crossfadeCoroutine;
        private MusicMood    _lastKnownMood;
        private bool         _autoTriggerArmed;  // true once we enter the overlap window

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether a crossfade is currently in progress.</summary>
        public bool IsCrossfading => _isCrossfading;

        /// <summary>Crossfade duration setting (seconds).</summary>
        public float CrossfadeDuration
        {
            get => crossfadeDuration;
            set => crossfadeDuration = Mathf.Clamp(value, 0.5f, 10f);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightSync == null)
                flightSync = FindFirstObjectByType<MusicFlightSync>();

            if (flightSync != null)
                flightSync.OnFlightMoodChanged += HandleFlightMoodChanged;
        }

        private void OnDestroy()
        {
            if (flightSync != null)
                flightSync.OnFlightMoodChanged -= HandleFlightMoodChanged;
        }

        private void Update()
        {
            if (!autoCrossfadeEnabled || _isCrossfading) return;
            CheckAutoTrigger();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Initiates a crossfade from <paramref name="from"/> to <paramref name="to"/>
        /// using the configured <see cref="CrossfadeDuration"/> and <see cref="crossfadeCurve"/>.
        /// </summary>
        /// <param name="from">The <see cref="AudioSource"/> currently playing (will fade out).</param>
        /// <param name="to">The <see cref="AudioSource"/> for the next track (will fade in).</param>
        public void StartCrossfade(AudioSource from, AudioSource to)
        {
            StartCrossfadeInternal(from, to, crossfadeDuration);
        }

        /// <summary>
        /// Initiates a priority crossfade using the shorter <see cref="priorityCrossfadeDuration"/>.
        /// Intended for flight-phase changes where a rapid transition is preferred.
        /// </summary>
        /// <param name="from">The <see cref="AudioSource"/> currently playing (will fade out).</param>
        /// <param name="to">The <see cref="AudioSource"/> for the next track (will fade in).</param>
        public void StartPriorityCrossfade(AudioSource from, AudioSource to)
        {
            StartCrossfadeInternal(from, to, priorityCrossfadeDuration);
        }

        /// <summary>
        /// Cancels an in-progress crossfade, leaving the fade-in source at full volume
        /// and stopping the fade-out source.
        /// </summary>
        public void CancelCrossfade()
        {
            if (!_isCrossfading) return;

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            if (_sourceB != null) _sourceB.volume = 1f;
            if (_sourceA != null) _sourceA.Stop();

            _isCrossfading = false;
        }

        /// <summary>
        /// Evaluates the crossfade volume for the fade-in source at normalised time <paramref name="t"/>.
        /// The fade-out source uses the complementary value.
        /// </summary>
        /// <param name="t">Normalised time [0, 1].</param>
        /// <param name="curve">Curve type to use for the evaluation.</param>
        /// <returns>Gain for the incoming track [0, 1].</returns>
        public float EvaluateCurve(float t, CrossfadeCurve curve)
        {
            switch (curve)
            {
                case CrossfadeCurve.Linear:
                    return t;
                case CrossfadeCurve.EaseInOut:
                    return Mathf.SmoothStep(0f, 1f, t);
                case CrossfadeCurve.EqualPower:
                    // Cosine-based equal-power: in = sin(t * π/2), out = cos(t * π/2)
                    return Mathf.Sin(t * Mathf.PI * 0.5f);
                default:
                    return t;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void StartCrossfadeInternal(AudioSource from, AudioSource to, float duration)
        {
            if (from == null || to == null)
            {
                Debug.LogWarning("[SWEF][MusicCrossfadeController] StartCrossfade: source(s) null — aborting.");
                return;
            }

            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);

            _sourceA = from;
            _sourceB = to;
            _crossfadeCoroutine = StartCoroutine(RunCrossfade(duration));
        }

        private IEnumerator RunCrossfade(float duration)
        {
            _isCrossfading = true;
            _autoTriggerArmed = false;

            float initialVolumeA = _sourceA != null ? _sourceA.volume : 1f;

            if (_sourceB != null && !_sourceB.isPlaying)
            {
                _sourceB.volume = 0f;
                _sourceB.Play();
            }

            OnCrossfadeStart?.Invoke(duration);
            onCrossfadeStartUnity?.Invoke();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t   = Mathf.Clamp01(elapsed / duration);
                float tIn = EvaluateCurve(t, crossfadeCurve);

                if (_sourceA != null) _sourceA.volume = initialVolumeA * (1f - tIn);
                if (_sourceB != null) _sourceB.volume = tIn;

                yield return null;
            }

            if (_sourceA != null)
            {
                _sourceA.volume = 0f;
                _sourceA.Stop();
            }
            if (_sourceB != null)
                _sourceB.volume = 1f;

            _isCrossfading      = false;
            _crossfadeCoroutine = null;

            OnCrossfadeComplete?.Invoke();
            onCrossfadeCompleteUnity?.Invoke();
        }

        private void CheckAutoTrigger()
        {
            if (MusicPlayerManager.Instance == null) return;

            AudioSource src = MusicPlayerManager.Instance.GetComponent<AudioSource>();
            if (src == null || !src.isPlaying || src.clip == null) return;

            float remaining = src.clip.length - src.time;

            if (!_autoTriggerArmed && remaining <= autoOverlapWindow)
            {
                _autoTriggerArmed = true;
                // Notify MusicPlayerManager to prepare the next track for crossfade
                Debug.Log("[SWEF][MusicCrossfadeController] Auto-crossfade window entered — signalling manager.");
            }
        }

        private void HandleFlightMoodChanged(MusicMood newMood)
        {
            if (newMood == _lastKnownMood) return;
            _lastKnownMood = newMood;

            if (!_isCrossfading)
            {
                // Notify manager that a priority crossfade should be triggered
                Debug.Log($"[SWEF][MusicCrossfadeController] Flight mood → {newMood}: priority crossfade requested.");
            }
        }
    }
}
