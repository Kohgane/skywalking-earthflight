// RaceCourseData.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CompetitiveRacing
{
    // ── RaceCheckpoint ────────────────────────────────────────────────────────────

    /// <summary>
    /// A single gate/waypoint on a race course.  Stored as part of
    /// <see cref="RaceCourse"/> and used at runtime by
    /// <see cref="CheckpointGateController"/>.
    /// </summary>
    [Serializable]
    public class RaceCheckpoint
    {
        [Tooltip("Unique identifier for this checkpoint within the course.")]
        public string checkpointId;

        [Tooltip("Zero-based order index along the course.")]
        public int index;

        [Tooltip("Functional role of this checkpoint.")]
        public CheckpointType type;

        // ── Geo Position ──────────────────────────────────────────────────────────

        [Tooltip("Latitude in decimal degrees.")]
        public double latitude;

        [Tooltip("Longitude in decimal degrees.")]
        public double longitude;

        [Tooltip("Altitude above sea level (metres).")]
        public float altitude;

        // ── Gate Shape ────────────────────────────────────────────────────────────

        [Tooltip("Radius (metres) within which the player triggers the checkpoint.")]
        [Min(10f)]
        public float triggerRadius = CompetitiveRacingConfig.DefaultCheckpointTriggerRadius;

        [Tooltip("Visual gate width (metres).")]
        [Min(5f)]
        public float gateWidth = 100f;

        [Tooltip("Visual gate height (metres).")]
        [Min(5f)]
        public float gateHeight = 60f;

        [Tooltip("World-space orientation of the gate ring/arch.")]
        public Quaternion gateRotation = Quaternion.identity;

        // ── Timing ────────────────────────────────────────────────────────────────

        [Tooltip("Target split time in seconds from race start; 0 = no target.")]
        [Min(0f)]
        public float targetTime;

        [Tooltip("Seconds added to (positive) or subtracted from (negative) total time on capture.")]
        public float bonusSeconds;

        // ── Flags ─────────────────────────────────────────────────────────────────

        [Tooltip("If true the checkpoint may be skipped without penalty.")]
        public bool isOptional;
    }

    // ── RaceCourse ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Full definition of a race course including all checkpoints and metadata.
    /// Wrapped by <see cref="RaceCourseData"/> when authored as a Unity asset.
    /// </summary>
    [Serializable]
    public class RaceCourse
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Tooltip("Globally unique identifier (GUID).")]
        public string courseId;

        [Tooltip("Display name shown in menus.")]
        public string courseName;

        [Tooltip("Short description shown on the course detail screen.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Author user identifier.")]
        public string authorId;

        [Tooltip("Author display name.")]
        public string authorName;

        // ── Mode & Environment ────────────────────────────────────────────────────

        [Tooltip("Default race mode this course was designed for.")]
        public RaceMode defaultMode = RaceMode.TimeTrial;

        [Tooltip("Difficulty rating of the course.")]
        public CourseDifficulty difficulty = CourseDifficulty.Intermediate;

        [Tooltip("Primary terrain environment of the course.")]
        public CourseEnvironment environment = CourseEnvironment.Mixed;

        // ── Checkpoints ───────────────────────────────────────────────────────────

        [Tooltip("Ordered list of checkpoint gates.")]
        public List<RaceCheckpoint> checkpoints = new List<RaceCheckpoint>();

        // ── Circuit ───────────────────────────────────────────────────────────────

        [Tooltip("Number of laps for circuit mode; 1 = sprint.")]
        [Min(1)]
        public int lapCount = 1;

        [Tooltip("True if the last checkpoint connects back to the first checkpoint.")]
        public bool isLoop;

        // ── Timing / Distances ────────────────────────────────────────────────────

        [Tooltip("Estimated finish time in seconds for an average pilot.")]
        public float estimatedTimeSeconds;

        [Tooltip("Total gate-to-gate distance in metres.")]
        public float totalDistanceMeters;

        // ── Medal Thresholds ──────────────────────────────────────────────────────

        [Tooltip("Time threshold for gold medal (seconds).")]
        public float goldTime;

        [Tooltip("Time threshold for silver medal (seconds).")]
        public float silverTime;

        [Tooltip("Time threshold for bronze medal (seconds).")]
        public float bronzeTime;

        // ── Dates ─────────────────────────────────────────────────────────────────

        public DateTime createdDate;
        public DateTime lastModifiedDate;

        // ── Community Stats ───────────────────────────────────────────────────────

        [Tooltip("Total number of times this course has been played.")]
        public int playCount;

        [Tooltip("Running average community rating (1–5).")]
        public float averageRating;

        [Tooltip("Number of ratings received.")]
        public int ratingCount;

        // ── Sharing ───────────────────────────────────────────────────────────────

        [Tooltip("Short alphanumeric share code used to import the course on another device.")]
        public string shareCode;
    }

    // ── RaceCourseData ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 88 — ScriptableObject wrapper for a <see cref="RaceCourse"/> asset.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/CompetitiveRacing/Race Course Data</em>.
    /// Assign a <see cref="coursePreview"/> sprite to display a thumbnail in the
    /// <see cref="CompetitiveRacingUI"/> course browser.</para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/CompetitiveRacing/Race Course Data",
        fileName = "NewRaceCourse")]
    public class RaceCourseData : ScriptableObject
    {
        [Tooltip("The race course definition authored in this asset.")]
        public RaceCourse course = new RaceCourse();

        [Tooltip("Optional thumbnail shown in the course browser UI.")]
        public Sprite coursePreview;
    }
}
