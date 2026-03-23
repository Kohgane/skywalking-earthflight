using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    // ── Enumerations ──────────────────────────────────────────────────────────────

    /// <summary>Lifecycle states of the recording subsystem.</summary>
    public enum RecordingState
    {
        /// <summary>No recording is active.</summary>
        Idle,
        /// <summary>Actively capturing frames.</summary>
        Recording,
        /// <summary>Capture is temporarily suspended.</summary>
        Paused,
        /// <summary>Recording has been finalised.</summary>
        Stopped
    }

    /// <summary>Available playback speed multipliers.</summary>
    public enum PlaybackSpeed
    {
        /// <summary>0.25× real-time.</summary>
        Quarter,
        /// <summary>0.5× real-time.</summary>
        Half,
        /// <summary>1× real-time.</summary>
        Normal,
        /// <summary>2× real-time.</summary>
        Double,
        /// <summary>4× real-time.</summary>
        Quadruple
    }

    /// <summary>Camera perspective modes available during replay.</summary>
    public enum CameraAngle
    {
        /// <summary>Third-person trailing camera that follows behind the aircraft.</summary>
        FollowCam,
        /// <summary>First-person view from inside the cockpit.</summary>
        CockpitCam,
        /// <summary>Dynamic chase camera whose distance scales with speed.</summary>
        ChaseCam,
        /// <summary>Camera that orbits around the aircraft.</summary>
        OrbitCam,
        /// <summary>Free-roaming camera independent of the aircraft.</summary>
        FreeCam,
        /// <summary>Auto-switching cinematic camera driven by flight events.</summary>
        CinematicCam
    }

    // ── Data Classes ──────────────────────────────────────────────────────────────

    /// <summary>
    /// A single captured snapshot of the aircraft state at one point in time.
    /// </summary>
    [System.Serializable]
    public class FlightFrame
    {
        /// <summary>World-space position of the aircraft.</summary>
        public Vector3 position;

        /// <summary>World-space rotation of the aircraft.</summary>
        public Quaternion rotation;

        /// <summary>Velocity vector in world space (m/s).</summary>
        public Vector3 velocity;

        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;

        /// <summary>Elapsed recording time in seconds at this frame.</summary>
        public float timestamp;

        /// <summary>Normalised throttle input [0, 1].</summary>
        public float throttle;

        /// <summary>Normalised pitch input [-1, 1].</summary>
        public float pitchInput;

        /// <summary>Normalised roll input [-1, 1].</summary>
        public float rollInput;

        /// <summary>Normalised yaw input [-1, 1].</summary>
        public float yawInput;

        /// <summary>Current airspeed scalar in m/s.</summary>
        public float speed;
    }

    /// <summary>
    /// A complete recorded flight session, including metadata and every
    /// captured <see cref="FlightFrame"/>.
    /// </summary>
    [System.Serializable]
    public class FlightRecording
    {
        // ── Identity ──────────────────────────────────────────────────────────
        /// <summary>Unique identifier (GUID) for this recording.</summary>
        public string recordingId;

        /// <summary>Display name of the pilot who recorded this flight.</summary>
        public string pilotName;

        /// <summary>Internal identifier of the aircraft used.</summary>
        public string aircraftType;

        /// <summary>ISO-8601 UTC timestamp when the recording was created.</summary>
        public string date;

        /// <summary>Total duration of the recording in seconds.</summary>
        public float duration;

        /// <summary>Name of the route or flight path (may be empty).</summary>
        public string routeName;

        // ── Frames ────────────────────────────────────────────────────────────
        /// <summary>All captured flight frames in chronological order.</summary>
        public List<FlightFrame> frames = new List<FlightFrame>();

        // ── Thumbnail ─────────────────────────────────────────────────────────
        /// <summary>Raw PNG bytes for a preview thumbnail.  May be <c>null</c>.</summary>
        public byte[] thumbnailData;

        // ── Derived statistics ────────────────────────────────────────────────
        /// <summary>Maximum altitude reached during the flight (metres).</summary>
        public float maxAltitude;

        /// <summary>Maximum speed reached during the flight (m/s).</summary>
        public float maxSpeed;

        /// <summary>Total distance flown in kilometres.</summary>
        public float totalDistanceKm;

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>Returns the number of captured frames.</summary>
        public int FrameCount => frames?.Count ?? 0;

        /// <summary>
        /// Looks up the frame nearest to <paramref name="time"/> using a
        /// binary search on <see cref="FlightFrame.timestamp"/>.
        /// </summary>
        public int FindFrameIndex(float time)
        {
            if (frames == null || frames.Count == 0) return -1;
            int lo = 0, hi = frames.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (frames[mid].timestamp < time) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }

    /// <summary>
    /// Persistent settings that control how recordings are captured.
    /// </summary>
    [System.Serializable]
    public class RecordingSettings
    {
        /// <summary>Number of frames captured per second (default 30).</summary>
        public float captureRate = 30f;

        /// <summary>Maximum recording length in seconds before old frames are trimmed.</summary>
        public float maxDuration = 300f;

        /// <summary>Quality level: 0 = low, 1 = medium, 2 = high.</summary>
        public int qualityLevel = 1;

        /// <summary>When <c>true</c> the recording is saved automatically on stop.</summary>
        public bool autoSave = true;
    }

    // ── Serialisation Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Utility class for converting <see cref="FlightRecording"/> objects to and
    /// from JSON, and for generating storage-friendly file names.
    /// </summary>
    public static class RecordingSerializer
    {
        private const string FilePrefix    = "SWEF_Recording_";
        private const string FileExtension = ".json";

        /// <summary>
        /// Serialises <paramref name="recording"/> to a JSON string.
        /// </summary>
        public static string ToJson(FlightRecording recording)
        {
            if (recording == null)
                throw new ArgumentNullException(nameof(recording));
            return JsonUtility.ToJson(recording, prettyPrint: false);
        }

        /// <summary>
        /// Deserialises a <see cref="FlightRecording"/> from a JSON string.
        /// </summary>
        public static FlightRecording FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            return JsonUtility.FromJson<FlightRecording>(json);
        }

        /// <summary>
        /// Builds the canonical file name for a recording using the project
        /// naming convention: <c>SWEF_Recording_{timestamp}_{aircraftType}.json</c>.
        /// </summary>
        public static string BuildFileName(FlightRecording recording)
        {
            if (recording == null)
                throw new ArgumentNullException(nameof(recording));
            string safe = SanitiseName(recording.aircraftType);
            string ts   = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{FilePrefix}{ts}_{safe}{FileExtension}";
        }

        // ── Private helpers ───────────────────────────────────────────────────────
        private static string SanitiseName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unknown";
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
