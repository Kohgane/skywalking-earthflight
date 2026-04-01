using System;
using System.Collections;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton MonoBehaviour that plays, previews, and stops visual transitions
    /// between replay clips.  Each transition type is driven by a coroutine and
    /// optional Unity materials.
    /// </summary>
    public class ReplayTransitionSystem : MonoBehaviour
    {
        #region Singleton

        private static ReplayTransitionSystem _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplayTransitionSystem Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplayTransitionSystem>();
                return _instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Transition Settings")]
        [SerializeField] private float defaultFadeDuration         = 0.5f;
        [SerializeField] private float defaultCrossDissolveDuration = 1f;

        [Header("Materials")]
        [SerializeField] private Material fadeMaterial;
        [SerializeField] private Material wipeMaterial;

        #endregion

        #region State

        private bool      _isTransitioning;
        private Coroutine _activeCoroutine;

        #endregion

        #region Events

        /// <summary>Fired when a transition begins.  Parameter is the transition type.</summary>
        public event Action<TransitionType> OnTransitionStarted;

        /// <summary>Fired when the active transition finishes.</summary>
        public event Action OnTransitionCompleted;

        #endregion

        #region Properties

        /// <summary>Whether a transition is currently in progress.</summary>
        public bool IsTransitioning => _isTransitioning;

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
        /// Plays the specified transition over the given duration.
        /// If a transition is already active it is stopped first.
        /// </summary>
        /// <param name="type">Type of transition to play.</param>
        /// <param name="duration">Duration in seconds.  A value ≤ 0 uses the system default.</param>
        /// <param name="onComplete">Optional callback invoked when the transition finishes.</param>
        public void PlayTransition(TransitionType type, float duration, Action onComplete = null)
        {
            StopTransition();

            float d = duration > 0f ? duration : GetDefaultDuration(type);

            _isTransitioning = true;
            OnTransitionStarted?.Invoke(type);
            Debug.Log($"[SWEF] ReplayTransitionSystem: Starting {type} transition ({d:F2}s).");

            _activeCoroutine = StartCoroutine(RunTransition(type, d, onComplete));
        }

        /// <summary>
        /// Previews the transition effect without advancing playback (single-frame preview).
        /// </summary>
        /// <param name="type">Type of transition to preview.</param>
        public void PreviewTransition(TransitionType type)
        {
            Debug.Log($"[SWEF] ReplayTransitionSystem: Preview {type}.");
            // Preview plays a very short version and auto-reverses
            PlayTransition(type, 0.2f, () => Debug.Log("[SWEF] ReplayTransitionSystem: Preview complete."));
        }

        /// <summary>Immediately stops any active transition and resets materials.</summary>
        public void StopTransition()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            ResetMaterials();
            _isTransitioning = false;
        }

        #endregion

        #region Internals

        private IEnumerator RunTransition(TransitionType type, float duration, Action onComplete)
        {
            switch (type)
            {
                case TransitionType.Fade:           yield return FadeCoroutine(duration);           break;
                case TransitionType.CrossDissolve:  yield return CrossDissolveCoroutine(duration);  break;
                case TransitionType.Wipe:           yield return WipeCoroutine(duration);           break;
                case TransitionType.Zoom:           yield return ZoomCoroutine(duration);           break;
                case TransitionType.Slide:          yield return SlideCoroutine(duration);          break;
                default:                            yield return null;                              break;
            }

            _isTransitioning = false;
            ResetMaterials();
            onComplete?.Invoke();
            OnTransitionCompleted?.Invoke();
            Debug.Log($"[SWEF] ReplayTransitionSystem: {type} transition completed.");
        }

        private IEnumerator FadeCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Fade out then in — peak darkness at t=0.5
                float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
                if (fadeMaterial != null)
                    fadeMaterial.SetFloat("_Alpha", alpha);
                yield return null;
            }
        }

        private IEnumerator CrossDissolveCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (fadeMaterial != null)
                    fadeMaterial.SetFloat("_Blend", t);
                yield return null;
            }
        }

        private IEnumerator WipeCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (wipeMaterial != null)
                    wipeMaterial.SetFloat("_Progress", t);
                yield return null;
            }
        }

        private IEnumerator ZoomCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(1f, 1.5f, t);
                if (fadeMaterial != null)
                    fadeMaterial.SetFloat("_Scale", scale);
                yield return null;
            }
        }

        private IEnumerator SlideCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t      = Mathf.Clamp01(elapsed / duration);
                float offset = Mathf.Lerp(0f, 1f, t);
                if (wipeMaterial != null)
                    wipeMaterial.SetFloat("_Offset", offset);
                yield return null;
            }
        }

        private void ResetMaterials()
        {
            if (fadeMaterial != null)
            {
                fadeMaterial.SetFloat("_Alpha", 0f);
                fadeMaterial.SetFloat("_Blend", 0f);
                fadeMaterial.SetFloat("_Scale", 1f);
            }
            if (wipeMaterial != null)
            {
                wipeMaterial.SetFloat("_Progress", 0f);
                wipeMaterial.SetFloat("_Offset",   0f);
            }
        }

        private float GetDefaultDuration(TransitionType type)
        {
            return type == TransitionType.CrossDissolve
                ? defaultCrossDissolveDuration
                : defaultFadeDuration;
        }

        #endregion
    }
}
