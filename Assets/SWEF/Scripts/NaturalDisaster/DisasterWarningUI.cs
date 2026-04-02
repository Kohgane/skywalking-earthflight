// DisasterWarningUI.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — Full-screen warning overlay displayed when a disaster enters the
    /// Warning phase.  Shows the disaster type icon, name, severity badge, bearing and
    /// distance to the epicentre, estimated time to onset, and action buttons.
    ///
    /// <para>Fades in/out using an <see cref="AnimationCurve"/>.
    /// Proximity-based audio warning becomes louder when the player is closer.</para>
    /// </summary>
    public class DisasterWarningUI : MonoBehaviour
    {
        // ── Inspector — Panel ─────────────────────────────────────────────────────

        [Header("Panel")]
        [Tooltip("CanvasGroup that controls the overlay alpha.")]
        [SerializeField] private CanvasGroup _panelGroup;

        [Tooltip("Curve for fade-in (time 0→1, value 0→1).")]
        [SerializeField] private AnimationCurve _fadeInCurve
            = AnimationCurve.EaseInOut(0f, 0f, 0.4f, 1f);

        [Tooltip("Curve for fade-out (time 0→1, value 1→0).")]
        [SerializeField] private AnimationCurve _fadeOutCurve
            = AnimationCurve.EaseInOut(0f, 1f, 0.4f, 0f);

        // ── Inspector — Content ───────────────────────────────────────────────────

        [Header("Content")]
        [Tooltip("Image used to display the disaster icon.")]
        [SerializeField] private Image _disasterIcon;

        [Tooltip("Text label for the disaster name.")]
        [SerializeField] private Text _disasterNameText;

        [Tooltip("Text label for the severity badge.")]
        [SerializeField] private Text _severityText;

        [Tooltip("Text showing distance (km) to the epicentre.")]
        [SerializeField] private Text _distanceText;

        [Tooltip("Text showing bearing (degrees) to the epicentre.")]
        [SerializeField] private Text _bearingText;

        [Tooltip("Text showing estimated seconds until onset.")]
        [SerializeField] private Text _etaText;

        // ── Inspector — Buttons ───────────────────────────────────────────────────

        [Header("Buttons")]
        [Tooltip("Button that dismisses the overlay without taking action.")]
        [SerializeField] private Button _avoidAreaButton;

        [Tooltip("Button that accepts and starts the linked rescue mission.")]
        [SerializeField] private Button _acceptMissionButton;

        // ── Inspector — Audio ─────────────────────────────────────────────────────

        [Header("Audio")]
        [Tooltip("AudioSource used to play the proximity warning sound.")]
        [SerializeField] private AudioSource _warningAudioSource;

        [Tooltip("Maximum distance (metres) at which the warning audio is at full volume.")]
        [SerializeField] [Min(100f)] private float _maxAudioDistance = 20000f;

        // ── Private State ─────────────────────────────────────────────────────────

        private ActiveDisaster _currentDisaster;
        private Transform      _playerTransform;
        private Coroutine      _fadeCoroutine;
        private bool           _isShowing;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_panelGroup != null) _panelGroup.alpha = 0f;

            if (_avoidAreaButton != null)    _avoidAreaButton.onClick.AddListener(OnAvoidArea);
            if (_acceptMissionButton != null) _acceptMissionButton.onClick.AddListener(OnAcceptMission);
        }

        private void Start()
        {
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null) _playerTransform = fc.transform;

            if (DisasterManager.Instance != null)
                DisasterManager.Instance.OnDisasterPhaseChanged += HandlePhaseChanged;
        }

        private void OnDestroy()
        {
            if (DisasterManager.Instance != null)
                DisasterManager.Instance.OnDisasterPhaseChanged -= HandlePhaseChanged;
        }

        private void Update()
        {
            if (!_isShowing || _currentDisaster == null || _playerTransform == null) return;

            UpdateDistanceAndBearing();
            UpdateETA();
            UpdateProximityAudio();
        }

        // ── Phase Handler ─────────────────────────────────────────────────────────

        private void HandlePhaseChanged(ActiveDisaster disaster)
        {
            if (disaster == null) return;

            if (disaster.currentPhase == DisasterPhase.Warning)
            {
                ShowWarning(disaster);
            }
            else if (_currentDisaster == disaster &&
                     disaster.currentPhase != DisasterPhase.Warning)
            {
                HideWarning();
            }
        }

        // ── Show / Hide ───────────────────────────────────────────────────────────

        private void ShowWarning(ActiveDisaster disaster)
        {
            _currentDisaster = disaster;
            PopulateContent(disaster);
            _isShowing = true;

            if (_warningAudioSource != null && disaster.data?.warningSound != null)
                _warningAudioSource.PlayOneShot(disaster.data.warningSound);

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadePanel(_fadeInCurve));
        }

        private void HideWarning()
        {
            _isShowing = false;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadePanel(_fadeOutCurve));
        }

        private IEnumerator FadePanel(AnimationCurve curve)
        {
            if (_panelGroup == null) yield break;

            float duration = curve.keys[curve.length - 1].time;
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _panelGroup.alpha = curve.Evaluate(elapsed);
                yield return null;
            }
            _panelGroup.alpha = curve.Evaluate(duration);
        }

        // ── Content Helpers ───────────────────────────────────────────────────────

        private void PopulateContent(ActiveDisaster disaster)
        {
            if (disaster.data == null) return;

            if (_disasterIcon != null && disaster.data.disasterIcon != null)
                _disasterIcon.sprite = disaster.data.disasterIcon;

            if (_disasterNameText != null)
                _disasterNameText.text = disaster.data.disasterName;

            if (_severityText != null)
                _severityText.text = disaster.currentSeverity.ToString().ToUpper();
        }

        private void UpdateDistanceAndBearing()
        {
            float dist = Vector3.Distance(_playerTransform.position, _currentDisaster.epicenter);

            if (_distanceText != null)
                _distanceText.text = $"{dist / 1000f:F1} km";

            if (_bearingText != null)
            {
                Vector3 dir   = _currentDisaster.epicenter - _playerTransform.position;
                float bearing = (Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 360f) % 360f;
                _bearingText.text = $"{bearing:F0}°";
            }
        }

        private void UpdateETA()
        {
            if (_etaText == null) return;
            float remaining = _currentDisaster.data.warningDuration - _currentDisaster.phaseElapsedTime;
            remaining = Mathf.Max(0f, remaining);
            _etaText.text = $"{remaining:F0}s";
        }

        private void UpdateProximityAudio()
        {
            if (_warningAudioSource == null || !_warningAudioSource.isPlaying) return;
            float dist = Vector3.Distance(_playerTransform.position, _currentDisaster.epicenter);
            _warningAudioSource.volume = 1f - Mathf.Clamp01(dist / _maxAudioDistance);
        }

        // ── Button Callbacks ──────────────────────────────────────────────────────

        private void OnAvoidArea()
        {
            HideWarning();
        }

        private void OnAcceptMission()
        {
            HideWarning();
            // RescueMissionGenerator handles mission generation via DisasterManager events.
            Debug.Log($"[SWEF] DisasterWarningUI: player accepted rescue mission for {_currentDisaster?.data?.disasterName}.");
        }
    }
}
