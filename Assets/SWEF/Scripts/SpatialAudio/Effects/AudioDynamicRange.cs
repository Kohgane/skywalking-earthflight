// AudioDynamicRange.cs — Phase 118: Spatial Audio & 3D Soundscape
// Dynamic range management: auto-ducking during radio calls, priority system.
// Namespace: SWEF.SpatialAudio

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages audio dynamic range: automatically ducks lower-priority audio sources
    /// when high-priority events (radio calls, warnings) are active. Uses a priority
    /// queue to determine the dominant audio event.
    /// </summary>
    public class AudioDynamicRange : MonoBehaviour
    {
        // ── Priority Levels ───────────────────────────────────────────────────────

        /// <summary>Priority levels for audio ducking decisions.</summary>
        public enum AudioPriority
        {
            /// <summary>Background ambient and environmental audio.</summary>
            Ambient    = 0,
            /// <summary>Engine and flight sounds.</summary>
            Flight     = 1,
            /// <summary>Radio communications and ATC.</summary>
            Radio      = 2,
            /// <summary>Safety-critical warning alerts.</summary>
            Warning    = 3
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Tracked Sources")]
        [Tooltip("Audio sources to duck when higher-priority audio is active.")]
        [SerializeField] private List<AudioSource> duckableSources = new List<AudioSource>();

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<AudioPriority, int> _activeCounts = new Dictionary<AudioPriority, int>();
        private float _currentDuckFactor = 1f;
        private bool  _isDucking;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (config == null || !config.enableAutoDucking) return;
            TickDucking();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers the start of a high-priority audio event.</summary>
        public void BeginHighPriorityEvent(AudioPriority priority)
        {
            if (!_activeCounts.ContainsKey(priority))
                _activeCounts[priority] = 0;
            _activeCounts[priority]++;
            UpdateDuckingState();
        }

        /// <summary>Registers the end of a high-priority audio event.</summary>
        public void EndHighPriorityEvent(AudioPriority priority)
        {
            if (!_activeCounts.ContainsKey(priority)) return;
            _activeCounts[priority] = Mathf.Max(0, _activeCounts[priority] - 1);
            UpdateDuckingState();
        }

        /// <summary>Returns the current duck volume factor (1 = no ducking, <1 = ducked).</summary>
        public float GetDuckFactor() => _currentDuckFactor;

        // ── Private ───────────────────────────────────────────────────────────────

        private void UpdateDuckingState()
        {
            int radioCount   = _activeCounts.TryGetValue(AudioPriority.Radio,   out int r) ? r : 0;
            int warningCount = _activeCounts.TryGetValue(AudioPriority.Warning, out int w) ? w : 0;
            _isDucking = radioCount > 0 || warningCount > 0;
        }

        private void TickDucking()
        {
            float target   = _isDucking ? (config != null ? config.duckingVolumeMultiplier : 0.3f) : 1f;
            float fadeRate = config != null ? 1f / Mathf.Max(0.01f, config.duckingFadeDuration) : 6.67f;

            _currentDuckFactor = Mathf.MoveTowards(_currentDuckFactor, target, fadeRate * Time.deltaTime);

            foreach (var src in duckableSources)
            {
                if (src == null) continue;
                // Store original volumes externally in a real system; here we apply factor
                src.volume = Mathf.Clamp01(src.volume * _currentDuckFactor /
                    (Mathf.Approximately(_currentDuckFactor, 0f) ? 1f : Mathf.Max(0.001f, _currentDuckFactor)));
            }
        }
    }
}
