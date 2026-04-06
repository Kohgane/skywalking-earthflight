// LandingChallengeData.cs — Phase 120: Precision Landing Challenge System
// Enums and data models for the Precision Landing Challenge System.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    // ── Challenge Type ─────────────────────────────────────────────────────────

    /// <summary>Types of precision landing challenges available.</summary>
    public enum ChallengeType
    {
        Standard,
        CarrierLanding,
        MountainApproach,
        CrosswindLanding,
        ShortField,
        WaterLanding,
        NightLanding,
        EmergencyLanding,
        FormationLanding
    }

    // ── Difficulty Level ───────────────────────────────────────────────────────

    /// <summary>Difficulty levels for landing challenges.</summary>
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Legendary
    }

    // ── Scoring Category ──────────────────────────────────────────────────────

    /// <summary>Categories evaluated during landing scoring.</summary>
    public enum ScoringCategory
    {
        CenterlineAccuracy,
        TouchdownZone,
        GlideSlopeAdherence,
        SpeedControl,
        SinkRate,
        Smoothness
    }

    // ── Landing Grade ─────────────────────────────────────────────────────────

    /// <summary>Overall grade assigned to a landing attempt.</summary>
    public enum LandingGrade
    {
        Perfect,
        Excellent,
        Good,
        Fair,
        Poor,
        Crash
    }

    // ── Challenge Status ──────────────────────────────────────────────────────

    /// <summary>Current status of a challenge.</summary>
    public enum ChallengeStatus
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Failed
    }

    // ── LSO Grade ─────────────────────────────────────────────────────────────

    /// <summary>Landing Signal Officer grading for carrier landings.</summary>
    public enum LSOGrade
    {
        OK,
        Fair,
        NoGrade,
        CutPass,
        WaveOff
    }

    // ── Weather Preset ────────────────────────────────────────────────────────

    /// <summary>Fixed weather conditions for challenge scenarios.</summary>
    public enum WeatherPreset
    {
        Clear,
        PartlyCloudy,
        Overcast,
        LightRain,
        HeavyRain,
        Fog,
        Thunderstorm,
        Crosswind,
        Gusting,
        Blizzard
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data classes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Result data from a single landing approach attempt.</summary>
    [Serializable]
    public class LandingResult
    {
        /// <summary>Overall weighted score (0–1000).</summary>
        public float TotalScore;

        /// <summary>Individual category scores keyed by <see cref="ScoringCategory"/>.</summary>
        public Dictionary<ScoringCategory, float> CategoryScores = new Dictionary<ScoringCategory, float>();

        /// <summary>Overall landing grade.</summary>
        public LandingGrade Grade;

        /// <summary>Whether the landing was within the designated touchdown zone.</summary>
        public bool InTouchdownZone;

        /// <summary>Centerline deviation at touchdown in metres.</summary>
        public float CenterlineDeviationMetres;

        /// <summary>Vertical speed at touchdown in feet-per-minute.</summary>
        public float SinkRateFPM;

        /// <summary>Airspeed at touchdown in knots.</summary>
        public float TouchdownSpeedKnots;

        /// <summary>Number of bounces recorded.</summary>
        public int BounceCount;

        /// <summary>Whether a go-around was performed.</summary>
        public bool WentAround;

        /// <summary>UTC timestamp of the landing attempt.</summary>
        public DateTime Timestamp;

        /// <summary>Star rating awarded (0–3).</summary>
        public int Stars;
    }

    /// <summary>Definition of a single landing challenge instance.</summary>
    [Serializable]
    public class ChallengeDefinition
    {
        /// <summary>Unique challenge identifier.</summary>
        public string ChallengeId;

        /// <summary>Display name shown in the challenge browser.</summary>
        public string DisplayName;

        /// <summary>Brief description of the challenge scenario.</summary>
        public string Description;

        /// <summary>Type of landing challenge.</summary>
        public ChallengeType Type;

        /// <summary>Difficulty level.</summary>
        public DifficultyLevel Difficulty;

        /// <summary>ICAO code of the destination airport.</summary>
        public string AirportICAO;

        /// <summary>Active runway identifier (e.g. "09L").</summary>
        public string RunwayId;

        /// <summary>Required weather preset for this challenge.</summary>
        public WeatherPreset Weather;

        /// <summary>Score thresholds for 1-, 2-, and 3-star awards.</summary>
        public float[] StarThresholds = { 600f, 800f, 950f };

        /// <summary>Whether this challenge is part of the daily rotation.</summary>
        public bool IsDaily;

        /// <summary>Prerequisite challenge IDs that must be completed first.</summary>
        public List<string> Prerequisites = new List<string>();
    }

    /// <summary>Persistent player progress data for landing challenges.</summary>
    [Serializable]
    public class ChallengeProgress
    {
        /// <summary>Challenge definition ID.</summary>
        public string ChallengeId;

        /// <summary>Current status.</summary>
        public ChallengeStatus Status;

        /// <summary>Best score achieved.</summary>
        public float BestScore;

        /// <summary>Best grade achieved.</summary>
        public LandingGrade BestGrade;

        /// <summary>Stars earned (0–3).</summary>
        public int StarsEarned;

        /// <summary>Total number of attempts.</summary>
        public int AttemptCount;

        /// <summary>Date of last attempt.</summary>
        public DateTime LastAttempt;
    }

    /// <summary>Touchdown event data captured at the moment of contact.</summary>
    [Serializable]
    public class TouchdownData
    {
        /// <summary>World-space position of touchdown point.</summary>
        public Vector3 Position;

        /// <summary>Aircraft speed at touchdown (knots).</summary>
        public float SpeedKnots;

        /// <summary>Vertical speed at touchdown (feet per minute, negative = descending).</summary>
        public float VerticalSpeedFPM;

        /// <summary>Bank angle at touchdown (degrees).</summary>
        public float BankAngleDeg;

        /// <summary>Crab angle at touchdown (degrees).</summary>
        public float CrabAngleDeg;

        /// <summary>G-force at impact.</summary>
        public float GForce;

        /// <summary>Offset from centreline in metres (positive = right).</summary>
        public float CentrelineOffsetMetres;

        /// <summary>Offset from designated touchdown point along runway (metres, positive = long).</summary>
        public float ThresholdDistanceMetres;
    }

    /// <summary>Approach quality snapshot used by the approach analyser.</summary>
    [Serializable]
    public class ApproachSnapshot
    {
        /// <summary>UTC time of snapshot.</summary>
        public DateTime Time;

        /// <summary>Glideslope deviation (dots, positive = above).</summary>
        public float GlideSlopeDots;

        /// <summary>Localiser deviation (dots, positive = right).</summary>
        public float LocaliserDots;

        /// <summary>Airspeed (knots).</summary>
        public float SpeedKnots;

        /// <summary>Target approach speed (knots).</summary>
        public float TargetSpeedKnots;

        /// <summary>Whether gear is down and locked.</summary>
        public bool GearDown;

        /// <summary>Flap setting index.</summary>
        public int FlapSetting;

        /// <summary>Altitude (feet).</summary>
        public float AltitudeFeet;
    }

    /// <summary>Frame data recorded for landing replay playback.</summary>
    [Serializable]
    public class ReplayFrame
    {
        /// <summary>Time offset from replay start (seconds).</summary>
        public float TimeOffset;

        /// <summary>Aircraft world position.</summary>
        public Vector3 Position;

        /// <summary>Aircraft rotation.</summary>
        public Quaternion Rotation;

        /// <summary>Airspeed (knots).</summary>
        public float SpeedKnots;

        /// <summary>Altitude (feet).</summary>
        public float AltitudeFeet;

        /// <summary>Gear state (0 = up, 1 = down).</summary>
        public int GearState;

        /// <summary>Flap setting index.</summary>
        public int FlapSetting;
    }
}
