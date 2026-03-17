using System.Collections;
using UnityEngine;

namespace SWEF.Recorder
{
    /// <summary>
    /// Plays back recorded flight data by moving a ghost object along the recorded path.
    /// Can be used for replay review.
    /// </summary>
    public class FlightPlayback : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FlightRecorder recorder;
        [SerializeField] private Transform ghostObject;

        [Header("Playback Settings")]
        [SerializeField] private float playbackSpeed = 1f;

        private Coroutine _playbackCoroutine;
        private int _currentFrameIndex;

        /// <summary>Whether playback is currently active.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Current playback progress as a 0–1 fraction of total frames.</summary>
        public float PlaybackProgress01
        {
            get
            {
                if (recorder == null || recorder.Frames.Count == 0) return 0f;
                return (float)_currentFrameIndex / (recorder.Frames.Count - 1);
            }
        }

        /// <summary>Fired when playback reaches the last frame.</summary>
        public event System.Action OnPlaybackFinished;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (recorder == null)
                recorder = FindFirstObjectByType<FlightRecorder>();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Starts playback from the beginning of the recorded frames.</summary>
        public void Play()
        {
            if (IsPlaying) return;
            if (recorder == null || recorder.Frames.Count < 2)
            {
                Debug.LogWarning("[SWEF] FlightPlayback: No recording data to play.");
                return;
            }

            _currentFrameIndex = 0;
            IsPlaying = true;
            _playbackCoroutine = StartCoroutine(PlaybackCoroutine());
            Debug.Log("[SWEF] FlightPlayback: Playback started.");
        }

        /// <summary>Stops active playback.</summary>
        public void Stop()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
            Debug.Log("[SWEF] FlightPlayback: Playback stopped.");
        }

        /// <summary>Sets the playback speed multiplier (clamped 0.25–4.0).</summary>
        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.25f, 4f);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private IEnumerator PlaybackCoroutine()
        {
            var frames = recorder.Frames;

            for (int i = 0; i < frames.Count - 1; i++)
            {
                _currentFrameIndex = i;

                if (!IsPlaying) yield break;

                var from = frames[i];
                var to   = frames[i + 1];

                float segmentDuration = (to.time - from.time) / Mathf.Max(playbackSpeed, 0.001f);
                float elapsed = 0f;

                while (elapsed < segmentDuration)
                {
                    if (!IsPlaying) yield break;

                    float t = segmentDuration > 0f ? elapsed / segmentDuration : 1f;

                    if (ghostObject != null)
                    {
                        ghostObject.position = Vector3.Lerp(from.position, to.position, t);
                        ghostObject.rotation = Quaternion.Lerp(from.rotation, to.rotation, t);
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Snap to last frame
            _currentFrameIndex = frames.Count - 1;
            if (ghostObject != null)
            {
                var last = frames[_currentFrameIndex];
                ghostObject.position = last.position;
                ghostObject.rotation = last.rotation;
            }

            IsPlaying = false;
            Debug.Log("[SWEF] FlightPlayback: Playback finished.");
            OnPlaybackFinished?.Invoke();
        }
    }
}
