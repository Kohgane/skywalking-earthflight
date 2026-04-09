using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Serializable data container for a complete SWEF flight replay (.swefr format).
    /// Stores all frame data and session metadata required for playback, ghost racing,
    /// and path visualization.
    /// </summary>
    [System.Serializable]
    public class ReplayData
    {
        // ── Metadata ──────────────────────────────────────────────────────────────
        /// <summary>Unique identifier (GUID) for this replay.</summary>
        public string replayId;

        /// <summary>Display name of the player who recorded this replay.</summary>
        public string playerName;

        /// <summary>ISO-8601 timestamp when the replay was recorded (<c>DateTime.UtcNow.ToString("o")</c>).</summary>
        public string recordedAt;

        /// <summary>Replay file format version.</summary>
        public int version = 1;

        /// <summary>ISO-8601 timestamp of when the replay was created.</summary>
        public string createdAt;

        /// <summary>Raw PNG bytes for an embedded preview thumbnail.  May be null.</summary>
        public byte[] thumbnailPng;

        /// <summary>Total duration of the replay in seconds.</summary>
        public float totalDurationSec;

        /// <summary>Maximum altitude reached during the replay (metres).</summary>
        public float maxAltitudeM;

        /// <summary>Maximum speed reached during the replay (m/s).</summary>
        public float maxSpeedMps;

        /// <summary>Total flight distance in kilometres.</summary>
        public float totalDistanceKm;

        /// <summary>Starting latitude of the flight.</summary>
        public double startLat;

        /// <summary>Starting longitude of the flight.</summary>
        public double startLon;

        /// <summary>Human-readable name of the starting location.</summary>
        public string startLocationName;

        /// <summary>Ordered list of recorded frames.</summary>
        public List<ReplayFrame> frames = new List<ReplayFrame>();

        // ── Serialization ─────────────────────────────────────────────────────────

        /// <summary>Serializes this instance to a pretty-printed JSON string.</summary>
        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Deserializes a JSON string into a <see cref="ReplayData"/> instance.</summary>
        /// <param name="json">The JSON string to parse.</param>
        public static ReplayData FromJson(string json) => JsonUtility.FromJson<ReplayData>(json);

        /// <summary>
        /// Serializes this instance to the <c>.swef-replay</c> package JSON format.
        /// </summary>
        /// <returns>JSON string representing the full package payload.</returns>
        public string ToSwefReplayPackage()
        {
            var wrapper = new SwefReplayPackage { schemaVersion = 1, replayData = this };
            return JsonUtility.ToJson(wrapper, true);
        }

        /// <summary>
        /// Deserializes a <c>.swef-replay</c> package JSON string and returns the
        /// embedded <see cref="ReplayData"/>.
        /// </summary>
        /// <param name="json">JSON package string.</param>
        /// <returns>The embedded <see cref="ReplayData"/>, or <c>null</c> on failure.</returns>
        public static ReplayData FromSwefReplayPackage(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<SwefReplayPackage>(json);
                return wrapper?.replayData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SWEF] ReplayData.FromSwefReplayPackage: {e.Message}");
                return null;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the total duration derived from frame timestamps.
        /// Falls back to zero when there are no frames.
        /// </summary>
        public float GetDuration() => frames.Count > 0 ? frames[frames.Count - 1].time : 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Wrapper used for the <c>.swef-replay</c> package file format.</summary>
    [System.Serializable]
    internal class SwefReplayPackage
    {
        /// <summary>Package schema version (currently 1).</summary>
        public int        schemaVersion;
        /// <summary>The embedded replay data.</summary>
        public ReplayData replayData;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single snapshot of flight state captured at a specific point in time.
    /// </summary>
    [System.Serializable]
    public class ReplayFrame
    {
        /// <summary>Seconds elapsed since the start of the replay.</summary>
        public float time;

        /// <summary>X component of the position (local to georeference).</summary>
        public float px;

        /// <summary>Y component of the position (local to georeference).</summary>
        public float py;

        /// <summary>Z component of the position (local to georeference).</summary>
        public float pz;

        /// <summary>X component of the rotation quaternion.</summary>
        public float rx;

        /// <summary>Y component of the rotation quaternion.</summary>
        public float ry;

        /// <summary>Z component of the rotation quaternion.</summary>
        public float rz;

        /// <summary>W component of the rotation quaternion.</summary>
        public float rw;

        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;

        /// <summary>Speed in metres per second.</summary>
        public float speed;

        /// <summary>Returns the position as a <see cref="Vector3"/>.</summary>
        public Vector3 Position => new Vector3(px, py, pz);

        /// <summary>Returns the rotation as a <see cref="Quaternion"/>.</summary>
        public Quaternion Rotation => new Quaternion(rx, ry, rz, rw);
    }
}
