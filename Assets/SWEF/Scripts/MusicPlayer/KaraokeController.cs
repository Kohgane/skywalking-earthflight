using System;
using System.Collections;
using UnityEngine;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Synchronises lyric display with <see cref="MusicPlayerManager"/> playback.
    /// <para>
    /// Subscribes to play/pause/seek/track-change events from <see cref="MusicPlayerManager"/>
    /// and tracks the active lyric line and word based on the current playback position.
    /// </para>
    /// <para>
    /// Attach this component to the same GameObject as (or a child of)
    /// <see cref="MusicPlayerManager"/>.
    /// </para>
    /// </summary>
    public class KaraokeController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const float DefaultLeadInSeconds    = 0.5f;
        private const float LyricsEndGraceSeconds   = 2f;
        private const float FallbackWordDurationSec = 5f;
        private const string LogTag                 = "[SWEF][KaraokeController]";

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("How many seconds ahead the 'next line' is pre-fetched for display.")]
        [SerializeField] private float leadInSeconds = DefaultLeadInSeconds;

        [Header("Practice Loop")]
        [Tooltip("When true, playback loops between LoopStartLine and LoopEndLine.")]
        [SerializeField] private bool  loopEnabled;

        [Tooltip("First line index (inclusive) of the practice loop.")]
        [SerializeField] private int   loopStartLine;

        [Tooltip("Last line index (inclusive) of the practice loop. -1 = end of lyrics.")]
        [SerializeField] private int   loopEndLine = -1;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the active lyric line changes.</summary>
        public event Action<int, LrcLine> OnLineChanged;

        /// <summary>
        /// Fired each frame while a word-timed line is active.
        /// Parameters: active word index (0-based), progress within that word (0–1).
        /// </summary>
        public event Action<int, float> OnWordProgress;

        /// <summary>Fired when lyrics begin (playback enters the first lyric line).</summary>
        public event Action OnLyricsStarted;

        /// <summary>Fired when lyrics end (playback passes the last lyric line).</summary>
        public event Action OnLyricsEnded;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Currently loaded lyrics data, or <c>null</c> if none loaded.</summary>
        public LrcData CurrentLyrics { get; private set; }

        /// <summary>Index of the currently active lyric line (-1 = before first line).</summary>
        public int CurrentLineIndex { get; private set; } = -1;

        /// <summary>Currently active <see cref="LrcLine"/>, or <c>null</c>.</summary>
        public LrcLine CurrentLine =>
            CurrentLyrics != null && CurrentLineIndex >= 0 && CurrentLineIndex < CurrentLyrics.lines.Count
                ? CurrentLyrics.lines[CurrentLineIndex]
                : null;

        /// <summary>Next <see cref="LrcLine"/> after <see cref="CurrentLine"/>, or <c>null</c>.</summary>
        public LrcLine NextLine =>
            CurrentLyrics != null && CurrentLineIndex + 1 < CurrentLyrics.lines.Count
                ? CurrentLyrics.lines[CurrentLineIndex + 1]
                : null;

        /// <summary>Previous <see cref="LrcLine"/> before <see cref="CurrentLine"/>, or <c>null</c>.</summary>
        public LrcLine PreviousLine =>
            CurrentLyrics != null && CurrentLineIndex > 0
                ? CurrentLyrics.lines[CurrentLineIndex - 1]
                : null;

        /// <summary>Whether lyrics have started playing.</summary>
        public bool IsLyricsActive { get; private set; }

        /// <summary>Configurable lead-in time (seconds).</summary>
        public float LeadInSeconds
        {
            get => leadInSeconds;
            set => leadInSeconds = Mathf.Max(0f, value);
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private bool  _isPlaying;
        private float _playbackSeconds;
        private bool  _lyricsEnded;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            CurrentLineIndex = -1;
        }

        private void Start()
        {
            // Subscribe to MusicPlayerManager events
            if (MusicPlayerManager.Instance != null)
            {
                MusicPlayerManager.Instance.OnTrackChanged         += HandleTrackChanged;
                MusicPlayerManager.Instance.OnPlaybackStateChanged += HandlePlaybackStateChanged;
                MusicPlayerManager.Instance.OnPlaybackTimeUpdated  += HandlePlaybackTimeUpdated;
            }
            else
            {
                Debug.LogWarning($"{LogTag} MusicPlayerManager.Instance not found; will retry each frame.");
            }

            // Subscribe to LyricsDatabase events
            if (LyricsDatabase.Instance != null)
                LyricsDatabase.Instance.OnLyricsLoaded += HandleLyricsLoaded;
        }

        private void OnDestroy()
        {
            if (MusicPlayerManager.Instance != null)
            {
                MusicPlayerManager.Instance.OnTrackChanged         -= HandleTrackChanged;
                MusicPlayerManager.Instance.OnPlaybackStateChanged -= HandlePlaybackStateChanged;
                MusicPlayerManager.Instance.OnPlaybackTimeUpdated  -= HandlePlaybackTimeUpdated;
            }

            if (LyricsDatabase.Instance != null)
                LyricsDatabase.Instance.OnLyricsLoaded -= HandleLyricsLoaded;
        }

        private void Update()
        {
            // Late-bind to manager in case it was not ready during Start
            if (MusicPlayerManager.Instance != null && !_managerBound)
                BindToManager();

            if (!_isPlaying || CurrentLyrics == null || !CurrentLyrics.HasLyrics)
                return;

            // Practice loop: seek back if past loop end
            if (loopEnabled && CurrentLineIndex >= 0)
            {
                int effectiveEnd = loopEndLine < 0
                    ? CurrentLyrics.lines.Count - 1
                    : Mathf.Min(loopEndLine, CurrentLyrics.lines.Count - 1);

                int effectiveStart = Mathf.Clamp(loopStartLine, 0, CurrentLyrics.lines.Count - 1);

                if (CurrentLineIndex > effectiveEnd && MusicPlayerManager.Instance != null)
                {
                    float seekTarget = CurrentLyrics.lines[effectiveStart].timestamp;
                    float duration   = MusicPlayerManager.Instance.GetCurrentTrackDuration();
                    if (duration > 0f)
                        MusicPlayerManager.Instance.Seek(seekTarget / duration);
                    return;
                }
            }

            TickLyrics(_playbackSeconds);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads new <see cref="LrcData"/> and resets all lyric state.
        /// </summary>
        /// <param name="data">The parsed lyrics to use. Pass <c>null</c> to clear lyrics.</param>
        public void SetLyrics(LrcData data)
        {
            CurrentLyrics    = data;
            CurrentLineIndex = -1;
            IsLyricsActive   = false;
            _lyricsEnded     = false;
        }

        /// <summary>
        /// Configures a practice loop between two line indices.
        /// Pass -1 for <paramref name="endLineIndex"/> to loop to the end of the lyrics.
        /// </summary>
        /// <param name="startLineIndex">First line (inclusive) of the loop region.</param>
        /// <param name="endLineIndex">Last line (inclusive), or -1 for end.</param>
        public void SetPracticeLoop(int startLineIndex, int endLineIndex = -1)
        {
            loopEnabled   = true;
            loopStartLine = Mathf.Max(0, startLineIndex);
            loopEndLine   = endLineIndex;
        }

        /// <summary>Disables the practice loop.</summary>
        public void ClearPracticeLoop()
        {
            loopEnabled = false;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleTrackChanged(MusicTrack track)
        {
            SetLyrics(null);

            if (track == null) return;

            // Load lyrics async; KaraokeController will react via HandleLyricsLoaded
            if (LyricsDatabase.Instance != null)
                LyricsDatabase.Instance.LoadLyricsAsync(track);
        }

        private void HandlePlaybackStateChanged(PlaybackState state)
        {
            _isPlaying = state == PlaybackState.Playing;
        }

        private void HandlePlaybackTimeUpdated(float seconds)
        {
            _playbackSeconds = seconds;
        }

        private void HandleLyricsLoaded(string trackId, LrcData data)
        {
            // Only apply if this is the current track
            if (MusicPlayerManager.Instance == null) return;
            MusicTrack current = MusicPlayerManager.Instance.GetCurrentTrack();
            if (current == null || current.trackId != trackId) return;

            SetLyrics(data);
        }

        // ── Core tick ─────────────────────────────────────────────────────────────

        private void TickLyrics(float seconds)
        {
            if (CurrentLyrics == null || !CurrentLyrics.HasLyrics) return;

            int newIndex = LrcParser.FindLineIndex(CurrentLyrics, seconds);

            if (newIndex != CurrentLineIndex)
            {
                CurrentLineIndex = newIndex;

                if (newIndex == 0 && !IsLyricsActive)
                {
                    IsLyricsActive = true;
                    _lyricsEnded   = false;
                    OnLyricsStarted?.Invoke();
                }

                if (newIndex >= 0 && newIndex < CurrentLyrics.lines.Count)
                    OnLineChanged?.Invoke(newIndex, CurrentLyrics.lines[newIndex]);
            }

            // Lyrics ended check
            if (!_lyricsEnded && CurrentLyrics.lines.Count > 0)
            {
                LrcLine lastLine = CurrentLyrics.lines[CurrentLyrics.lines.Count - 1];
                if (seconds > lastLine.timestamp + LyricsEndGraceSeconds)
                {
                    _lyricsEnded   = true;
                    IsLyricsActive = false;
                    OnLyricsEnded?.Invoke();
                }
            }

            // Word-level progress
            LrcLine activeLine = CurrentLine;
            if (activeLine != null && activeLine.HasWordTiming)
            {
                int wordIndex = LrcParser.FindWordIndex(activeLine, seconds);
                if (wordIndex >= 0)
                {
                    float wordProgress = 0f;
                    float wordStart    = activeLine.words[wordIndex].startTime;
                    float wordEnd      = (wordIndex + 1 < activeLine.words.Count)
                        ? activeLine.words[wordIndex + 1].startTime
                        : activeLine.timestamp + FallbackWordDurationSec;

                    float wordDuration = wordEnd - wordStart;
                    if (wordDuration > 0f)
                        wordProgress = Mathf.Clamp01((seconds - wordStart) / wordDuration);

                    OnWordProgress?.Invoke(wordIndex, wordProgress);
                }
            }
        }

        // ── Late-bind helper ──────────────────────────────────────────────────────

        private bool _managerBound;

        private void BindToManager()
        {
            MusicPlayerManager.Instance.OnTrackChanged         += HandleTrackChanged;
            MusicPlayerManager.Instance.OnPlaybackStateChanged += HandlePlaybackStateChanged;
            MusicPlayerManager.Instance.OnPlaybackTimeUpdated  += HandlePlaybackTimeUpdated;
            _managerBound = true;

            // Sync current state
            _isPlaying = MusicPlayerManager.Instance.CurrentPlaybackState == PlaybackState.Playing;

            MusicTrack current = MusicPlayerManager.Instance.GetCurrentTrack();
            if (current != null && LyricsDatabase.Instance != null)
                LyricsDatabase.Instance.LoadLyricsAsync(current);
        }
    }
}
