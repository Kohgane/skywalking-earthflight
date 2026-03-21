using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.MusicPlayer
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Source of a music track — local file or a streaming service.</summary>
    public enum MusicSource
    {
        Local,
        Spotify,
        YouTubeMusic,
        AppleMusic
    }

    /// <summary>Current playback state of the music player.</summary>
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused,
        Buffering,
        Error
    }

    /// <summary>Repeat/loop mode for the music player queue.</summary>
    public enum RepeatMode
    {
        None,
        RepeatOne,
        RepeatAll
    }

    // ── MusicTrack ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Immutable data record describing a single music track from any supported source.
    /// <para>
    /// <see cref="trackId"/> is globally unique and follows the format
    /// <c>{source}_{id}</c>, e.g. <c>Spotify_7ouMYWpwJ422jRcDASZB7P</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public class MusicTrack
    {
        /// <summary>
        /// Globally unique identifier in the format <c>{source}_{id}</c>,
        /// e.g. <c>Spotify_7ouMYWpwJ422jRcDASZB7P</c> or <c>Local_my_song</c>.
        /// </summary>
        public string trackId;

        /// <summary>Display title of the track.</summary>
        public string title;

        /// <summary>Artist name(s).</summary>
        public string artist;

        /// <summary>Album name.</summary>
        public string album;

        /// <summary>Total duration of the track in seconds.</summary>
        public float durationSeconds;

        /// <summary>URL pointing to the cover-art image for this track.</summary>
        public string artworkUrl;

        /// <summary>The service (or local filesystem) from which this track originates.</summary>
        public MusicSource source;

        /// <summary>
        /// The track's identifier within its originating streaming service,
        /// e.g. a Spotify track ID or a YouTube video ID.
        /// </summary>
        public string sourceTrackId;

        /// <summary>
        /// Resolved playback URL used for streaming or local file serving.
        /// May be <c>null</c> or empty for streaming services that resolve URLs
        /// at playback time via their SDK.
        /// </summary>
        public string streamUrl;

        /// <summary>
        /// Absolute path to the audio file on the local device.
        /// Only populated when <see cref="source"/> is <see cref="MusicSource.Local"/>.
        /// </summary>
        public string localFilePath;

        /// <summary>
        /// Whether this track has been cached for offline playback.
        /// </summary>
        public bool isAvailableOffline;

        /// <summary>
        /// Arbitrary key/value metadata (e.g. genre, BPM, explicit flag).
        /// Not serialized to JSON automatically — callers must handle this field manually.
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> metadata;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a canonical <see cref="trackId"/> from a source and a service-specific ID.
        /// </summary>
        public static string BuildTrackId(MusicSource source, string id) =>
            $"{source}_{id}";

        /// <summary>
        /// Returns a human-readable duration string in <c>m:ss</c> format.
        /// </summary>
        public string FormattedDuration()
        {
            int totalSeconds = Mathf.RoundToInt(durationSeconds);
            return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
        }

        /// <summary>
        /// Returns <c>true</c> when the track comes from an external streaming service
        /// (i.e. not a local file).
        /// </summary>
        public bool IsStreaming => source != MusicSource.Local;
    }
}
