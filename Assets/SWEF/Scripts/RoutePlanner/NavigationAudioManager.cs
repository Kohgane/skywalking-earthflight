using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Audio feedback manager for the navigation system.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Voice guidance cues (approaching, reached, off-course, complete, turn left/right).</item>
    ///   <item>Chime/tone alternatives when voice clips are not assigned.</item>
    ///   <item>Cooldown between cues to prevent spam.</item>
    ///   <item>Spatial audio source positioned at each waypoint.</item>
    ///   <item>Independent volume control.</item>
    ///   <item>Mute/unmute toggle.</item>
    ///   <item>Priority system — higher-priority cues interrupt lower-priority ones.</item>
    /// </list>
    /// </para>
    /// Requires a <see cref="NavigationController"/> in the scene.
    /// </summary>
    public class NavigationAudioManager : MonoBehaviour
    {
        #region Constants

        private const float DefaultApproachDistance = 400f;  // metres — triggers "approaching"
        private const float DefaultCooldown         = 5f;    // seconds minimum between any cues
        private const float TurnCueThreshold        = 30f;   // degrees heading correction for turn cue

        // Priority levels (higher = more important)
        private const int PriorityApproaching   = 1;
        private const int PriorityWaypointReached = 3;
        private const int PriorityOffCourse     = 2;
        private const int PriorityRouteComplete  = 5;
        private const int PriorityTurn          = 1;

        #endregion

        #region Inspector

        [Header("Controller")]
        [Tooltip("NavigationController to listen to. Auto-found if null.")]
        [SerializeField] private NavigationController navigationController;

        [Header("Audio Sources")]
        [Tooltip("AudioSource used for voice/chime cues (2D).")]
        [SerializeField] private AudioSource voiceSource;
        [Tooltip("AudioSource used for spatial waypoint audio (3D, positioned at waypoint).")]
        [SerializeField] private AudioSource spatialSource;

        [Header("Voice Clips")]
        [SerializeField] private AudioClip[] approachingClips;
        [SerializeField] private AudioClip[] waypointReachedClips;
        [SerializeField] private AudioClip[] offCourseClips;
        [SerializeField] private AudioClip[] routeCompleteClips;
        [SerializeField] private AudioClip[] turnLeftClips;
        [SerializeField] private AudioClip[] turnRightClips;

        [Header("Chime Alternatives")]
        [SerializeField] private AudioClip approachChime;
        [SerializeField] private AudioClip arrivalChime;
        [SerializeField] private AudioClip offCourseChime;
        [SerializeField] private AudioClip completeChime;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float navigationVolume = 0.8f;
        [SerializeField] private bool  muted;
        [SerializeField] private float approachDistance = DefaultApproachDistance;
        [SerializeField] private float cueCooldown      = DefaultCooldown;

        #endregion

        #region Private State

        private float _lastCueTime   = -999f;
        private int   _lastPriority  = 0;
        private bool  _approachPlayed;
        private bool  _turnCuePlayed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (navigationController == null)
                navigationController = FindFirstObjectByType<NavigationController>();

            EnsureAudioSources();
        }

        private void OnEnable()
        {
            if (navigationController == null) return;
            navigationController.OnWaypointReached   += HandleWaypointReached;
            navigationController.OnRouteDeviation    += HandleRouteDeviation;
            navigationController.OnNavigationStarted += HandleNavigationStarted;
            navigationController.OnNavigationEnded   += HandleNavigationEnded;
        }

        private void OnDisable()
        {
            if (navigationController == null) return;
            navigationController.OnWaypointReached   -= HandleWaypointReached;
            navigationController.OnRouteDeviation    -= HandleRouteDeviation;
            navigationController.OnNavigationStarted -= HandleNavigationStarted;
            navigationController.OnNavigationEnded   -= HandleNavigationEnded;
        }

        private void Update()
        {
            if (!navigationController.IsNavigating) return;

            CheckApproachingCue();
            CheckTurnCue();
        }

        #endregion

        #region Public API

        /// <summary>Mutes or unmutes all navigation audio.</summary>
        public void SetMuted(bool mute)
        {
            muted = mute;
            if (voiceSource   != null) voiceSource.mute   = mute;
            if (spatialSource != null) spatialSource.mute = mute;
        }

        /// <summary>Sets the navigation audio volume (0–1).</summary>
        public void SetVolume(float volume)
        {
            navigationVolume = Mathf.Clamp01(volume);
            if (voiceSource   != null) voiceSource.volume   = navigationVolume;
            if (spatialSource != null) spatialSource.volume = navigationVolume;
        }

        #endregion

        #region Event Handlers

        private void HandleNavigationStarted(List<Waypoint> waypoints)
        {
            _approachPlayed = false;
            _turnCuePlayed  = false;
        }

        private void HandleNavigationEnded()
        {
            // Nothing to clean up here
        }

        private void HandleWaypointReached(Waypoint wp, int index)
        {
            _approachPlayed = false;
            _turnCuePlayed  = false;

            bool isLast = navigationController.Waypoints != null &&
                          index >= navigationController.Waypoints.Count - 1;

            if (isLast)
                PlayCue(PickRandom(routeCompleteClips) ?? completeChime, PriorityRouteComplete);
            else
                PlayCue(PickRandom(waypointReachedClips) ?? arrivalChime, PriorityWaypointReached);

            // Spatial audio at the waypoint
            PlaySpatial(wp.position, arrivalChime);
        }

        private void HandleRouteDeviation(float distance)
        {
            PlayCue(PickRandom(offCourseClips) ?? offCourseChime, PriorityOffCourse);
        }

        #endregion

        #region Proximity Cues

        private void CheckApproachingCue()
        {
            if (_approachPlayed) return;

            float dist = navigationController.DistanceToWaypoint;
            if (dist <= approachDistance)
            {
                _approachPlayed = true;
                PlayCue(PickRandom(approachingClips) ?? approachChime, PriorityApproaching);
            }
        }

        private void CheckTurnCue()
        {
            if (_turnCuePlayed) return;

            float correction = navigationController.HeadingCorrection;
            if (Mathf.Abs(correction) >= TurnCueThreshold)
            {
                _turnCuePlayed = true;
                AudioClip clip = correction < 0f
                    ? PickRandom(turnLeftClips)
                    : PickRandom(turnRightClips);
                PlayCue(clip, PriorityTurn);
            }
        }

        #endregion

        #region Playback

        private void PlayCue(AudioClip clip, int priority)
        {
            if (muted || clip == null || voiceSource == null) return;

            float now = Time.time;
            bool onCooldown = now - _lastCueTime < cueCooldown && priority <= _lastPriority;
            if (onCooldown) return;

            _lastCueTime  = now;
            _lastPriority = priority;

            if (voiceSource.isPlaying && priority > _lastPriority)
                voiceSource.Stop();

            voiceSource.volume = navigationVolume;
            voiceSource.PlayOneShot(clip);
        }

        private void PlaySpatial(Vector3 worldPos, AudioClip clip)
        {
            if (muted || clip == null || spatialSource == null) return;
            spatialSource.transform.position = worldPos;
            spatialSource.volume             = navigationVolume * 0.5f;
            spatialSource.PlayOneShot(clip);
        }

        private static AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        #endregion

        #region Setup

        private void EnsureAudioSources()
        {
            if (voiceSource == null)
            {
                voiceSource        = gameObject.AddComponent<AudioSource>();
                voiceSource.loop   = false;
                voiceSource.volume = navigationVolume;
            }

            if (spatialSource == null)
            {
                var go             = new GameObject("NavSpatialAudio");
                go.transform.SetParent(transform);
                spatialSource            = go.AddComponent<AudioSource>();
                spatialSource.spatialBlend = 1f;  // fully 3D
                spatialSource.loop       = false;
                spatialSource.volume     = navigationVolume * 0.5f;
            }
        }

        #endregion
    }
}
