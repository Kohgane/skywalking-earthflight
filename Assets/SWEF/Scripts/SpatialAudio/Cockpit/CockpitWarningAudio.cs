// CockpitWarningAudio.cs — Phase 118: Spatial Audio & 3D Soundscape
// Warning audio system: stall horn, gear warning, altitude callouts, GPWS, overspeed.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages cockpit warning audio: stall horn, gear warning, GPWS callouts
    /// ("terrain, pull up"), altitude callouts, and overspeed clacker.
    /// Warning audio always plays at maximum priority.
    /// </summary>
    public class CockpitWarningAudio : MonoBehaviour
    {
        // ── Warning Identifiers ───────────────────────────────────────────────────

        /// <summary>Type of cockpit warning audio event.</summary>
        public enum WarningType
        {
            /// <summary>Aerodynamic stall warning horn.</summary>
            StallHorn,
            /// <summary>Gear not down for landing warning.</summary>
            GearWarning,
            /// <summary>GPWS terrain proximity warning.</summary>
            GPWS,
            /// <summary>Overspeed clacker.</summary>
            Overspeed,
            /// <summary>Altitude deviation alert.</summary>
            AltitudeAlert,
            /// <summary>Autopilot disconnect warning.</summary>
            AutopilotDisconnect
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Warning Sources")]
        [SerializeField] private AudioSource warningLoopSource;
        [SerializeField] private AudioSource warningOneShotSource;

        [Header("Warning Clips")]
        [SerializeField] private AudioClip stallHornClip;
        [SerializeField] private AudioClip gearWarningClip;
        [SerializeField] private AudioClip gpwsClip;
        [SerializeField] private AudioClip overspeedClip;
        [SerializeField] private AudioClip altitudeAlertClip;
        [SerializeField] private AudioClip autopilotDisconnectClip;

        // ── State ─────────────────────────────────────────────────────────────────

        private WarningType? _activeLoopWarning;

        /// <summary>Currently active looping warning, or null if none.</summary>
        public WarningType? ActiveLoopWarning => _activeLoopWarning;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Activates a looping warning (e.g., stall horn, gear warning).
        /// Only one looping warning is active at a time; higher priority replaces lower.
        /// </summary>
        public void TriggerLoopingWarning(WarningType warning)
        {
            _activeLoopWarning = warning;
            float vol = config != null ? config.warningVolume : 1f;

            if (warningLoopSource != null)
            {
                AudioClip clip = GetClip(warning);
                if (clip != null)
                {
                    warningLoopSource.clip   = clip;
                    warningLoopSource.volume = vol;
                    warningLoopSource.loop   = true;
                    warningLoopSource.Play();
                }
            }
        }

        /// <summary>Stops the currently active looping warning.</summary>
        public void StopLoopingWarning()
        {
            _activeLoopWarning = null;
            if (warningLoopSource != null) warningLoopSource.Stop();
        }

        /// <summary>
        /// Plays a one-shot warning tone (e.g., altitude callout, autopilot disconnect).
        /// </summary>
        public void TriggerOneShotWarning(WarningType warning)
        {
            float vol = config != null ? config.warningVolume : 1f;
            AudioClip clip = GetClip(warning);
            if (warningOneShotSource != null && clip != null)
                warningOneShotSource.PlayOneShot(clip, vol);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private AudioClip GetClip(WarningType warning)
        {
            switch (warning)
            {
                case WarningType.StallHorn:           return stallHornClip;
                case WarningType.GearWarning:         return gearWarningClip;
                case WarningType.GPWS:                return gpwsClip;
                case WarningType.Overspeed:           return overspeedClip;
                case WarningType.AltitudeAlert:       return altitudeAlertClip;
                case WarningType.AutopilotDisconnect: return autopilotDisconnectClip;
                default:                              return null;
            }
        }
    }
}
