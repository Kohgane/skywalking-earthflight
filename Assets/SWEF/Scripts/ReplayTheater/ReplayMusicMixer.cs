using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton MonoBehaviour that manages background music and engine-sound mixing
    /// for the Replay Theater.  Supports fade in/out, beat markers, and per-clip
    /// engine-sound muting.
    /// </summary>
    public class ReplayMusicMixer : MonoBehaviour
    {
        #region Singleton

        private static ReplayMusicMixer _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplayMusicMixer Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplayMusicMixer>();
                return _instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource engineSoundSource;

        [Header("Music Settings")]
        [SerializeField] private float      defaultFadeInDuration  = 2f;
        [SerializeField] private float      defaultFadeOutDuration = 2f;
        [SerializeField] private AudioClip[] availableTracks;

        #endregion

        #region State

        private List<float>               _beatMarkers          = new List<float>();
        private Dictionary<string, bool>  _clipEngineMuteState  = new Dictionary<string, bool>();
        private Coroutine                 _fadingCoroutine;

        #endregion

        #region Events

        /// <summary>Fired when the active music track changes.  Parameter is the new clip.</summary>
        public event Action<AudioClip> OnTrackChanged;

        /// <summary>Fired when a beat marker is added.  Parameter is the marker time in seconds.</summary>
        public event Action<float> OnBeatMarkerAdded;

        #endregion

        #region Properties

        /// <summary>Whether the music source is currently playing.</summary>
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        /// <summary>The audio clip currently assigned to the music source, or <c>null</c>.</summary>
        public AudioClip CurrentTrack => musicSource != null ? musicSource.clip : null;

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
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the music track to the provided <see cref="AudioClip"/> and begins playback.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        public void SetMusicTrack(AudioClip clip)
        {
            if (musicSource == null) return;
            musicSource.clip = clip;
            musicSource.Play();
            Debug.Log($"[SWEF] ReplayMusicMixer: Track set to '{clip?.name}'.");
            OnTrackChanged?.Invoke(clip);
        }

        /// <summary>
        /// Sets the music track by index into <c>availableTracks</c> and begins playback.
        /// </summary>
        /// <param name="trackIndex">Zero-based index into the available tracks array.</param>
        public void SetMusicTrack(int trackIndex)
        {
            if (availableTracks == null || trackIndex < 0 || trackIndex >= availableTracks.Length)
            {
                Debug.LogWarning($"[SWEF] ReplayMusicMixer: Track index {trackIndex} is out of range.");
                return;
            }
            SetMusicTrack(availableTracks[trackIndex]);
        }

        /// <summary>Fades the music in from silence over the specified duration.</summary>
        /// <param name="duration">Fade duration in seconds.  Use ≤ 0 for the default.</param>
        public void FadeInMusic(float duration = -1f)
        {
            float d = duration > 0f ? duration : defaultFadeInDuration;
            if (_fadingCoroutine != null) StopCoroutine(_fadingCoroutine);
            _fadingCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, d));
        }

        /// <summary>Fades the music out to silence over the specified duration.</summary>
        /// <param name="duration">Fade duration in seconds.  Use ≤ 0 for the default.</param>
        public void FadeOutMusic(float duration = -1f)
        {
            float d = duration > 0f ? duration : defaultFadeOutDuration;
            if (_fadingCoroutine != null) StopCoroutine(_fadingCoroutine);
            _fadingCoroutine = StartCoroutine(FadeCoroutine(musicSource != null ? musicSource.volume : 1f, 0f, d));
        }

        /// <summary>Immediately sets the music volume.</summary>
        /// <param name="volume">Volume level in the range [0, 1].</param>
        public void SetMusicVolume(float volume)
        {
            if (musicSource == null) return;
            musicSource.volume = Mathf.Clamp01(volume);
            Debug.Log($"[SWEF] ReplayMusicMixer: Volume → {volume:F2}.");
        }

        /// <summary>
        /// Adds a beat marker at the given time.  Beat markers are used for sync operations.
        /// </summary>
        /// <param name="time">Time in seconds to mark.</param>
        public void AddBeatMarker(float time)
        {
            if (!_beatMarkers.Contains(time))
            {
                _beatMarkers.Add(time);
                _beatMarkers.Sort();
                Debug.Log($"[SWEF] ReplayMusicMixer: Beat marker added at {time:F3}s.");
                OnBeatMarkerAdded?.Invoke(time);
            }
        }

        /// <summary>Removes the beat marker at the given time, if it exists.</summary>
        /// <param name="time">Time in seconds of the marker to remove.</param>
        public void RemoveBeatMarker(float time)
        {
            if (_beatMarkers.Remove(time))
                Debug.Log($"[SWEF] ReplayMusicMixer: Beat marker at {time:F3}s removed.");
        }

        /// <summary>
        /// Mutes or unmutes the engine sound for a specific clip.
        /// </summary>
        /// <param name="clipId">Identifier of the clip.</param>
        /// <param name="mute"><c>true</c> to mute the engine sound; <c>false</c> to unmute.</param>
        public void MuteEngineSound(string clipId, bool mute)
        {
            _clipEngineMuteState[clipId] = mute;
            Debug.Log($"[SWEF] ReplayMusicMixer: Engine sound for clip '{clipId}' muted = {mute}.");
        }

        /// <summary>Seeks the music source to the given time and resumes playback.</summary>
        /// <param name="time">Preview start time in seconds.</param>
        public void PreviewFromTime(float time)
        {
            if (musicSource == null || musicSource.clip == null) return;
            musicSource.time = Mathf.Clamp(time, 0f, musicSource.clip.length);
            if (!musicSource.isPlaying) musicSource.Play();
            Debug.Log($"[SWEF] ReplayMusicMixer: Previewing from {time:F2}s.");
        }

        #endregion

        #region Internals

        private IEnumerator FadeCoroutine(float fromVolume, float toVolume, float duration)
        {
            if (musicSource == null) yield break;

            if (toVolume > 0f && !musicSource.isPlaying)
                musicSource.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(fromVolume, toVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            musicSource.volume = toVolume;

            if (toVolume <= 0f)
                musicSource.Stop();

            Debug.Log($"[SWEF] ReplayMusicMixer: Fade complete (volume = {toVolume:F2}).");
        }

        #endregion
    }
}
