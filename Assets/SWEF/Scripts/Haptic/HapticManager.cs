using System;
using System.Collections;
using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace SWEF.Haptic
{
    /// <summary>
    /// Singleton MonoBehaviour that centralises all haptic/vibration feedback for SWEF.
    /// Survives scene loads via DontDestroyOnLoad.
    /// Supports iOS Taptic Engine, Android VibrationEffect, and an Editor log-only stub.
    /// Settings (enabled / intensity) are persisted in PlayerPrefs and synchronised with
    /// <see cref="SWEF.Settings.SettingsManager"/> at runtime.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static HapticManager Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyHapticsEnabled  = "SWEF_HapticsEnabled";
        private const string KeyHapticIntensity = "SWEF_HapticIntensity";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Defaults")]
        [SerializeField] private bool  hapticsEnabled  = true;
        [SerializeField] [Range(0f, 1f)] private float hapticIntensity = 1.0f;

        // ── Public properties ────────────────────────────────────────────────────
        /// <summary>Whether haptic feedback is globally enabled.</summary>
        public bool HapticsEnabled  => hapticsEnabled;

        /// <summary>Intensity multiplier applied to all patterns (0–1).</summary>
        public float HapticIntensity => hapticIntensity;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever a haptic pattern is triggered. Useful for analytics and debug overlays.</summary>
        public static event Action<HapticPattern> OnHapticTriggered;

        // ── Continuous haptic state ──────────────────────────────────────────────
        private Coroutine _continuousRoutine;

        // ── iOS native bindings ──────────────────────────────────────────────────
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _TriggerHaptic(int style);
#endif

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPreferences();
            SubscribeToSettings();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSettings();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers a one-shot haptic feedback pattern.
        /// No-ops when haptics are disabled or intensity is zero.
        /// </summary>
        /// <param name="pattern">The pattern to play.</param>
        public void Trigger(HapticPattern pattern)
        {
            if (!hapticsEnabled || hapticIntensity <= 0f) return;

            OnHapticTriggered?.Invoke(pattern);

            var (duration, amplitude) = GetPatternParams(pattern);
            PlayHaptic(duration, amplitude);

            Debug.Log($"[SWEF] HapticManager: {pattern} ({duration} ms, amp {amplitude})");
        }

        /// <summary>
        /// Begins a repeating haptic effect for ongoing events (e.g. boost).
        /// Call <see cref="StopContinuous"/> to cancel.
        /// </summary>
        /// <param name="pattern">The pattern to repeat.</param>
        /// <param name="duration">Total duration in seconds to repeat the pattern.</param>
        public void TriggerContinuous(HapticPattern pattern, float duration)
        {
            StopContinuous();
            _continuousRoutine = StartCoroutine(ContinuousRoutine(pattern, duration));
        }

        /// <summary>Cancels any currently running continuous haptic effect.</summary>
        public void StopContinuous()
        {
            if (_continuousRoutine != null)
            {
                StopCoroutine(_continuousRoutine);
                _continuousRoutine = null;
            }
        }

        /// <summary>Enables or disables haptic feedback and persists the preference.</summary>
        public void SetHapticsEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
            PlayerPrefs.SetInt(KeyHapticsEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets the haptic intensity multiplier (0–1) and persists the preference.</summary>
        public void SetHapticIntensity(float intensity)
        {
            hapticIntensity = Mathf.Clamp01(intensity);
            PlayerPrefs.SetFloat(KeyHapticIntensity, hapticIntensity);
            PlayerPrefs.Save();
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void LoadPreferences()
        {
            hapticsEnabled  = PlayerPrefs.GetInt(KeyHapticsEnabled,  hapticsEnabled  ? 1 : 0) == 1;
            hapticIntensity = PlayerPrefs.GetFloat(KeyHapticIntensity, hapticIntensity);
        }

        private void SubscribeToSettings()
        {
            SWEF.Settings.SettingsManager.OnHapticsSettingChanged   += SetHapticsEnabled;
            SWEF.Settings.SettingsManager.OnHapticIntensityChanged  += SetHapticIntensity;
        }

        private void UnsubscribeFromSettings()
        {
            SWEF.Settings.SettingsManager.OnHapticsSettingChanged   -= SetHapticsEnabled;
            SWEF.Settings.SettingsManager.OnHapticIntensityChanged  -= SetHapticIntensity;
        }

        /// <summary>
        /// Returns the (durationMs, amplitude) pair for a given <see cref="HapticPattern"/>.
        /// Complex patterns (multi-pulse) are handled inside <see cref="PlayHaptic"/>.
        /// </summary>
        private (long durationMs, int amplitude) GetPatternParams(HapticPattern pattern)
        {
            switch (pattern)
            {
                case HapticPattern.Light:              return (10,  80);
                case HapticPattern.Medium:             return (25, 128);
                case HapticPattern.Heavy:              return (50, 255);
                case HapticPattern.Success:            return (15, 180);   // double-tap handled separately
                case HapticPattern.Warning:            return (40, 128);
                case HapticPattern.Error:              return (80, 255);
                case HapticPattern.DoubleTap:          return (15, 200);
                case HapticPattern.RapidPulse:         return (10,  80);
                case HapticPattern.AltitudeWarning:    return (20, 180);   // 3× short pulses
                case HapticPattern.TeleportComplete:   return (10, 128);   // rising 3-step
                case HapticPattern.ScreenshotSnap:     return (15, 200);
                case HapticPattern.AchievementUnlock:  return (10, 128);   // rising 5-step
                case HapticPattern.Boost:              return (10,  80);
                case HapticPattern.Stall:              return (40, 255);   // heavy double-pulse
                default:                               return (20, 128);
            }
        }

        private void PlayHaptic(long durationMs, int amplitude)
        {
            // Scale amplitude by intensity
            int scaledAmp = Mathf.RoundToInt(amplitude * hapticIntensity);
            if (scaledAmp <= 0) return;

#if UNITY_IOS && !UNITY_EDITOR
            // Map amplitude to iOS UIImpactFeedbackStyle: 0=light, 1=medium, 2=heavy
            int style = scaledAmp < 100 ? 0 : scaledAmp < 200 ? 1 : 2;
            _TriggerHaptic(style);
#elif UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroid(durationMs, scaledAmp);
#else
            // Editor / unsupported platform — log only
            Debug.Log($"[SWEF] HapticManager (stub): duration={durationMs}ms amp={scaledAmp}");
#endif
        }

#if UNITY_ANDROID
        private static void TriggerAndroid(long durationMs, int amplitude)
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator == null) return;

                using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                    "createOneShot", durationMs, amplitude);
                vibrator.Call("vibrate", effect);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] HapticManager Android: {ex.Message}");
            }
        }
#endif

        private IEnumerator ContinuousRoutine(HapticPattern pattern, float totalDuration)
        {
            float elapsed = 0f;
            var (durationMs, amplitude) = GetPatternParams(pattern);
            float intervalSec = durationMs / 1000f + 0.05f; // pulse + 50 ms gap

            while (elapsed < totalDuration)
            {
                Trigger(pattern);
                yield return new WaitForSeconds(intervalSec);
                elapsed += intervalSec;
            }

            _continuousRoutine = null;
        }
    }
}
