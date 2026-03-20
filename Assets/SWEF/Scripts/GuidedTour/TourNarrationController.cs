using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Queue-based narration controller that plays audio clips and subtitle text
    /// when tour waypoints are reached.  Integrates with <see cref="LocalizationManager"/>
    /// for translated subtitle text and never overlaps concurrent audio.
    /// </summary>
    public class TourNarrationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Audio")]
        [SerializeField] private AudioSource narrationAudioSource;
        [SerializeField] [Range(0f, 1f)] private float narrationVolume = 1f;

        [Header("Subtitles")]
        [SerializeField] private GameObject subtitlePanel;
        [SerializeField] private Text       subtitleText;
        [SerializeField] private float      subtitleFadeTime = 0.4f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when narration for <paramref name="narrationKey"/> begins playing.</summary>
        public event Action<string> OnNarrationStarted;

        /// <summary>Fired when narration for <paramref name="narrationKey"/> finishes or is skipped.</summary>
        public event Action<string> OnNarrationFinished;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Queue<NarrationRequest> _queue = new Queue<NarrationRequest>();
        private Coroutine  _playbackCoroutine;
        private string     _currentKey;

        private struct NarrationRequest
        {
            public string     key;
            public AudioClip  clip;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (narrationAudioSource == null)
                narrationAudioSource = gameObject.AddComponent<AudioSource>();

            narrationAudioSource.volume = narrationVolume;

            var tourManager = FindFirstObjectByType<TourManager>();
            if (tourManager != null)
            {
                tourManager.OnWaypointReached += (idx, wp) =>
                    PlayNarration(wp.narrationKey, null);
                tourManager.OnTourCancelled += _ => SkipNarration();
            }

            if (subtitlePanel != null) subtitlePanel.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a narration entry.  If an audio source is not playing,
        /// playback starts immediately; otherwise the entry is appended to the queue.
        /// </summary>
        /// <param name="key">Localization key used to fetch the subtitle text.</param>
        /// <param name="clip">Optional audio clip to play. Pass <c>null</c> to show subtitles only.</param>
        public void PlayNarration(string key, AudioClip clip)
        {
            if (string.IsNullOrEmpty(key)) return;
            _queue.Enqueue(new NarrationRequest { key = key, clip = clip });

            if (_playbackCoroutine == null)
                _playbackCoroutine = StartCoroutine(PlaybackLoop());
        }

        /// <summary>
        /// Stops the currently playing narration and advances to the next item in the queue.
        /// </summary>
        public void SkipNarration()
        {
            if (narrationAudioSource != null && narrationAudioSource.isPlaying)
                narrationAudioSource.Stop();

            _queue.Clear();

            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }

            if (!string.IsNullOrEmpty(_currentKey))
                OnNarrationFinished?.Invoke(_currentKey);

            _currentKey = null;
            StartCoroutine(HideSubtitle());
        }

        /// <summary>Sets the volume of the narration audio source.</summary>
        /// <param name="volume">Volume in [0, 1].</param>
        public void SetNarrationVolume(float volume)
        {
            narrationVolume = Mathf.Clamp01(volume);
            if (narrationAudioSource != null)
                narrationAudioSource.volume = narrationVolume;
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private IEnumerator PlaybackLoop()
        {
            while (_queue.Count > 0)
            {
                var req = _queue.Dequeue();
                _currentKey = req.key;

                // Resolve subtitle text via localization.
                string subtitle = req.key;
                var    locMgr   = LocalizationManager.Instance;
                if (locMgr != null)
                    subtitle = locMgr.GetText(req.key);

                ShowSubtitle(subtitle);
                OnNarrationStarted?.Invoke(req.key);

                // Play audio if provided.
                if (req.clip != null && narrationAudioSource != null)
                {
                    narrationAudioSource.clip   = req.clip;
                    narrationAudioSource.volume = narrationVolume;
                    narrationAudioSource.Play();

                    // Wait until the clip finishes.
                    while (narrationAudioSource.isPlaying)
                        yield return null;
                }
                else
                {
                    // Text-only: hold for a fixed duration so the player can read it.
                    yield return new WaitForSeconds(4f);
                }

                OnNarrationFinished?.Invoke(req.key);
                _currentKey = null;
            }

            // Queue empty — hide subtitle.
            yield return HideSubtitle();
            _playbackCoroutine = null;
        }

        private void ShowSubtitle(string text)
        {
            if (subtitlePanel == null) return;
            subtitlePanel.SetActive(true);
            if (subtitleText != null) subtitleText.text = text;
        }

        private IEnumerator HideSubtitle()
        {
            if (subtitlePanel == null) yield break;

            var cg = subtitlePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float t = 0f;
                while (t < subtitleFadeTime)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(1f, 0f, t / subtitleFadeTime);
                    yield return null;
                }
                cg.alpha = 0f;
            }

            subtitlePanel.SetActive(false);
            if (subtitleText != null) subtitleText.text = string.Empty;

            // Restore alpha so the subtitle can fade in again on the next narration.
            if (cg != null) cg.alpha = 1f;
        }
    }
}
