using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    // ── Data types ────────────────────────────────────────────────────────────────

    /// <summary>A single step in a haptic feedback pattern.</summary>
    [Serializable]
    public struct HapticStep
    {
        /// <summary>Normalised vibration intensity (0–1).</summary>
        [Range(0f, 1f)] public float intensity;

        /// <summary>Duration of this vibration burst in seconds.</summary>
        [Range(0.01f, 2f)] public float duration;

        /// <summary>Pause after this burst before the next step, in seconds.</summary>
        [Range(0f, 2f)] public float pause;

        public HapticStep(float intensity, float duration, float pause)
        {
            this.intensity = intensity;
            this.duration  = duration;
            this.pause     = pause;
        }
    }

    /// <summary>A named haptic pattern composed of a sequence of <see cref="HapticStep"/>s.</summary>
    [Serializable]
    public class HapticPattern
    {
        /// <summary>Unique identifier for this pattern.</summary>
        public string name;

        /// <summary>Steps that make up the pattern.</summary>
        public HapticStep[] steps;

        public HapticPattern(string name, HapticStep[] steps)
        {
            this.name  = name;
            this.steps = steps;
        }
    }

    /// <summary>
    /// Extends the base <see cref="SWEF.Haptic.HapticManager"/> with
    /// accessibility-focused haptic patterns — haptic substitution for visual cues,
    /// audio-to-haptic conversion, and an enhanced pattern library.
    /// </summary>
    public class HapticAccessibility : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static HapticAccessibility Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyEnabled   = "SWEF_HapticA11yEnabled";
        private const string KeyIntensity = "SWEF_HapticA11yIntensity";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Global Settings")]
        [SerializeField] private bool  hapticEnabled           = true;
        [SerializeField] [Range(0f, 2f)] private float intensityMultiplier = 1f;

        [Header("Audio-to-Haptic")]
        [SerializeField] private bool audioToHapticEnabled = false;

        // ── Built-in pattern library ─────────────────────────────────────────────
        private readonly Dictionary<string, HapticPattern> _patterns =
            new Dictionary<string, HapticPattern>(StringComparer.Ordinal);

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Coroutine _currentPattern;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a named pattern begins playing.</summary>
        public event Action<string> OnPatternPlayed;

        /// <summary>Fired when the global intensity multiplier changes.</summary>
        public event Action<float> OnHapticIntensityChanged;

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
            RegisterBuiltInPatterns();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Plays a registered haptic pattern by name.</summary>
        /// <param name="patternName">Exact name as registered in the pattern library.</param>
        public void Play(string patternName)
        {
            if (!hapticEnabled || intensityMultiplier <= 0f) return;
            if (!_patterns.TryGetValue(patternName, out HapticPattern pattern))
            {
                Debug.LogWarning($"[SWEF HapticA11y] Pattern not found: '{patternName}'");
                return;
            }
            Play(pattern);
        }

        /// <summary>Plays a <see cref="HapticPattern"/> directly.</summary>
        public void Play(HapticPattern pattern)
        {
            if (!hapticEnabled || pattern == null) return;
            if (_currentPattern != null) StopCoroutine(_currentPattern);
            _currentPattern = StartCoroutine(RunPattern(pattern));
            OnPatternPlayed?.Invoke(pattern.name);
        }

        /// <summary>Stops any currently playing pattern.</summary>
        public void Stop()
        {
            if (_currentPattern != null)
            {
                StopCoroutine(_currentPattern);
                _currentPattern = null;
            }
            Handheld.StopPlayingVibration();
        }

        /// <summary>Enables or disables all haptic feedback.</summary>
        public void SetEnabled(bool enabled)
        {
            hapticEnabled = enabled;
            if (!enabled) Stop();
            PlayerPrefs.SetInt(KeyEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets the global intensity multiplier (0–200%).</summary>
        public void SetIntensityMultiplier(float multiplier)
        {
            intensityMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
            PlayerPrefs.SetFloat(KeyIntensity, intensityMultiplier);
            PlayerPrefs.Save();
            OnHapticIntensityChanged?.Invoke(intensityMultiplier);
        }

        /// <summary>Enables or disables audio-to-haptic conversion.</summary>
        public void SetAudioToHaptic(bool enabled) => audioToHapticEnabled = enabled;

        /// <summary>
        /// Called by the audio system when a sound event occurs.
        /// Converts it to a haptic pulse when audio-to-haptic is active.
        /// </summary>
        public void OnAudioEvent(string eventName, float amplitude)
        {
            if (!audioToHapticEnabled || !hapticEnabled) return;
            float scaled = Mathf.Clamp01(amplitude * intensityMultiplier);
            TriggerSinglePulse(scaled, 0.08f);
        }

        /// <summary>Registers a custom named pattern in the library.</summary>
        public void RegisterPattern(HapticPattern pattern)
        {
            if (pattern == null || string.IsNullOrEmpty(pattern.name)) return;
            _patterns[pattern.name] = pattern;
        }

        // ── Built-in patterns ────────────────────────────────────────────────────
        private void RegisterBuiltInPatterns()
        {
            Register("Waypoint_Near",       new[] {
                new HapticStep(0.3f, 0.1f, 0.1f), new HapticStep(0.5f, 0.1f, 0.1f), new HapticStep(0.8f, 0.15f, 0f)
            });
            Register("Stall_Warning",       new[] {
                new HapticStep(1.0f, 0.05f, 0.05f), new HapticStep(1.0f, 0.05f, 0.05f),
                new HapticStep(1.0f, 0.05f, 0.05f), new HapticStep(1.0f, 0.05f, 0.3f)
            });
            Register("Altitude_Low",        new[] {
                new HapticStep(0.6f, 0.15f, 0.15f), new HapticStep(0.8f, 0.15f, 0.15f), new HapticStep(1.0f, 0.3f, 0f)
            });
            Register("Formation_Drift",     new[] {
                new HapticStep(0.4f, 0.08f, 0.12f), new HapticStep(0.4f, 0.08f, 0.12f)
            });
            Register("Mission_Complete",    new[] {
                new HapticStep(0.5f, 0.1f, 0.05f), new HapticStep(0.7f, 0.1f, 0.05f), new HapticStep(1.0f, 0.3f, 0f)
            });
            Register("Collision_Warning",   new[] {
                new HapticStep(1.0f, 0.1f, 0.05f), new HapticStep(1.0f, 0.1f, 0.05f),
                new HapticStep(1.0f, 0.1f, 0.05f), new HapticStep(1.0f, 0.5f, 0f)
            });
            Register("Turbulence",          new[] {
                new HapticStep(0.6f, 0.05f, 0.03f), new HapticStep(0.9f, 0.05f, 0.03f),
                new HapticStep(0.5f, 0.05f, 0.03f), new HapticStep(0.8f, 0.05f, 0.03f)
            });
            Register("Landing_Gear",        new[] {
                new HapticStep(0.4f, 0.2f, 0.1f), new HapticStep(0.6f, 0.15f, 0.1f), new HapticStep(0.3f, 0.1f, 0f)
            });
            Register("Rhythm_Formation",    new[] {
                new HapticStep(0.5f, 0.1f, 0.1f), new HapticStep(0.5f, 0.1f, 0.1f),
                new HapticStep(0.5f, 0.1f, 0.1f), new HapticStep(0.5f, 0.1f, 0.3f)
            });
        }

        private void Register(string name, HapticStep[] steps) =>
            _patterns[name] = new HapticPattern(name, steps);

        // ── Pattern execution ─────────────────────────────────────────────────────
        private IEnumerator RunPattern(HapticPattern pattern)
        {
            foreach (var step in pattern.steps)
            {
                float scaledIntensity = Mathf.Clamp01(step.intensity * intensityMultiplier);
                TriggerSinglePulse(scaledIntensity, step.duration);
                yield return new WaitForSeconds(step.duration);
                if (step.pause > 0f) yield return new WaitForSeconds(step.pause);
            }
            _currentPattern = null;
        }

        private static void TriggerSinglePulse(float intensity, float duration)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Note: Unity's Handheld.Vibrate() does not support a duration parameter on Android.
            // Duration control requires direct JNI calls to VibrationEffect, which is platform-specific.
            Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            Debug.Log($"[SWEF HapticA11y] Pulse — intensity={intensity:P0}, duration={duration:F2}s");
#endif
        }

        private void LoadPreferences()
        {
            hapticEnabled       = PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
            intensityMultiplier = PlayerPrefs.GetFloat(KeyIntensity, 1f);
        }
    }
}
