// AdaptiveMusicData.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Emotional/contextual mood states the adaptive music system can be in.</summary>
    public enum MusicMood
    {
        Peaceful,
        Cruising,
        Adventurous,
        Tense,
        Danger,
        Epic,
        Serene,
        Mysterious,
        Triumphant
    }

    /// <summary>Instrument/stem layer within the adaptive music mix.</summary>
    public enum MusicLayer
    {
        Drums,
        Bass,
        Melody,
        Pads,
        Strings,
        Percussion,
        Choir,
        Synth
    }

    // ── Structs ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of flight-related parameters used to determine music mood and intensity.
    /// All fields have sensible defaults so callers that cannot populate every value
    /// still produce a valid context.
    /// </summary>
    [Serializable]
    public struct FlightMusicContext
    {
        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;

        /// <summary>Airspeed in m/s.</summary>
        public float speed;

        /// <summary>Current G-force (positive = pull-up).</summary>
        public float gForce;

        /// <summary>Normalised weather intensity (0 = clear, 1 = extreme storm).</summary>
        [Range(0f, 1f)]
        public float weatherIntensity;

        /// <summary>Hour of day in 24-hour float format (e.g. 6.5 = 06:30).</summary>
        [Range(0f, 24f)]
        public float timeOfDay;

        /// <summary>Normalised danger level (0 = safe, 1 = imminent crash/emergency).</summary>
        [Range(0f, 1f)]
        public float dangerLevel;

        /// <summary>Numeric biome identifier from <c>BiomeClassifier</c>.</summary>
        public int biomeType;

        /// <summary>Whether a transport/survey/combat mission is currently active.</summary>
        public bool isInMission;

        /// <summary>Whether the aircraft is inside a declared combat/danger zone.</summary>
        public bool isInCombatZone;

        /// <summary>Whether a landing sequence is in progress.</summary>
        public bool isLanding;

        /// <summary>Whether the aircraft is above the Kármán line (>100 km altitude).</summary>
        public bool isInSpace;

        /// <summary>Whether the mission was completed within the last 10 seconds.</summary>
        public bool missionJustCompleted;

        /// <summary>Whether a stall warning is currently active.</summary>
        public bool stallWarning;

        /// <summary>Hull/aircraft damage expressed as a fraction (0 = intact, 1 = destroyed).</summary>
        [Range(0f, 1f)]
        public float damageLevel;

        /// <summary>Sun altitude in degrees above the horizon (negative = below).</summary>
        public float sunAltitudeDeg;

        /// <summary>Returns a <see cref="FlightMusicContext"/> filled with safe default values.</summary>
        public static FlightMusicContext Default()
        {
            return new FlightMusicContext
            {
                altitude           = 0f,
                speed              = 0f,
                gForce             = 1f,
                weatherIntensity   = 0f,
                timeOfDay          = 12f,
                dangerLevel        = 0f,
                biomeType          = 0,
                isInMission        = false,
                isInCombatZone     = false,
                isLanding          = false,
                isInSpace          = false,
                missionJustCompleted = false,
                stallWarning       = false,
                damageLevel        = 0f,
                sunAltitudeDeg     = 45f,
            };
        }
    }

    // ── Classes ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a single audio stem (loop) within an <see cref="AdaptiveMusicProfile"/>.
    /// </summary>
    [Serializable]
    public class StemDefinition
    {
        [Tooltip("Path to the AudioClip asset relative to a Resources folder.")]
        public string audioClipResourcePath = string.Empty;

        [Tooltip("Which instrument layer this stem belongs to.")]
        public MusicLayer layer = MusicLayer.Pads;

        [Tooltip("Mood this stem is associated with.")]
        public MusicMood mood = MusicMood.Peaceful;

        [Tooltip("Beats per minute of the stem loop.")]
        [Range(40f, 240f)]
        public float bpm = 80f;

        [Tooltip("Musical key identifier (e.g. 'Cmaj', 'Dmin').")]
        public string key = "Cmaj";

        [Tooltip("Start sample of the seamless loop region within the clip.")]
        public int loopStartSample = 0;

        [Tooltip("End sample of the seamless loop region within the clip (-1 = use clip length).")]
        public int loopEndSample = -1;
    }

    /// <summary>
    /// Defines a pair of moods involved in a transition rule.
    /// </summary>
    [Serializable]
    public class MoodTransitionRule
    {
        public MusicMood from;
        public MusicMood to;

        [Tooltip("Crossfade duration in seconds for this specific mood transition.")]
        [Range(0.1f, 30f)]
        public float crossfadeDuration = 3f;

        [Tooltip("Optional stinger clip resource path played when this transition fires.")]
        public string stingerResourcePath = string.Empty;
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// Project-wide configuration for the Adaptive Music system.
    /// Create via <c>Assets → Create → SWEF → Adaptive Music Profile</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Adaptive Music Profile", fileName = "AdaptiveMusicProfile")]
    public class AdaptiveMusicProfile : ScriptableObject
    {
        [Header("Stems")]
        [Tooltip("All available audio stems in this profile.")]
        public List<StemDefinition> stems = new List<StemDefinition>();

        [Header("Transitions")]
        [Tooltip("Per-pair mood transition rules. Pairs not listed use defaultCrossfadeDuration.")]
        public List<MoodTransitionRule> transitionRules = new List<MoodTransitionRule>();

        [Tooltip("Default crossfade duration in seconds when no specific rule is defined.")]
        [Range(0.1f, 30f)]
        public float defaultCrossfadeDuration = 3f;

        [Tooltip("Minimum time (seconds) a mood must be active before being replaced by another.")]
        [Range(1f, 60f)]
        public float minimumMoodDuration = 8f;

        [Header("Intensity Curves")]
        [Tooltip("Per-layer volume curve driven by overall intensity (0–1 on X, 0–1 volume on Y).")]
        public AnimationCurve drumsIntensityCurve   = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve bassIntensityCurve    = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve melodyIntensityCurve  = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve padsIntensityCurve    = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve stringsIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve percussionIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve choirIntensityCurve   = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve synthIntensityCurve   = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>
        /// Looks up the crossfade duration for the given mood pair, falling back to the
        /// profile default if no specific rule is registered.
        /// </summary>
        public float GetCrossfadeDuration(MusicMood from, MusicMood to)
        {
            if (transitionRules != null)
            {
                foreach (var rule in transitionRules)
                {
                    if (rule.from == from && rule.to == to)
                        return rule.crossfadeDuration;
                }
            }
            return defaultCrossfadeDuration;
        }

        /// <summary>Returns the stinger resource path for the given transition, or empty string.</summary>
        public string GetStingerPath(MusicMood from, MusicMood to)
        {
            if (transitionRules != null)
            {
                foreach (var rule in transitionRules)
                {
                    if (rule.from == from && rule.to == to)
                        return rule.stingerResourcePath ?? string.Empty;
                }
            }
            return string.Empty;
        }

        /// <summary>Returns the intensity curve for the given layer.</summary>
        public AnimationCurve GetLayerCurve(MusicLayer layer)
        {
            switch (layer)
            {
                case MusicLayer.Drums:      return drumsIntensityCurve;
                case MusicLayer.Bass:       return bassIntensityCurve;
                case MusicLayer.Melody:     return melodyIntensityCurve;
                case MusicLayer.Pads:       return padsIntensityCurve;
                case MusicLayer.Strings:    return stringsIntensityCurve;
                case MusicLayer.Percussion: return percussionIntensityCurve;
                case MusicLayer.Choir:      return choirIntensityCurve;
                case MusicLayer.Synth:      return synthIntensityCurve;
                default:                    return padsIntensityCurve;
            }
        }
    }
}
