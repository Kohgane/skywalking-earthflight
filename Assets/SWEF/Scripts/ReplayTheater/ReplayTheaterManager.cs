using System;
using System.Collections;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton manager that owns and coordinates a Replay Theater session.
    /// Call <see cref="EnterTheater"/> to load a replay and activate theater mode,
    /// and <see cref="ExitTheater"/> to tear it down and resume normal gameplay.
    /// </summary>
    public class ReplayTheaterManager : MonoBehaviour
    {
        #region Singleton

        private static ReplayTheaterManager _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplayTheaterManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplayTheaterManager>();
                return _instance;
            }
        }

        #endregion

        #region Theater State

        /// <summary>Possible states of the Replay Theater.</summary>
        public enum TheaterState
        {
            /// <summary>Theater is not active.</summary>
            Inactive,
            /// <summary>Theater is loading replay data.</summary>
            Loading,
            /// <summary>Replay is playing.</summary>
            Playing,
            /// <summary>Replay is paused.</summary>
            Paused,
            /// <summary>User is dragging the timeline scrub bar.</summary>
            Scrubbing,
            /// <summary>User is editing the cinematic camera path.</summary>
            Editing
        }

        #endregion

        #region Inspector

        [Header("Dependencies")]
        [SerializeField] private ReplayTimeline        timeline;
        [SerializeField] private CinematicCameraEditor cameraEditor;
        [SerializeField] private ReplayTheaterUI       theaterUI;
        [SerializeField] private ReplayTheaterSettings settings;

        #endregion

        #region State

        private TheaterState _state = TheaterState.Inactive;
        private ReplayData   _currentData;

        #endregion

        #region Events

        /// <summary>Fired when the theater is entered and fully loaded.</summary>
        public event Action<ReplayData> OnTheaterEntered;

        /// <summary>Fired when the theater is exited.</summary>
        public event Action OnTheaterExited;

        /// <summary>Fired whenever the theater state changes.</summary>
        public event Action<TheaterState> OnStateChanged;

        #endregion

        #region Properties

        /// <summary>Current theater state.</summary>
        public TheaterState State => _state;

        /// <summary>Currently loaded <see cref="ReplayData"/>, or <c>null</c>.</summary>
        public ReplayData CurrentData => _currentData;

        /// <summary>Whether the theater is currently active.</summary>
        public bool IsActive => _state != TheaterState.Inactive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");

            // Auto-discover sub-systems if not assigned
            if (timeline      == null) timeline      = GetComponentInChildren<ReplayTimeline>();
            if (cameraEditor  == null) cameraEditor  = GetComponentInChildren<CinematicCameraEditor>();
            if (theaterUI     == null) theaterUI     = GetComponentInChildren<ReplayTheaterUI>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enters the Replay Theater, pauses the main game, and loads <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The replay to load into the theater.</param>
        public void EnterTheater(ReplayData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SWEF] ReplayTheaterManager: EnterTheater called with null data.");
                return;
            }

            if (IsActive)
            {
                Debug.LogWarning("[SWEF] ReplayTheaterManager: Already in theater. Call ExitTheater first.");
                return;
            }

            Debug.Log($"[SWEF] ReplayTheaterManager: Entering theater for replay '{data.replayId}'.");
            SetState(TheaterState.Loading);

            // Pause main game
            Time.timeScale = 0f;

            StartCoroutine(LoadTheaterAsync(data));
        }

        /// <summary>
        /// Exits the Replay Theater, cleans up, and resumes the main game.
        /// </summary>
        public void ExitTheater()
        {
            if (!IsActive) return;

            Debug.Log("[SWEF] ReplayTheaterManager: Exiting theater.");

            timeline?.Stop();
            cameraEditor?.ClearKeyframes();
            theaterUI?.Hide();

            _currentData = null;
            Time.timeScale = 1f;

            SetState(TheaterState.Inactive);
            OnTheaterExited?.Invoke();
        }

        /// <summary>Transitions the theater into <see cref="TheaterState.Playing"/>.</summary>
        public void Play()
        {
            if (!IsActive) return;
            timeline?.Play();
            SetState(TheaterState.Playing);
        }

        /// <summary>Transitions the theater into <see cref="TheaterState.Paused"/>.</summary>
        public void Pause()
        {
            if (_state != TheaterState.Playing) return;
            timeline?.Pause();
            SetState(TheaterState.Paused);
        }

        /// <summary>Stops playback and rewinds to the beginning.</summary>
        public void Stop()
        {
            if (!IsActive) return;
            timeline?.Stop();
            SetState(TheaterState.Paused);
        }

        /// <summary>Enters camera editing mode.</summary>
        public void BeginEditing()
        {
            if (!IsActive) return;
            timeline?.Pause();
            SetState(TheaterState.Editing);
        }

        /// <summary>Exits camera editing mode and returns to paused state.</summary>
        public void EndEditing()
        {
            if (_state != TheaterState.Editing) return;
            SetState(TheaterState.Paused);
        }

        /// <summary>Notifies the manager that the user has started scrubbing.</summary>
        public void BeginScrubbing()
        {
            if (!IsActive) return;
            timeline?.Pause();
            SetState(TheaterState.Scrubbing);
        }

        /// <summary>Notifies the manager that scrubbing has ended.</summary>
        public void EndScrubbing()
        {
            if (_state != TheaterState.Scrubbing) return;
            SetState(TheaterState.Paused);
        }

        #endregion

        #region Internals

        private IEnumerator LoadTheaterAsync(ReplayData data)
        {
            _currentData = data;

            // Allow one frame for UI to respond
            yield return null;

            timeline?.Load(data);
            cameraEditor?.SetReplayDuration(data.GetDuration());
            theaterUI?.Show(data);

            SetState(TheaterState.Paused);
            OnTheaterEntered?.Invoke(data);
            Debug.Log("[SWEF] ReplayTheaterManager: Theater ready.");
        }

        private void SetState(TheaterState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnStateChanged?.Invoke(_state);
            Debug.Log($"[SWEF] ReplayTheaterManager: State → {_state}.");
        }

        #endregion
    }
}
