// TabletKeyboardHandler.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that manages on-screen keyboard behaviour for search
    /// and input fields on tablets, and detects the presence of an external
    /// (Bluetooth / Smart Connector) keyboard.
    ///
    /// <para>When the software keyboard appears the UI layout is shifted upward so
    /// input fields remain visible.  When it disappears the layout is restored.</para>
    ///
    /// <para>On platforms that support hardware keyboard detection the
    /// <see cref="HasExternalKeyboard"/> property is updated and
    /// <see cref="OnExternalKeyboardChanged"/> is fired so the HUD can show or hide
    /// on-screen control hints.</para>
    /// </summary>
    public class TabletKeyboardHandler : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletKeyboardHandler Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Layout Adjustment")]
        [Tooltip("RectTransform of the root UI panel that should be shifted when the " +
                 "keyboard appears.  Leave null to skip layout adjustment.")]
        [SerializeField] private RectTransform managedPanel;

        [Tooltip("Extra padding (pixels) added above the keyboard when adjusting layout.")]
        [SerializeField] private float keyboardPaddingPx = 16f;

        [Tooltip("Duration (seconds) of the keyboard appear/disappear animation.")]
        [SerializeField] private float animationDuration = 0.2f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool    _keyboardVisible;
        private bool    _hasExternalKeyboard;
        private float   _keyboardHeightNorm;
        private float   _currentOffsetY;
        private float   _targetOffsetY;
        private Vector2 _originalAnchoredPosition;
        private bool    _originalPositionCached;

        /// <summary>True while the software keyboard is visible.</summary>
        public bool IsKeyboardVisible => _keyboardVisible;

        /// <summary>True when an external (hardware) keyboard is detected.</summary>
        public bool HasExternalKeyboard => _hasExternalKeyboard;

        /// <summary>Normalised height [0–1] of the currently visible software keyboard.</summary>
        public float KeyboardHeightNormalized => _keyboardHeightNorm;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the software keyboard appears or disappears.</summary>
        public event Action<bool>  OnKeyboardVisibilityChanged;

        /// <summary>Fired when external keyboard connection state changes.</summary>
        public event Action<bool>  OnExternalKeyboardChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (managedPanel != null)
            {
                _originalAnchoredPosition = managedPanel.anchoredPosition;
                _originalPositionCached   = true;
            }
        }

        private void Update()
        {
            PollKeyboardState();
            AnimatePanel();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Open the system keyboard, optionally pre-filled with <paramref name="initialText"/>.
        /// </summary>
        /// <param name="initialText">Initial text shown in the keyboard input area.</param>
        /// <param name="keyboardType">The type of keyboard to show.</param>
        /// <returns>The opened <see cref="TouchScreenKeyboard"/> instance, or null on PC.</returns>
        public TouchScreenKeyboard OpenKeyboard(
            string initialText = "",
            TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default)
        {
#if UNITY_IOS || UNITY_ANDROID
            return TouchScreenKeyboard.Open(
                initialText,
                keyboardType,
                autocorrection: true,
                multiline: false,
                secure: false,
                alert: false);
#else
            return null;
#endif
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void PollKeyboardState()
        {
#if UNITY_IOS || UNITY_ANDROID
            float heightNorm = GetKeyboardHeightNormalized();
            bool  visible    = heightNorm > 0.01f;

            if (visible != _keyboardVisible || !Mathf.Approximately(heightNorm, _keyboardHeightNorm))
            {
                _keyboardVisible   = visible;
                _keyboardHeightNorm = heightNorm;
                _targetOffsetY     = visible
                    ? (heightNorm * Screen.height) + keyboardPaddingPx
                    : 0f;
                OnKeyboardVisibilityChanged?.Invoke(_keyboardVisible);
            }

            // External hardware keyboard detection heuristic:
            // Unity reports TouchScreenKeyboard.isSupported = false on devices that
            // have a physical keyboard active (Smart Keyboard, Magic Keyboard via BT).
            bool externalKbd = !TouchScreenKeyboard.isSupported;
            if (externalKbd != _hasExternalKeyboard)
            {
                _hasExternalKeyboard = externalKbd;
                OnExternalKeyboardChanged?.Invoke(_hasExternalKeyboard);
            }
#else
            // PC always has an "external" keyboard
            if (!_hasExternalKeyboard)
            {
                _hasExternalKeyboard = true;
                OnExternalKeyboardChanged?.Invoke(true);
            }
#endif
        }

        private static float GetKeyboardHeightNormalized()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Screen.height <= 0) return 0f;
            return (float)TouchScreenKeyboard.area.height / Screen.height;
#else
            return 0f;
#endif
        }

        private void AnimatePanel()
        {
            if (managedPanel == null || !_originalPositionCached) return;
            if (animationDuration <= 0f)
            {
                _currentOffsetY = _targetOffsetY;
            }
            else
            {
                _currentOffsetY = Mathf.Lerp(
                    _currentOffsetY,
                    _targetOffsetY,
                    Time.unscaledDeltaTime / animationDuration);
            }

            managedPanel.anchoredPosition = _originalAnchoredPosition +
                                            new Vector2(0f, _currentOffsetY);
        }
    }
}
