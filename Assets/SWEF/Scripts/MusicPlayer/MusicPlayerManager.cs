using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Audio;
using SWEF.Flight;
using SWEF.Settings;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Central singleton manager for the In-Flight Music Player system.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Maintains a track library and playlist registry.</item>
    ///   <item>Drives a dedicated <see cref="AudioSource"/> for music playback.</item>
    ///   <item>Ducks <see cref="AudioManager"/>'s BGM while active.</item>
    ///   <item>Crossfades between tracks using <see cref="MusicPlayerConfig.crossfadeDuration"/>.</item>
    ///   <item>Persists state to PlayerPrefs under the <c>SWEF_Music_</c> key prefix.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class MusicPlayerManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MusicPlayerManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [Tooltip("Dedicated AudioSource for music playback. Created automatically if null.")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("FlightController reference. Auto-found if null.")]
        [SerializeField] private FlightController flightController;

        [Tooltip("AltitudeController reference. Auto-found if null.")]
        [SerializeField] private AltitudeController altitudeController;

        [Tooltip("AudioManager reference. Auto-found if null.")]
        [SerializeField] private AudioManager audioManager;

        [Tooltip("SettingsManager reference. Auto-found if null.")]
        [SerializeField] private SettingsManager settingsManager;

        [Header("Configuration")]
        [Tooltip("Player configuration (volumes, modes, feature flags).")]
        [SerializeField] private MusicPlayerConfig config = new MusicPlayerConfig();

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyPrefix        = "SWEF_Music_";
        private const string KeyVolume        = KeyPrefix + "Volume";
        private const string KeyShuffle       = KeyPrefix + "Shuffle";
        private const string KeyRepeat        = KeyPrefix + "Repeat";
        private const string KeyCurrentTrack  = KeyPrefix + "CurrentTrack";
        private const string KeyCurrentList   = KeyPrefix + "CurrentPlaylist";

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly Dictionary<string, MusicTrack>    _trackLibrary = new Dictionary<string, MusicTrack>();
        private readonly Dictionary<string, MusicPlaylist> _playlists    = new Dictionary<string, MusicPlaylist>();
        private readonly MusicPlaylistController           _playlistCtrl = new MusicPlaylistController();

        private MusicPlayerState _state      = new MusicPlayerState();
        private PlaybackState    _playbackState = PlaybackState.Stopped;
        private bool             _wasFlying  = false;
        private Coroutine        _crossfadeCoroutine;
        private float            _bgmOriginalVolume = 1f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the active track changes.</summary>
        public event Action<MusicTrack> OnTrackChanged;

        /// <summary>Fired when the playback state (playing/paused/stopped) changes.</summary>
        public event Action<PlaybackState> OnPlaybackStateChanged;

        /// <summary>Fired when the active playlist changes.</summary>
        public event Action<MusicPlaylist> OnPlaylistChanged;

        /// <summary>Fired when the music volume changes.</summary>
        public event Action<float> OnVolumeChanged;

        /// <summary>
        /// Fired every frame while the player is playing, supplying the current playback
        /// position in seconds. Subscribe from <c>KaraokeController</c> for lyric sync.
        /// </summary>
        public event Action<float> OnPlaybackTimeUpdated;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Read-only access to the current player configuration.</summary>
        public MusicPlayerConfig Config => config;

        /// <summary>Read-only snapshot of the current player state.</summary>
        public MusicPlayerState State => _state;

        /// <summary>Current playback state enum.</summary>
        public PlaybackState CurrentPlaybackState => _playbackState;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureMusicSource();

            if (flightController  == null) flightController  = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
            if (audioManager      == null) audioManager      = AudioManager.Instance
                                                                ?? FindFirstObjectByType<AudioManager>();
            if (settingsManager   == null) settingsManager   = FindFirstObjectByType<SettingsManager>();
        }

        private void Start()
        {
            LoadState();

            if (audioManager != null)
                _bgmOriginalVolume = 1f; // will be restored when player stops
        }

        private void Update()
        {
            // Update normalised playback position in state
            if (musicSource != null && musicSource.clip != null && musicSource.isPlaying)
            {
                _state.playbackPosition = musicSource.clip.length > 0f
                    ? musicSource.time / musicSource.clip.length
                    : 0f;

                OnPlaybackTimeUpdated?.Invoke(musicSource.time);
            }

            // Track ended — advance to next
            if (_playbackState == PlaybackState.Playing
                && musicSource != null
                && !musicSource.isPlaying
                && musicSource.clip != null)
            {
                OnTrackNaturalEnd();
            }

            // Auto-play on flight start
            if (config.autoPlayOnFlight && flightController != null)
            {
                bool flying = flightController.IsFlying;
                if (flying && !_wasFlying && _playbackState == PlaybackState.Stopped)
                    Play();
                _wasFlying = flying;
            }
        }

        private void OnDestroy()
        {
            SaveState();
            RestoreBGMVolume();
        }

        // ── Playback control ──────────────────────────────────────────────────────

        /// <summary>Starts or resumes playback.</summary>
        public void Play()
        {
            if (_playbackState == PlaybackState.Paused)
            {
                Resume();
                return;
            }

            // Pick a track if none is queued
            if (string.IsNullOrEmpty(_state.currentTrackId))
            {
                string next = PickFirstTrack();
                if (string.IsNullOrEmpty(next)) return;
                _state.currentTrackId = next;
            }

            if (_trackLibrary.TryGetValue(_state.currentTrackId, out MusicTrack track))
                StartPlayback(track, 0f);
        }

        /// <summary>Pauses playback.</summary>
        public void Pause()
        {
            if (_playbackState != PlaybackState.Playing) return;
            if (musicSource != null) musicSource.Pause();
            SetPlaybackState(PlaybackState.Paused);
            _state.isPaused  = true;
            _state.isPlaying = false;
        }

        /// <summary>Resumes paused playback.</summary>
        public void Resume()
        {
            if (_playbackState != PlaybackState.Paused) return;
            if (musicSource != null) musicSource.UnPause();
            SetPlaybackState(PlaybackState.Playing);
            _state.isPaused  = false;
            _state.isPlaying = true;
        }

        /// <summary>Stops playback and resets position.</summary>
        public void Stop()
        {
            StopCrossfade();
            if (musicSource != null) musicSource.Stop();
            SetPlaybackState(PlaybackState.Stopped);
            _state.isPlaying        = false;
            _state.isPaused         = false;
            _state.playbackPosition = 0f;
            RestoreBGMVolume();
            SaveState();
        }

        /// <summary>Advances to the next track in the current playlist.</summary>
        public void NextTrack()
        {
            string nextId = GetNextTrackId();
            if (!string.IsNullOrEmpty(nextId))
                PlayTrack(nextId);
        }

        /// <summary>Goes back to the previous track (via history).</summary>
        public void PreviousTrack()
        {
            string prevId = GetPreviousTrackId();
            if (!string.IsNullOrEmpty(prevId))
                PlayTrack(prevId);
        }

        /// <summary>Plays the track with the given <paramref name="trackId"/>.</summary>
        public void PlayTrack(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            if (!_trackLibrary.TryGetValue(trackId, out MusicTrack track))
            {
                Debug.LogWarning($"[SWEF][MusicPlayerManager] Track not found: {trackId}");
                return;
            }

            if (config.crossfadeDuration > 0f && _playbackState == PlaybackState.Playing)
            {
                StopCrossfade();
                _crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(track));
            }
            else
            {
                StartPlayback(track, 0f);
            }
        }

        /// <summary>Sets the active playlist by ID and optionally starts playback from the first track.</summary>
        public void SetPlaylist(string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId)) return;
            if (!_playlists.TryGetValue(playlistId, out MusicPlaylist playlist))
            {
                Debug.LogWarning($"[SWEF][MusicPlayerManager] Playlist not found: {playlistId}");
                return;
            }

            _state.currentPlaylistId = playlistId;
            _playlistCtrl.Reset();
            OnPlaylistChanged?.Invoke(playlist);
            SaveState();
        }

        /// <summary>Sets the music volume (0–1).</summary>
        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _state.volume = volume;
            config.musicVolume = volume;
            if (musicSource != null) musicSource.volume = volume;
            OnVolumeChanged?.Invoke(volume);
            SaveState();
        }

        /// <summary>Sets the active shuffle mode.</summary>
        public void SetShuffleMode(ShuffleMode mode)
        {
            config.shuffleMode  = mode;
            _state.shuffleMode  = mode;
            _playlistCtrl.CurrentShuffleMode = mode;
            SaveState();
        }

        /// <summary>Sets the active repeat mode.</summary>
        public void SetRepeatMode(RepeatMode mode)
        {
            config.repeatMode = mode;
            _state.repeatMode = mode;
            SaveState();
        }

        /// <summary>
        /// Seeks to a normalised position (0–1) within the current track.
        /// </summary>
        public void Seek(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if (musicSource == null || musicSource.clip == null) return;
            musicSource.time        = normalizedTime * musicSource.clip.length;
            _state.playbackPosition = normalizedTime;
        }

        /// <summary>Returns the current normalised playback position (0–1).</summary>
        public float GetPlaybackProgress() => _state.playbackPosition;

        /// <summary>
        /// Returns the current playback position in seconds, or 0 when nothing is playing.
        /// </summary>
        public float GetCurrentPlaybackTime()
        {
            if (musicSource == null || musicSource.clip == null) return 0f;
            return musicSource.time;
        }

        /// <summary>
        /// Returns the total duration of the current track in seconds, or 0 when no clip is loaded.
        /// </summary>
        public float GetCurrentTrackDuration()
        {
            if (musicSource == null || musicSource.clip == null) return 0f;
            return musicSource.clip.length;
        }

        /// <summary>Returns the currently active <see cref="MusicTrack"/>, or <c>null</c>.</summary>
        public MusicTrack GetCurrentTrack()
        {
            if (string.IsNullOrEmpty(_state.currentTrackId)) return null;
            _trackLibrary.TryGetValue(_state.currentTrackId, out MusicTrack track);
            return track;
        }

        /// <summary>Returns the currently active <see cref="MusicPlaylist"/>, or <c>null</c>.</summary>
        public MusicPlaylist GetCurrentPlaylist()
        {
            if (string.IsNullOrEmpty(_state.currentPlaylistId)) return null;
            _playlists.TryGetValue(_state.currentPlaylistId, out MusicPlaylist playlist);
            return playlist;
        }

        // ── Library management ────────────────────────────────────────────────────

        /// <summary>Registers a track in the library. Overwrites any existing entry with the same ID.</summary>
        public void RegisterTrack(MusicTrack track)
        {
            if (track == null || string.IsNullOrEmpty(track.trackId)) return;
            _trackLibrary[track.trackId] = track;
        }

        /// <summary>Removes a track from the library.</summary>
        public void UnregisterTrack(string trackId)
        {
            _trackLibrary.Remove(trackId);
        }

        /// <summary>
        /// Creates and registers a new user playlist.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="trackIds">Initial ordered list of track IDs.</param>
        /// <returns>The newly created <see cref="MusicPlaylist"/>.</returns>
        public MusicPlaylist CreatePlaylist(string name, List<string> trackIds)
        {
            var playlist = new MusicPlaylist
            {
                playlistId    = Guid.NewGuid().ToString(),
                name          = name,
                trackIds      = trackIds ?? new List<string>(),
                createdDate   = DateTime.UtcNow.ToString("o"),
                isUserCreated = true
            };
            _playlists[playlist.playlistId] = playlist;
            return playlist;
        }

        /// <summary>Deletes a playlist by ID. Cannot delete system playlists.</summary>
        public void DeletePlaylist(string playlistId)
        {
            if (!_playlists.TryGetValue(playlistId, out MusicPlaylist pl)) return;
            if (!pl.isUserCreated)
            {
                Debug.LogWarning("[SWEF][MusicPlayerManager] Cannot delete a system playlist.");
                return;
            }
            _playlists.Remove(playlistId);
            if (_state.currentPlaylistId == playlistId)
                _state.currentPlaylistId = string.Empty;
        }

        /// <summary>Toggles the <see cref="MusicTrack.isFavorite"/> flag on the given track.</summary>
        public void ToggleFavorite(string trackId)
        {
            if (_trackLibrary.TryGetValue(trackId, out MusicTrack track))
                track.isFavorite = !track.isFavorite;
        }

        /// <summary>Returns all tracks that have been marked as favourites.</summary>
        public List<MusicTrack> GetFavorites()
        {
            var result = new List<MusicTrack>();
            foreach (var track in _trackLibrary.Values)
                if (track.isFavorite) result.Add(track);
            return result;
        }

        /// <summary>Returns all tracks whose <see cref="MusicTrack.moodTags"/> contain <paramref name="mood"/>.</summary>
        public List<MusicTrack> GetTracksByMood(MusicMood mood)
        {
            var result = new List<MusicTrack>();
            foreach (var track in _trackLibrary.Values)
                if (track.moodTags != null && track.moodTags.Contains(mood))
                    result.Add(track);
            return result;
        }

        /// <summary>Returns all tracks matching the given <paramref name="genre"/> (case-insensitive).</summary>
        public List<MusicTrack> GetTracksByGenre(string genre)
        {
            var result = new List<MusicTrack>();
            foreach (var track in _trackLibrary.Values)
                if (string.Equals(track.genre, genre, StringComparison.OrdinalIgnoreCase))
                    result.Add(track);
            return result;
        }

        /// <summary>Returns a read-only view of all registered playlists.</summary>
        public IEnumerable<MusicPlaylist> GetAllPlaylists() => _playlists.Values;

        /// <summary>Returns a read-only view of all registered tracks.</summary>
        public IEnumerable<MusicTrack> GetAllTracks() => _trackLibrary.Values;

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void EnsureMusicSource()
        {
            if (musicSource == null)
            {
                musicSource              = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake  = false;
                musicSource.loop         = false;
                musicSource.spatialBlend = 0f; // 2-D (UI music)
            }
        }

        private void StartPlayback(MusicTrack track, float fromNormalizedPosition)
        {
            StopCrossfade();

            _state.currentTrackId   = track.trackId;
            _state.isPlaying        = true;
            _state.isPaused         = false;
            _state.playbackPosition = fromNormalizedPosition;

            // Attempt to load the clip from Resources
            StartCoroutine(LoadClipAndPlay(track, fromNormalizedPosition));

            DuckBGMVolume();
            SetPlaybackState(PlaybackState.Playing);
            OnTrackChanged?.Invoke(track);
            SaveState();
        }

        private IEnumerator LoadClipAndPlay(MusicTrack track, float normalizedStart)
        {
            string path = !string.IsNullOrEmpty(track.audioClipPath)
                ? track.audioClipPath
                : track.localFilePath;

            if (!string.IsNullOrEmpty(path))
            {
                ResourceRequest req = Resources.LoadAsync<AudioClip>(path);
                yield return req;

                if (req.asset is AudioClip clip)
                {
                    musicSource.clip   = clip;
                    musicSource.volume = _state.volume;
                    musicSource.time   = normalizedStart * clip.length;
                    musicSource.Play();
                    yield break;
                }
            }

            // No clip available (streaming or missing asset) — state-only update
            Debug.Log($"[SWEF][MusicPlayerManager] No local clip for '{track.title}'; state updated only.");
        }

        private IEnumerator CrossfadeCoroutine(MusicTrack nextTrack)
        {
            float duration    = Mathf.Max(0.1f, config.crossfadeDuration);
            float startVolume = musicSource != null ? musicSource.volume : _state.volume;
            float elapsed     = 0f;

            // Fade out
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration * 0.5f));
                yield return null;
            }

            // Swap track
            _state.currentTrackId = nextTrack.trackId;
            StartCoroutine(LoadClipAndPlay(nextTrack, 0f));
            OnTrackChanged?.Invoke(nextTrack);

            elapsed = 0f;
            float targetVolume = _state.volume;

            // Fade in
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration * 0.5f));
                yield return null;
            }

            if (musicSource != null) musicSource.volume = targetVolume;
            _crossfadeCoroutine = null;
            SaveState();
        }

        private void StopCrossfade()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }
        }

        private void DuckBGMVolume()
        {
            if (audioManager == null) return;
            audioManager.SetBGMVolume(config.sfxDuckingAmount);
        }

        private void RestoreBGMVolume()
        {
            if (audioManager == null) return;
            audioManager.SetBGMVolume(_bgmOriginalVolume);
        }

        private void SetPlaybackState(PlaybackState newState)
        {
            if (_playbackState == newState) return;
            _playbackState = newState;
            OnPlaybackStateChanged?.Invoke(newState);
        }

        private void OnTrackNaturalEnd()
        {
            if (_state.repeatMode == RepeatMode.One)
            {
                Seek(0f);
                if (musicSource != null) musicSource.Play();
                return;
            }

            string nextId = GetNextTrackId();
            if (!string.IsNullOrEmpty(nextId))
            {
                PlayTrack(nextId);
            }
            else
            {
                Stop();
            }
        }

        private string GetNextTrackId()
        {
            MusicPlaylist playlist = GetCurrentPlaylist();
            if (playlist == null || playlist.trackIds.Count == 0)
                return string.Empty;

            var tracks = new List<MusicTrack>();
            foreach (string id in playlist.trackIds)
                if (_trackLibrary.TryGetValue(id, out MusicTrack t)) tracks.Add(t);

            return _playlistCtrl.GetNextTrack(_state, playlist, tracks);
        }

        private string GetPreviousTrackId()
        {
            MusicPlaylist playlist = GetCurrentPlaylist();
            if (playlist == null || playlist.trackIds.Count == 0)
                return string.Empty;

            var tracks = new List<MusicTrack>();
            foreach (string id in playlist.trackIds)
                if (_trackLibrary.TryGetValue(id, out MusicTrack t)) tracks.Add(t);

            return _playlistCtrl.GetPreviousTrack(_state, playlist, tracks);
        }

        private string PickFirstTrack()
        {
            MusicPlaylist playlist = GetCurrentPlaylist();
            if (playlist != null && playlist.trackIds.Count > 0)
                return playlist.trackIds[0];

            foreach (string id in _trackLibrary.Keys)
                return id;

            return string.Empty;
        }

        // ── State persistence ─────────────────────────────────────────────────────

        private void SaveState()
        {
            PlayerPrefs.SetFloat(KeyVolume,        _state.volume);
            PlayerPrefs.SetInt  (KeyShuffle,  (int)_state.shuffleMode);
            PlayerPrefs.SetInt  (KeyRepeat,   (int)_state.repeatMode);
            PlayerPrefs.SetString(KeyCurrentTrack, _state.currentTrackId ?? string.Empty);
            PlayerPrefs.SetString(KeyCurrentList,  _state.currentPlaylistId ?? string.Empty);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            _state.volume            = PlayerPrefs.GetFloat (KeyVolume,   0.8f);
            _state.shuffleMode       = (ShuffleMode)PlayerPrefs.GetInt(KeyShuffle, 0);
            _state.repeatMode        = (RepeatMode) PlayerPrefs.GetInt(KeyRepeat,  0);
            _state.currentTrackId    = PlayerPrefs.GetString(KeyCurrentTrack, string.Empty);
            _state.currentPlaylistId = PlayerPrefs.GetString(KeyCurrentList,  string.Empty);

            config.shuffleMode  = _state.shuffleMode;
            config.repeatMode   = _state.repeatMode;
            config.musicVolume  = _state.volume;

            if (musicSource != null) musicSource.volume = _state.volume;
            _playlistCtrl.CurrentShuffleMode = _state.shuffleMode;
        }
    }
}
