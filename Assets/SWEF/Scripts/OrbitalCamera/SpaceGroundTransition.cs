// SpaceGroundTransition.cs — SWEF Satellite View & Orbital Camera System
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Manages cinematic multi-phase transitions between orbital altitude and
    /// ground level.  Drives the <see cref="OrbitalCameraController"/> during
    /// descent / ascent, ramping speed and atmospheric effects through each phase.
    /// </summary>
    public class SpaceGroundTransition : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("Orbital camera profile — transition config is read from here.")]
        [SerializeField] private OrbitalCameraProfile profile;

        [Header("Speed Curve")]
        [Tooltip("Normalised speed multiplier over transition progress (0–1).")]
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

        [Header("Atmospheric Scatter")]
        [Tooltip("Maximum scatter intensity at low altitude.")]
        [SerializeField] private float maxScatterIntensity = 1f;

        #endregion

        #region Events

        /// <summary>Raised at transition start. Parameter is <c>true</c> for descent.</summary>
        public event Action<bool> OnTransitionStarted;

        /// <summary>Raised when moving from one transition phase to the next.</summary>
        public event Action<int> OnTransitionPhaseChanged;

        /// <summary>Raised when the transition reaches its destination.</summary>
        public event Action OnTransitionComplete;

        #endregion

        #region Private State

        private Coroutine _activeTransition;
        private float _progress;
        private bool _isTransitioning;
        private int _currentPhase;

        #endregion

        #region Public API

        /// <summary>
        /// Starts a cinematic descent to the specified geographic coordinate.
        /// Aborts any transition already in progress.
        /// </summary>
        /// <param name="latitude">Target latitude in degrees.</param>
        /// <param name="longitude">Target longitude in degrees.</param>
        /// <param name="durationOverride">Override total duration (seconds). ≤0 uses profile default.</param>
        public void StartDescentTo(double latitude, double longitude, float durationOverride)
        {
            AbortTransition();
            var dur = durationOverride > 0f ? durationOverride
                : (profile != null ? profile.transitionConfig.transitionDuration : 10f);
            _activeTransition = StartCoroutine(DescentCoroutine(latitude, longitude, dur));
        }

        /// <summary>
        /// Starts a cinematic ascent to the specified orbital altitude.
        /// Aborts any transition already in progress.
        /// </summary>
        /// <param name="targetAltitudeKm">Target altitude in kilometres.</param>
        /// <param name="durationOverride">Override total duration (seconds). ≤0 uses profile default.</param>
        public void StartAscentToOrbit(float targetAltitudeKm, float durationOverride)
        {
            AbortTransition();
            var dur = durationOverride > 0f ? durationOverride
                : (profile != null ? profile.transitionConfig.transitionDuration : 10f);
            _activeTransition = StartCoroutine(AscentCoroutine(targetAltitudeKm, dur));
        }

        /// <summary>Immediately stops the active transition, if any.</summary>
        public void AbortTransition()
        {
            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
                _activeTransition = null;
            }
            _isTransitioning = false;
            _progress = 0f;
            _currentPhase = 0;
        }

        /// <summary>Returns transition progress in the 0–1 range.</summary>
        public float GetTransitionProgress() => _progress;

        #endregion

        #region Coroutines

        private IEnumerator DescentCoroutine(double lat, double lon, float duration)
        {
            _isTransitioning = true;
            _progress = 0f;
            OnTransitionStarted?.Invoke(true);

            // Phases: 0=Orbit, 1=HighAtmosphere, 2=CloudPierce, 3=GroundApproach
            var phaseThresholds = new float[] { 0f, 0.3f, 0.6f, 0.8f, 1f };
            _currentPhase = 0;
            OnTransitionPhaseChanged?.Invoke(_currentPhase);

            var ctrl = OrbitalCameraController.Instance;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _progress = Mathf.Clamp01(elapsed / duration);

                // Phase detection
                for (var p = 0; p < phaseThresholds.Length - 1; p++)
                {
                    if (_progress >= phaseThresholds[p] && _progress < phaseThresholds[p + 1] &&
                        _currentPhase != p)
                    {
                        _currentPhase = p;
                        OnTransitionPhaseChanged?.Invoke(_currentPhase);
                    }
                }

                // Drive altitude
                if (ctrl != null && profile != null)
                {
                    var cfg = profile.transitionConfig;
                    var altKm = SampleKeyframes(cfg.altitudeKeyframesKm, _progress);
                    ctrl.SetTargetAltitude(altKm);
                }

                // Point camera toward destination
                ctrl?.LookAtCoordinate(lat, lon);

                yield return null;
            }

            ctrl?.LookAtCoordinate(lat, lon);
            _progress = 1f;
            _isTransitioning = false;
            _activeTransition = null;
            OnTransitionComplete?.Invoke();
        }

        private IEnumerator AscentCoroutine(float targetAltKm, float duration)
        {
            _isTransitioning = true;
            _progress = 0f;
            OnTransitionStarted?.Invoke(false);

            var startAlt = OrbitalCameraController.Instance != null
                ? OrbitalCameraController.Instance.GetCurrentAltitudeKm()
                : 0f;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _progress = Mathf.Clamp01(elapsed / duration);
                var speedMult = speedCurve.Evaluate(_progress);
                var altKm = Mathf.Lerp(startAlt, targetAltKm, _progress * speedMult);
                OrbitalCameraController.Instance?.SetTargetAltitude(altKm);
                yield return null;
            }

            OrbitalCameraController.Instance?.SetTargetAltitude(targetAltKm);
            _progress = 1f;
            _isTransitioning = false;
            _activeTransition = null;
            OnTransitionComplete?.Invoke();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the scatter intensity (0–1) based on current altitude, ramping
        /// from <see cref="maxScatterIntensity"/> at ground to 0 above the scatter
        /// start altitude defined in the profile.
        /// </summary>
        public float GetScatterIntensity(float altitudeKm)
        {
            if (profile == null) return 0f;
            var scatterStart = profile.transitionConfig.atmosphericScatterStartAltitudeKm;
            if (scatterStart <= 0f) return 0f;
            return Mathf.Clamp01(1f - altitudeKm / scatterStart) * maxScatterIntensity;
        }

        private static float SampleKeyframes(float[] frames, float t)
        {
            if (frames == null || frames.Length == 0) return 0f;
            if (frames.Length == 1) return frames[0];

            var segLen = 1f / (frames.Length - 1);
            var segIdx = Mathf.Clamp(Mathf.FloorToInt(t / segLen), 0, frames.Length - 2);
            var segT = (t - segIdx * segLen) / segLen;
            return Mathf.Lerp(frames[segIdx], frames[segIdx + 1], segT);
        }

        #endregion
    }
}
