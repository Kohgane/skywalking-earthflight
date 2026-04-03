// FlashWarningController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that intercepts flash/strobe visual effects and replaces them
    /// with a non-flashing warning icon when the <c>flashWarning</c> accessibility
    /// setting is enabled.
    ///
    /// <para>Other scripts should call <see cref="TriggerFlash"/> instead of directly
    /// playing a strobe effect.  When the setting is disabled the supplied action is
    /// executed normally; when enabled the icon is shown instead.</para>
    /// </summary>
    public class FlashWarningController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static FlashWarningController Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Warning Icon")]
        [SerializeField] private Image  warningIcon;
        [SerializeField] private Text   warningLabel;
        [SerializeField] private float  iconDisplayDuration = 3f;
        [SerializeField] private Color  iconColor           = Color.yellow;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool      _flashWarningEnabled;
        private Coroutine _hideCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (warningIcon  != null) warningIcon.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (AccessibilityManager.Instance != null)
            {
                _flashWarningEnabled = AccessibilityManager.Instance.Profile.flashWarning;
                AccessibilityManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void OnDestroy()
        {
            if (AccessibilityManager.Instance != null)
                AccessibilityManager.Instance.OnProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            if (AccessibilityManager.Instance != null)
                _flashWarningEnabled = AccessibilityManager.Instance.Profile.flashWarning;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers a flash effect.  When flash-warning mode is active the
        /// <paramref name="flashAction"/> is suppressed and a warning icon is shown
        /// with the supplied <paramref name="description"/> label instead.
        /// </summary>
        /// <param name="flashAction">The strobe/flash coroutine or action to play normally.</param>
        /// <param name="description">Short description shown on the icon (e.g. "Lightning strike").</param>
        public void TriggerFlash(Action flashAction, string description = "Flash effect")
        {
            if (_flashWarningEnabled)
            {
                ShowIcon(description);
            }
            else
            {
                flashAction?.Invoke();
            }
        }

        /// <summary>Shows the warning icon for <see cref="iconDisplayDuration"/> seconds.</summary>
        public void ShowIcon(string label = "")
        {
            if (warningIcon == null) return;

            warningIcon.color = iconColor;
            warningIcon.gameObject.SetActive(true);

            if (warningLabel != null)
                warningLabel.text = label;

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(iconDisplayDuration);
            if (warningIcon != null)
                warningIcon.gameObject.SetActive(false);
        }
    }
}
