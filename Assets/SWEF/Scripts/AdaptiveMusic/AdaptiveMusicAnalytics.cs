// AdaptiveMusicAnalytics.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Observes <see cref="AdaptiveMusicManager"/> events and dispatches telemetry
    /// events to <c>SWEF.Analytics.TelemetryDispatcher</c> via a null-safe helper.
    ///
    /// Events reported:
    ///   music_mood_changed, music_intensity_peak, music_stem_activated,
    ///   music_user_override, music_mode_selected, music_session_summary
    /// </summary>
    public class AdaptiveMusicAnalytics : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────

        private float _sessionStartTime;
        private int   _moodChanges;
        private float _peakIntensity;
        private int   _stemActivations;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _sessionStartTime = Time.time;

            var mgr = AdaptiveMusicManager.Instance;
            if (mgr == null) return;

            mgr.OnMoodChanged      += OnMoodChanged;
            mgr.OnIntensityChanged += OnIntensityChanged;
            mgr.OnStemActivated    += OnStemActivated;
        }

        private void OnDisable()
        {
            var mgr = AdaptiveMusicManager.Instance;
            if (mgr == null) return;

            mgr.OnMoodChanged      -= OnMoodChanged;
            mgr.OnIntensityChanged -= OnIntensityChanged;
            mgr.OnStemActivated    -= OnStemActivated;
        }

        private void OnApplicationQuit()
        {
            FlushSessionSummary();
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnMoodChanged(MusicMood previous, MusicMood current)
        {
            _moodChanges++;
            EnqueueEvent("music_mood_changed", new Dictionary<string, object>
            {
                { "from",  previous.ToString() },
                { "to",    current.ToString()  },
                { "count", _moodChanges        },
            });
        }

        private void OnIntensityChanged(float intensity)
        {
            if (intensity > _peakIntensity)
            {
                _peakIntensity = intensity;
                if (intensity >= 0.9f)
                {
                    EnqueueEvent("music_intensity_peak", new Dictionary<string, object>
                    {
                        { "intensity", intensity },
                    });
                }
            }
        }

        private void OnStemActivated(MusicLayer layer)
        {
            _stemActivations++;
            EnqueueEvent("music_stem_activated", new Dictionary<string, object>
            {
                { "layer", layer.ToString() },
            });
        }

        /// <summary>Logs that the user manually changed the adaptive music mode.</summary>
        public void TrackModeSelected(string modeName)
        {
            EnqueueEvent("music_mode_selected", new Dictionary<string, object>
            {
                { "mode", modeName },
            });
        }

        /// <summary>Logs that the user manually overrode the adaptive intensity.</summary>
        public void TrackUserOverride(float intensity)
        {
            EnqueueEvent("music_user_override", new Dictionary<string, object>
            {
                { "intensity", intensity },
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void FlushSessionSummary()
        {
            float sessionDuration = Time.time - _sessionStartTime;
            EnqueueEvent("music_session_summary", new Dictionary<string, object>
            {
                { "duration_s",       sessionDuration  },
                { "mood_changes",     _moodChanges     },
                { "peak_intensity",   _peakIntensity   },
                { "stem_activations", _stemActivations },
            });
        }

        private static void EnqueueEvent(string eventName, Dictionary<string, object> data)
        {
#if SWEF_ANALYTICS_AVAILABLE
            try
            {
                SWEF.Analytics.TelemetryDispatcher.EnqueueEvent(eventName, data);
            }
            catch { /* analytics unavailable */ }
#endif
        }
    }
}
