// AdaptiveMusicAnalytics.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Telemetry collector for the Adaptive Music System.
    ///
    /// <para>Events dispatched:
    /// <list type="bullet">
    ///   <item><c>music_mood_changed</c> — whenever the active mood changes.</item>
    ///   <item><c>music_intensity_peak</c> — when intensity exceeds 0.9 for the first time per session.</item>
    ///   <item><c>music_stem_activated</c> — when a stem layer is activated.</item>
    ///   <item><c>music_user_override</c> — when the player manually overrides mood/intensity.</item>
    ///   <item><c>music_mode_selected</c> — when the player changes the music mode.</item>
    ///   <item><c>music_session_summary</c> — flushed when the application quits.</item>
    /// </list>
    /// </para>
    ///
    /// <para>All <see cref="TelemetryDispatcher"/> calls are null-safe.</para>
    /// </summary>
    public class AdaptiveMusicAnalytics : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [SerializeField] private AdaptiveMusicManager adaptiveManager;
        [SerializeField] private TelemetryDispatcher  telemetryDispatcher;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _sessionStartTime;
        private float _peakIntensity;
        private bool  _intensityPeakFired;

        private readonly Dictionary<MusicMood, float> _moodDurations = new Dictionary<MusicMood, float>();
        private MusicMood _currentMood;
        private float     _moodEnteredAt;
        private int       _userOverrideCount;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (adaptiveManager    == null) adaptiveManager    = FindFirstObjectByType<AdaptiveMusicManager>();
            if (telemetryDispatcher == null) telemetryDispatcher = FindFirstObjectByType<TelemetryDispatcher>();

            // Initialise mood duration tracking
            foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
                _moodDurations[mood] = 0f;
        }

        private void OnEnable()
        {
            if (adaptiveManager == null) return;
            adaptiveManager.OnMoodChanged     += HandleMoodChanged;
            adaptiveManager.OnIntensityChanged += HandleIntensityChanged;
            adaptiveManager.OnStemActivated   += HandleStemActivated;
        }

        private void OnDisable()
        {
            if (adaptiveManager == null) return;
            adaptiveManager.OnMoodChanged     -= HandleMoodChanged;
            adaptiveManager.OnIntensityChanged -= HandleIntensityChanged;
            adaptiveManager.OnStemActivated   -= HandleStemActivated;
        }

        private void Start()
        {
            _sessionStartTime = Time.realtimeSinceStartup;
            _currentMood      = adaptiveManager != null ? adaptiveManager.CurrentMood : MusicMood.Peaceful;
            _moodEnteredAt    = _sessionStartTime;
        }

        private void OnApplicationQuit()
        {
            FlushSessionSummary();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Tracks a manual mood/intensity override by the player.</summary>
        public void TrackUserOverride()
        {
            _userOverrideCount++;
            Dispatch("music_user_override", new Dictionary<string, object>
            {
                { "override_count", _userOverrideCount },
                { "mood",           _currentMood.ToString() }
            });
        }

        /// <summary>Tracks a music mode selection.</summary>
        public void TrackModeSelected(MusicMode mode)
        {
            Dispatch("music_mode_selected", new Dictionary<string, object>
            {
                { "mode", mode.ToString() }
            });
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleMoodChanged(MusicMood from, MusicMood to)
        {
            // Accumulate time in previous mood
            float elapsed = Time.realtimeSinceStartup - _moodEnteredAt;
            if (_moodDurations.ContainsKey(from))
                _moodDurations[from] += elapsed;

            _currentMood   = to;
            _moodEnteredAt = Time.realtimeSinceStartup;

            Dispatch("music_mood_changed", new Dictionary<string, object>
            {
                { "from", from.ToString() },
                { "to",   to.ToString()   }
            });
        }

        private void HandleIntensityChanged(float intensity)
        {
            if (intensity > _peakIntensity)
                _peakIntensity = intensity;

            if (!_intensityPeakFired && intensity >= 0.9f)
            {
                _intensityPeakFired = true;
                Dispatch("music_intensity_peak", new Dictionary<string, object>
                {
                    { "intensity", intensity },
                    { "mood",      _currentMood.ToString() }
                });
            }
        }

        private void HandleStemActivated(MusicLayer layer)
        {
            Dispatch("music_stem_activated", new Dictionary<string, object>
            {
                { "layer", layer.ToString() },
                { "mood",  _currentMood.ToString() }
            });
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void FlushSessionSummary()
        {
            // Finalise current mood duration
            float elapsed = Time.realtimeSinceStartup - _moodEnteredAt;
            if (_moodDurations.ContainsKey(_currentMood))
                _moodDurations[_currentMood] += elapsed;

            float totalTime = Time.realtimeSinceStartup - _sessionStartTime;

            MusicMood mostCommonMood = MusicMood.Peaceful;
            float     maxDuration    = -1f;
            foreach (var kvp in _moodDurations)
            {
                if (kvp.Value > maxDuration)
                {
                    maxDuration    = kvp.Value;
                    mostCommonMood = kvp.Key;
                }
            }

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "total_listening_time_s", totalTime },
                { "peak_intensity",         _peakIntensity },
                { "most_common_mood",       mostCommonMood.ToString() },
                { "user_override_count",    _userOverrideCount }
            };

            // Add mood distribution percentages
            foreach (var kvp in _moodDurations)
            {
                float pct = totalTime > 0f ? kvp.Value / totalTime * 100f : 0f;
                payload[$"mood_{kvp.Key.ToString().ToLower()}_pct"] = pct;
            }

            Dispatch("music_session_summary", payload);
        }

        private void Dispatch(string eventName, Dictionary<string, object> data)
        {
            if (telemetryDispatcher == null) return;

            TelemetryEvent evt = new TelemetryEvent
            {
                eventName  = eventName,
                properties = data
            };

            telemetryDispatcher.EnqueueEvent(evt);
        }
    }
}
