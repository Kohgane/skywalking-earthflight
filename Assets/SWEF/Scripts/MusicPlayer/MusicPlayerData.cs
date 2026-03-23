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
        /// <summary>No repeat — stop after the last track.</summary>
        Off,
        /// <summary>Repeat the current track indefinitely.</summary>
        One,
        /// <summary>Repeat all tracks in the playlist.</summary>
        All
    }

    // ── ShuffleMode ───────────────────────────────────────────────────────────────

    /// <summary>Shuffle mode for playlist ordering.</summary>
    public enum ShuffleMode
    {
        /// <summary>Tracks play in their natural playlist order.</summary>
        Off,
        /// <summary>Tracks are played in random order (Fisher-Yates shuffle).</summary>
        On,
        /// <summary>Energy-aware shuffle — matches track energy to current flight intensity.</summary>
        Smart
    }

    // ── EqualizerPreset ───────────────────────────────────────────────────────────

    /// <summary>Built-in equalizer presets for music playback.</summary>
    public enum EqualizerPreset
    {
        Flat,
        BassBoost,
        TrebleBoost,
        Vocal,
        Electronic,
        Classical,
        /// <summary>Compensates for wind and engine noise at high speeds.</summary>
        Flight
    }

    // ── MusicMood ─────────────────────────────────────────────────────────────────

    /// <summary>Emotional mood tag for a music track or current flight state.</summary>
    public enum MusicMood
    {
        Calm,
        Energetic,
        Epic,
        Melancholic,
        Mysterious,
        Peaceful,
        Adventurous,
        Dramatic
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

        /// <summary>Music genre (e.g. "Electronic", "Ambient", "Classical").</summary>
        public string genre;

        /// <summary>Beats per minute of the track.</summary>
        public float bpm;

        /// <summary>
        /// Perceived energy level in the range 0–1.
        /// 0 = very calm/ambient, 1 = very intense/high-energy.
        /// </summary>
        [Range(0f, 1f)]
        public float energy;

        /// <summary>List of mood descriptors for this track. May be empty.</summary>
        public List<MusicMood> moodTags;

        /// <summary>Local file path to the audio clip (Unity Resources or StreamingAssets).</summary>
        public string audioClipPath;

        /// <summary>Local file path to the album art image.</summary>
        public string albumArtPath;

        /// <summary>Whether this track has been unlocked for playback.</summary>
        public bool isUnlocked = true;

        /// <summary>Whether the player has marked this track as a favourite.</summary>
        public bool isFavorite;

        /// <summary>
        /// Optional path to an LRC lyrics file for this track.
        /// May be absolute, relative to <c>StreamingAssets</c>, or left empty.
        /// Used by <c>LyricsDatabase</c> to locate embedded lyrics.
        /// </summary>
        public string lrcFilePath;

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

    // ── MusicPlaylist ─────────────────────────────────────────────────────────────

    /// <summary>
    /// An ordered collection of track IDs representing a named playlist.
    /// </summary>
    [Serializable]
    public class MusicPlaylist
    {
        /// <summary>Globally unique playlist identifier.</summary>
        public string playlistId;

        /// <summary>Display name of the playlist.</summary>
        public string name;

        /// <summary>Optional description shown in the library UI.</summary>
        public string description;

        /// <summary>Ordered list of <see cref="MusicTrack.trackId"/> values.</summary>
        public List<string> trackIds = new List<string>();

        /// <summary>ISO-8601 creation date string.</summary>
        public string createdDate;

        /// <summary>True when created by the player (vs. a built-in/system playlist).</summary>
        public bool isUserCreated;
    }

    // ── MusicPlayerConfig ─────────────────────────────────────────────────────────

    /// <summary>
    /// Persisted configuration for the in-flight music player.
    /// Serialized to/from PlayerPrefs as JSON.
    /// </summary>
    [Serializable]
    public class MusicPlayerConfig
    {
        /// <summary>Master volume multiplier (0–1).</summary>
        [Range(0f, 1f)] public float masterVolume = 1f;

        /// <summary>Music-specific volume (0–1), multiplied by <see cref="masterVolume"/>.</summary>
        [Range(0f, 1f)] public float musicVolume = 0.8f;

        /// <summary>
        /// Volume multiplier applied to AudioManager's BGM while the music player is active.
        /// 0 = full duck, 1 = no duck.
        /// </summary>
        [Range(0f, 1f)] public float sfxDuckingAmount = 0.3f;

        /// <summary>Duration in seconds for crossfade transitions between tracks.</summary>
        [Range(0f, 10f)] public float crossfadeDuration = 2f;

        /// <summary>If true, music starts automatically when a flight begins.</summary>
        public bool autoPlayOnFlight = true;

        /// <summary>Current shuffle mode.</summary>
        public ShuffleMode shuffleMode = ShuffleMode.Off;

        /// <summary>Current repeat mode.</summary>
        public RepeatMode repeatMode = RepeatMode.Off;

        /// <summary>Active equalizer preset.</summary>
        public EqualizerPreset equalizerPreset = EqualizerPreset.Flat;

        /// <summary>Whether automatic flight-to-mood synchronization is enabled.</summary>
        public bool flightSyncEnabled = true;

        /// <summary>Whether weather-based ambient mixing is enabled.</summary>
        public bool weatherMixEnabled = true;

        /// <summary>Whether the audio visualizer effect is enabled.</summary>
        public bool visualizerEnabled = true;
    }

    // ── FlightMusicProfile ────────────────────────────────────────────────────────

    /// <summary>
    /// Maps flight parameters (altitude, speed, time of day) to <see cref="MusicMood"/> values.
    /// Configure thresholds in the Inspector on the <c>MusicFlightSync</c> component.
    /// </summary>
    [Serializable]
    public class FlightMusicProfile
    {
        [Header("Altitude Thresholds (metres)")]
        [Tooltip("Below this altitude the mood is Adventurous.")]
        public float lowAltitudeMax = 500f;
        [Tooltip("Above this altitude the mood shifts to Epic or Peaceful.")]
        public float highAltitudeMin = 8000f;

        [Header("Speed Thresholds (m/s)")]
        [Tooltip("Below this speed the mood is Calm.")]
        public float calmSpeedMax = 50f;
        [Tooltip("Above this speed the mood is Energetic.")]
        public float energeticSpeedMin = 150f;

        [Header("Time-of-Day Ranges (0–24 h)")]
        public float dawnStart  = 5f;
        public float dawnEnd    = 7f;
        public float duskStart  = 18f;
        public float duskEnd    = 20f;
        /// <summary>Hour at which night begins (wraps around midnight).</summary>
        public float nightStart = 21f;
        /// <summary>Hour at which night ends (early morning).</summary>
        public float nightEnd   = 4f;
    }

    // ── MusicPlayerState ─────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime snapshot of the music player's playback state.
    /// Persisted to PlayerPrefs so it survives scene reloads.
    /// </summary>
    [Serializable]
    public class MusicPlayerState
    {
        /// <summary>Track ID currently loaded (may differ from the playing track during crossfade).</summary>
        public string currentTrackId;

        /// <summary>Playlist ID that is currently active. Empty for ad-hoc queue playback.</summary>
        public string currentPlaylistId;

        /// <summary>True when the music player is actively playing audio.</summary>
        public bool isPlaying;

        /// <summary>True when playback has been paused.</summary>
        public bool isPaused;

        /// <summary>Normalised playback position in the current track (0–1).</summary>
        [Range(0f, 1f)] public float playbackPosition;

        /// <summary>Current music volume (0–1).</summary>
        [Range(0f, 1f)] public float volume = 0.8f;

        /// <summary>Active shuffle mode mirrored from <see cref="MusicPlayerConfig"/>.</summary>
        public ShuffleMode shuffleMode = ShuffleMode.Off;

        /// <summary>Active repeat mode mirrored from <see cref="MusicPlayerConfig"/>.</summary>
        public RepeatMode repeatMode = RepeatMode.Off;
    }
}
