using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Social
{
    /// <summary>
    /// Serializable data class representing a single social feed post.
    /// Supports JSON round-trip via <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class SocialPost
    {
        public string postId;
        public string authorName;
        public string imagePath;
        public string thumbnailPath;
        public string caption;
        public double latitude;
        public double longitude;
        public float  altitude;
        /// <summary>ISO 8601 timestamp string for JSON serialisation.</summary>
        public string timestamp;
        public int    likeCount;
        public bool   isLikedByMe;
        public string flightDataId;
        public string weatherCondition;
        public List<string> tags = new List<string>();

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Serialises this post to a JSON string using <see cref="JsonUtility"/>.</summary>
        public string ToJson() => JsonUtility.ToJson(this, prettyPrint: true);

        /// <summary>Deserialises a <see cref="SocialPost"/> from a JSON string.</summary>
        public static SocialPost FromJson(string json) => JsonUtility.FromJson<SocialPost>(json);

        /// <summary>
        /// Returns a human-readable relative timestamp such as "2m ago", "1h ago", or "3d ago".
        /// Falls back to the raw timestamp string if parsing fails.
        /// </summary>
        public string GetFormattedTimestamp()
        {
            if (!DateTime.TryParse(timestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime ts))
                return timestamp;

            TimeSpan delta = DateTime.UtcNow - ts;
            if (delta.TotalSeconds < 60)  return $"{(int)delta.TotalSeconds}s ago";
            if (delta.TotalMinutes < 60)  return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours   < 24)  return $"{(int)delta.TotalHours}h ago";
            if (delta.TotalDays    < 30)  return $"{(int)delta.TotalDays}d ago";
            // Approximation: 30 days/month, 365 days/year — sufficient for display purposes
            if (delta.TotalDays    < 365) return $"{(int)(delta.TotalDays / 30)}mo ago";
            return $"{(int)(delta.TotalDays / 365)}y ago";
        }

        /// <summary>Returns a formatted "lat, lon" coordinate string.</summary>
        public string GetLocationString()
        {
            string latStr = latitude  >= 0 ? $"{latitude:F4}°N"  : $"{-latitude:F4}°S";
            string lonStr = longitude >= 0 ? $"{longitude:F4}°E" : $"{-longitude:F4}°W";
            return $"{latStr}, {lonStr}";
        }
    }
}
