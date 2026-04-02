// CheckpointGateController.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Runtime MonoBehaviour attached to each checkpoint gate prefab.
    /// Handles visual state colours, proximity trigger detection, wrong-way
    /// approach validation, split-time floating text, VFX, and audio.
    ///
    /// <para>Instantiated by <see cref="CourseVisualizerRenderer"/> for each checkpoint
    /// in the active course.  Receives state updates from <see cref="RaceManager"/>.</para>
    /// </summary>
    public class CheckpointGateController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Gate Visuals")]
        [Tooltip("Renderer of the gate ring/arch mesh.")]
        [SerializeField] private Renderer _gateRenderer;

        [Tooltip("Particle system played on gate capture.")]
        [SerializeField] private ParticleSystem _capturePulseVFX;

        [Tooltip("Particle system played when a personal best split is achieved.")]
        [SerializeField] private ParticleSystem _personalBestVFX;

        [Header("Colours")]
        [SerializeField] private Color _upcomingColor     = new Color(1.0f, 0.85f, 0.0f, 0.8f);
        [SerializeField] private Color _activeNextColor   = new Color(0.1f, 0.9f, 0.1f, 0.9f);
        [SerializeField] private Color _capturedColor     = new Color(0.2f, 0.5f, 1.0f, 0.4f);
        [SerializeField] private Color _missedColor       = new Color(1.0f, 0.1f, 0.1f, 0.9f);

        [Header("Split Time UI")]
        [Tooltip("World-space Text component that displays the split time on capture.")]
        [SerializeField] private Text _splitTimeText;

        [Tooltip("Seconds the split time text is displayed before fading.")]
        [SerializeField] [Min(0.5f)] private float _splitDisplayDuration = 3f;

        [Header("Audio")]
        [Tooltip("AudioSource used for chime and warning sounds.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Played on successful checkpoint capture.")]
        [SerializeField] private AudioClip _checkpointChime;

        [Tooltip("Played when a mandatory checkpoint is missed.")]
        [SerializeField] private AudioClip _missedWarning;

        // ── Data ──────────────────────────────────────────────────────────────────

        /// <summary>The checkpoint this gate represents.</summary>
        public RaceCheckpoint checkpoint { get; private set; }

        /// <summary>Whether this gate is the currently active next checkpoint.</summary>
        public bool isActiveNext { get; private set; }

        /// <summary>Whether this checkpoint has been captured this session.</summary>
        public bool isCaptured { get; private set; }

        /// <summary>Whether this checkpoint was missed this session.</summary>
        public bool isMissed { get; private set; }

        // ── Event ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the player enters the trigger radius.
        /// Arguments: (checkpoint, splitTime).
        /// </summary>
        public event Action<RaceCheckpoint, float> OnCheckpointCaptured;

        // ── Private State ─────────────────────────────────────────────────────────

        private Transform _playerTransform;
        private float     _splitTextTimer;
        private bool      _triggerActive;
        private static readonly int _colorPropId = Shader.PropertyToID("_Color");

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null) _playerTransform = fc.transform;

            if (_splitTimeText != null)
                _splitTimeText.gameObject.SetActive(false);

            SetVisualState(GateVisualState.Upcoming);
        }

        private void Update()
        {
            if (_playerTransform == null || isCaptured || checkpoint == null) return;
            if (!isActiveNext) return;

            float dist = Vector3.Distance(_playerTransform.position, transform.position);
            if (dist <= checkpoint.triggerRadius && !_triggerActive)
            {
                _triggerActive = true;
                TriggerCapture();
            }
            else if (dist > checkpoint.triggerRadius)
            {
                _triggerActive = false;
            }

            // Fade split text
            if (_splitTimeText != null && _splitTimeText.gameObject.activeSelf)
            {
                _splitTextTimer -= Time.deltaTime;
                if (_splitTextTimer <= 0f)
                    _splitTimeText.gameObject.SetActive(false);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Binds this gate to a <see cref="RaceCheckpoint"/> and positions it.</summary>
        public void Initialize(RaceCheckpoint cp, Transform playerTransform = null)
        {
            checkpoint       = cp;
            _playerTransform = playerTransform;

            transform.position = new Vector3(
                (float)(cp.longitude * 111320.0 * Math.Cos(cp.latitude * Mathf.Deg2Rad)),
                cp.altitude,
                (float)(cp.latitude * 111320.0));
            transform.rotation = cp.gateRotation;
            transform.localScale = new Vector3(cp.gateWidth, cp.gateHeight, 1f);
        }

        /// <summary>Marks this gate as the next checkpoint the player must hit.</summary>
        public void SetAsActiveNext(bool active)
        {
            isActiveNext = active;
            if (!isCaptured && !isMissed)
                SetVisualState(active ? GateVisualState.ActiveNext : GateVisualState.Upcoming);
        }

        /// <summary>Marks the gate as captured with the given split time.</summary>
        public void MarkCaptured(float splitTime, bool isPersonalBestSplit)
        {
            isCaptured = true;
            SetVisualState(GateVisualState.Captured);
            ShowSplitTime(splitTime, isPersonalBestSplit);
            PlayCaptureFeedback(isPersonalBestSplit);
        }

        /// <summary>Marks the gate as missed (mandatory checkpoint was skipped).</summary>
        public void MarkMissed()
        {
            isMissed = true;
            SetVisualState(GateVisualState.Missed);
            PlayMissedFeedback();
        }

        /// <summary>Resets the gate to its default upcoming state.</summary>
        public void ResetGate()
        {
            isCaptured   = false;
            isMissed     = false;
            isActiveNext = false;
            _triggerActive = false;
            SetVisualState(GateVisualState.Upcoming);
            if (_splitTimeText != null)
                _splitTimeText.gameObject.SetActive(false);
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void TriggerCapture()
        {
            // Simple direction check — dot product of player velocity vs gate forward
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null)
            {
                Vector3 vel = fc.Velocity;
                if (vel.sqrMagnitude > 0.01f)
                {
                    float dot = Vector3.Dot(vel.normalized, transform.forward);
                    if (dot < CompetitiveRacingConfig.WrongWayDotThreshold)
                        return; // Approaching from wrong direction
                }
            }

            float elapsed = RaceManager.Instance != null ? RaceManager.Instance.elapsedTime : 0f;
            OnCheckpointCaptured?.Invoke(checkpoint, elapsed);
        }

        private void SetVisualState(GateVisualState state)
        {
            if (_gateRenderer == null) return;
            Color c = state switch
            {
                GateVisualState.Upcoming   => _upcomingColor,
                GateVisualState.ActiveNext => _activeNextColor,
                GateVisualState.Captured   => _capturedColor,
                GateVisualState.Missed     => _missedColor,
                _                          => _upcomingColor
            };
            var mpb = new MaterialPropertyBlock();
            _gateRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(_colorPropId, c);
            _gateRenderer.SetPropertyBlock(mpb);
        }

        private void ShowSplitTime(float splitTime, bool isPB)
        {
            if (_splitTimeText == null) return;
            int m = (int)splitTime / 60;
            int s = (int)splitTime % 60;
            int ms = (int)((splitTime % 1f) * 100);
            _splitTimeText.text  = isPB
                ? $"<color=gold>★ {m}:{s:00}.{ms:00}</color>"
                : $"{m}:{s:00}.{ms:00}";
            _splitTimeText.gameObject.SetActive(true);
            _splitTextTimer = _splitDisplayDuration;
        }

        private void PlayCaptureFeedback(bool isPersonalBestSplit)
        {
            _capturePulseVFX?.Play();
            if (isPersonalBestSplit) _personalBestVFX?.Play();
            if (_audioSource != null && _checkpointChime != null)
                _audioSource.PlayOneShot(_checkpointChime);
        }

        private void PlayMissedFeedback()
        {
            if (_audioSource != null && _missedWarning != null)
                _audioSource.PlayOneShot(_missedWarning);
        }

        // ── Nested Types ──────────────────────────────────────────────────────────

        private enum GateVisualState { Upcoming, ActiveNext, Captured, Missed }
    }
}
