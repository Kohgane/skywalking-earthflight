using System;
using System.Collections;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Drives gamepad vibration and (on supported platforms) haptic
    /// feedback in response to in-flight events.
    /// <para>
    /// Uses Unity's legacy <c>Input</c> system via the
    /// <c>XInputDotNetPure</c> / platform-native pathway where available, with a
    /// safe no-op fallback on platforms that do not support rumble.
    /// Vibration strength respects the <see cref="GamepadProfile.vibrationEnabled"/> flag.
    /// </para>
    /// </summary>
    public class InputHapticFeedback : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputHapticFeedback Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Defaults")]
        [Tooltip("Master vibration intensity multiplier [0, 1].")]
        [Range(0f, 1f)]
        [SerializeField] private float masterIntensity = 1f;

        [Tooltip("Default vibration duration in seconds when none is specified.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float defaultDuration = 0.15f;

        [Header("Preset Intensities")]
        [Tooltip("Light feedback — button presses, UI navigation.")]
        [Range(0f, 1f)] [SerializeField] private float lightIntensity  = 0.2f;

        [Tooltip("Medium feedback — weapon fire, landing gear.")]
        [Range(0f, 1f)] [SerializeField] private float mediumIntensity = 0.5f;

        [Tooltip("Heavy feedback — collisions, hard landing, boost.")]
        [Range(0f, 1f)] [SerializeField] private float heavyIntensity  = 0.9f;

        #endregion

        #region Events

        /// <summary>Fired when haptic feedback is played.  Carries intensity and duration.</summary>
        public event Action<float, float> OnHapticPlayed;

        #endregion

        #region Public Properties

        /// <summary>Master vibration intensity multiplier [0, 1].</summary>
        public float MasterIntensity
        {
            get => masterIntensity;
            set => masterIntensity = Mathf.Clamp01(value);
        }

        /// <summary><c>true</c> when vibration is enabled in the active <see cref="GamepadProfile"/>.</summary>
        public bool VibrationEnabled
        {
            get
            {
                if (InputBindingManager.Instance?.Profile == null)
                    return true;
                return InputBindingManager.Instance.Profile.gamepadProfile.vibrationEnabled;
            }
        }

        #endregion

        #region Private State

        private Coroutine _activeVibration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            StopVibration();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) StopVibration();
        }

        #endregion

        #region Public API — Preset Feedback

        /// <summary>Plays a short light haptic pulse (button press, UI confirmation).</summary>
        public void PlayLight(float duration = -1f)
            => Play(lightIntensity, duration < 0f ? defaultDuration : duration);

        /// <summary>Plays a medium haptic pulse (action feedback, speed change).</summary>
        public void PlayMedium(float duration = -1f)
            => Play(mediumIntensity, duration < 0f ? defaultDuration : duration);

        /// <summary>Plays a strong haptic pulse (collision, boost, hard landing).</summary>
        public void PlayHeavy(float duration = -1f)
            => Play(heavyIntensity, duration < 0f ? defaultDuration * 1.5f : duration);

        /// <summary>
        /// Plays continuous asymmetric vibration — useful for engine rumble or turbulence.
        /// Stops any currently playing vibration.
        /// </summary>
        /// <param name="lowFrequency">Left / low-frequency motor intensity [0, 1].</param>
        /// <param name="highFrequency">Right / high-frequency motor intensity [0, 1].</param>
        /// <param name="duration">Duration in seconds.</param>
        public void PlayAsymmetric(float lowFrequency, float highFrequency, float duration)
        {
            if (!VibrationEnabled || masterIntensity <= 0f) return;
            StopVibration();
            float lo = lowFrequency  * masterIntensity;
            float hi = highFrequency * masterIntensity;
            _activeVibration = StartCoroutine(VibrationRoutine(lo, hi, duration));
        }

        /// <summary>
        /// Plays a vibration with <paramref name="intensity"/> for <paramref name="duration"/> seconds.
        /// Interrupts any currently active vibration.
        /// </summary>
        public void Play(float intensity, float duration)
        {
            if (!VibrationEnabled || masterIntensity <= 0f) return;
            StopVibration();
            float scaled = Mathf.Clamp01(intensity * masterIntensity);
            _activeVibration = StartCoroutine(VibrationRoutine(scaled, scaled, duration));
            OnHapticPlayed?.Invoke(scaled, duration);
        }

        /// <summary>Immediately stops any active vibration.</summary>
        public void StopVibration()
        {
            if (_activeVibration != null)
            {
                StopCoroutine(_activeVibration);
                _activeVibration = null;
            }
            SetMotors(0f, 0f);
        }

        #endregion

        #region Private — Vibration Coroutine

        private IEnumerator VibrationRoutine(float low, float high, float duration)
        {
            SetMotors(low, high);
            yield return new WaitForSecondsRealtime(Mathf.Max(duration, 0.016f));
            SetMotors(0f, 0f);
            _activeVibration = null;
        }

        /// <summary>
        /// Sets rumble motor speeds.  The actual rumble API call is platform-dependent;
        /// this project uses the Unity Input system so the call is a no-op stub unless
        /// the project imports an XInput / GameInput wrapper.
        /// </summary>
        private static void SetMotors(float low, float high)
        {
            // Stub — replace with platform-specific vibration API:
            //   e.g. GamePad.SetVibration(PlayerIndex.One, low, high)  (XInput / Unity.InputSystem)
            //        Handheld.Vibrate()                                (iOS/Android single pulse)
            // This default no-op keeps the system compiling without additional packages.
#if UNITY_ANDROID || UNITY_IOS
            if (low > 0.01f || high > 0.01f)
                Handheld.Vibrate();
#endif
        }

        #endregion
    }
}
