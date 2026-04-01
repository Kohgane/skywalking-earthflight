// AdaptiveMusicData.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Enumerations
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Emotional mood state that drives stem selection and mixing.</summary>
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

    /// <summary>Individual stem layer within the adaptive music mix.</summary>
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

    // ─────────────────────────────────────────────────────────────────────────────
    // Stem Definition
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a single audio stem: which clip to play, which layer it belongs to,
    /// which mood it serves, and musical metadata for beat-synchronisation.
    /// </summary>
    [Serializable]
    public class StemDefinition
    {
        [Tooltip("Path to the AudioClip asset (Resources-relative or Addressable address).")]
        public string audioClipPath = "";

        [Tooltip("Runtime reference resolved from audioClipPath at startup.")]
        [NonSerialized] public AudioClip audioClip;

        [Tooltip("Which mix layer this stem occupies.")]
        public MusicLayer layer = MusicLayer.Pads;

        [Tooltip("Mood this stem is intended for.")]
        public MusicMood mood = MusicMood.Peaceful;

        [Tooltip("Beats per minute of the stem. Used for bar-quantised scheduling.")]
        public float bpm = 120f;

        [Tooltip("Musical key (e.g. \"C\", \"Am\", \"F#m\").")]
        public string key = "C";

        [Tooltip("Sample position at which the loop starts.")]
        public int loopStartSample = 0;

        [Tooltip("Sample position at which the loop ends (0 = end of clip).")]
        public int loopEndSample = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Transition Rule
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Custom crossfade duration and optional stinger for a specific mood pair.</summary>
    [Serializable]
    public class MoodTransitionRule
    {
        public MusicMood fromMood = MusicMood.Peaceful;
        public MusicMood toMood   = MusicMood.Cruising;

        [Tooltip("Crossfade duration in seconds for this specific pair.")]
        [Min(0.1f)] public float crossfadeDuration = 3f;

        [Tooltip("Optional stinger clip played at the moment of transition.")]
        public AudioClip stingerClip;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Adaptive Music Profile (ScriptableObject)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject that bundles all stems, transition rules, intensity curves,
    /// and crossfade durations for one adaptive music profile (e.g. "Standard", "Cinematic").
    /// </summary>
    [CreateAssetMenu(fileName = "AdaptiveMusicProfile", menuName = "SWEF/AdaptiveMusic/Profile", order = 1)]
    public class AdaptiveMusicProfile : ScriptableObject
    {
        [Header("Stems")]
        [Tooltip("All stem definitions available in this profile.")]
        public List<StemDefinition> stems = new List<StemDefinition>();

        [Header("Transitions")]
        [Tooltip("Override rules for specific mood-to-mood crossfade durations.")]
        public List<MoodTransitionRule> transitionRules = new List<MoodTransitionRule>();

        [Tooltip("Default crossfade duration used when no specific rule exists.")]
        [Min(0.1f)] public float defaultCrossfadeDuration = 3f;

        [Header("Intensity Curves")]
        [Tooltip("Intensity-to-volume curve for each layer. Index matches MusicLayer enum.")]
        public AnimationCurve[] layerIntensityCurves = new AnimationCurve[8];

        [Header("Timing")]
        [Tooltip("Minimum time (seconds) a mood must hold before switching to prevent flicker.")]
        [Min(1f)] public float minimumMoodHoldTime = 8f;

        [Tooltip("Number of AudioSources in the stem pool.")]
        [Range(2, 16)] public int stemPoolSize = 8;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the crossfade duration for the given transition pair.
        /// Falls back to <see cref="defaultCrossfadeDuration"/> when no specific rule exists.
        /// </summary>
        public float GetCrossfadeDuration(MusicMood from, MusicMood to)
        {
            foreach (MoodTransitionRule rule in transitionRules)
            {
                if (rule.fromMood == from && rule.toMood == to)
                    return rule.crossfadeDuration;
            }
            return defaultCrossfadeDuration;
        }

        /// <summary>Returns the stinger clip for a transition pair, or null if none defined.</summary>
        public AudioClip GetStinger(MusicMood from, MusicMood to)
        {
            foreach (MoodTransitionRule rule in transitionRules)
            {
                if (rule.fromMood == from && rule.toMood == to)
                    return rule.stingerClip;
            }
            return null;
        }

        /// <summary>Returns all stems matching the given mood.</summary>
        public List<StemDefinition> GetStemsForMood(MusicMood mood)
        {
            List<StemDefinition> result = new List<StemDefinition>();
            foreach (StemDefinition stem in stems)
            {
                if (stem.mood == mood)
                    result.Add(stem);
            }
            return result;
        }

        /// <summary>Returns the stem for a specific mood and layer, or null if not found.</summary>
        public StemDefinition GetStem(MusicMood mood, MusicLayer layer)
        {
            foreach (StemDefinition stem in stems)
            {
                if (stem.mood == mood && stem.layer == layer)
                    return stem;
            }
            return null;
        }

        /// <summary>
        /// Returns the intensity-to-volume curve for a given layer.
        /// Falls back to a linear curve if not configured.
        /// </summary>
        public AnimationCurve GetLayerCurve(MusicLayer layer)
        {
            int index = (int)layer;
            if (layerIntensityCurves != null && index < layerIntensityCurves.Length
                && layerIntensityCurves[index] != null
                && layerIntensityCurves[index].length > 0)
            {
                return layerIntensityCurves[index];
            }
            return AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Flight Music Context
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of all flight-state data used by <see cref="MoodResolver"/> to determine
    /// the current mood and intensity. Built each tick by <c>FlightContextAnalyzer</c>.
    /// </summary>
    public struct FlightMusicContext
    {
        // ── Flight ────────────────────────────────────────────────────────────────
        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;

        /// <summary>Aircraft speed in metres per second.</summary>
        public float speed;

        /// <summary>G-force currently experienced by the aircraft (1.0 = normal gravity).</summary>
        public float gForce;

        /// <summary>Whether the aircraft is currently flying (airborne).</summary>
        public bool isFlying;

        // ── Weather ───────────────────────────────────────────────────────────────
        /// <summary>Normalised weather intensity 0–1 (0 = calm, 1 = extreme storm).</summary>
        public float weatherIntensity;

        /// <summary>True if a storm is in close proximity.</summary>
        public bool inStorm;

        // ── Time ──────────────────────────────────────────────────────────────────
        /// <summary>Simulated hour of day (0–23.99).</summary>
        public float timeOfDay;

        /// <summary>Sun altitude in degrees above the horizon (negative = below).</summary>
        public float sunAltitudeDeg;

        // ── World ─────────────────────────────────────────────────────────────────
        /// <summary>Danger level 0–1 (0 = safe, 1 = critical emergency).</summary>
        public float dangerLevel;

        /// <summary>Biome identifier string (e.g. "Forest", "Desert", "Ocean").</summary>
        public string biomeType;

        // ── Mission ───────────────────────────────────────────────────────────────
        /// <summary>True while a transport/cargo mission is active.</summary>
        public bool isInMission;

        /// <summary>True immediately after a mission is completed (within a short window).</summary>
        public bool missionJustCompleted;

        // ── Status ────────────────────────────────────────────────────────────────
        /// <summary>True if an active emergency is in progress.</summary>
        public bool hasActiveEmergency;

        /// <summary>Damage proportion 0–1 (0 = intact, 1 = fully destroyed).</summary>
        public float damageLevel;

        /// <summary>True when a stall warning is active.</summary>
        public bool stallWarning;

        /// <summary>True when the aircraft is on approach / landing.</summary>
        public bool isLanding;

        /// <summary>True when above 100,000 m (Kármán line).</summary>
        public bool isInSpace;

        // ── Factory ───────────────────────────────────────────────────────────────

        /// <summary>Returns a context struct populated with safe default values.</summary>
        public static FlightMusicContext Default()
        {
            return new FlightMusicContext
            {
                altitude          = 0f,
                speed             = 0f,
                gForce            = 1f,
                isFlying          = false,
                weatherIntensity  = 0f,
                inStorm           = false,
                timeOfDay         = 12f,
                sunAltitudeDeg    = 45f,
                dangerLevel       = 0f,
                biomeType         = "Unknown",
                isInMission       = false,
                missionJustCompleted = false,
                hasActiveEmergency = false,
                damageLevel       = 0f,
                stallWarning      = false,
                isLanding         = false,
                isInSpace         = false
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Music Mode (user preference)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>User-selectable music mode that controls adaptive vs. playlist behaviour.</summary>
    public enum MusicMode
    {
        /// <summary>Only adaptive music plays; user playlist is paused.</summary>
        AdaptiveOnly = 0,

        /// <summary>Only the user's playlist plays; adaptive music is disabled.</summary>
        PlaylistOnly = 1,

        /// <summary>Adaptive plays during flight; playlist takes over in menus.</summary>
        Hybrid = 2
    }
}
