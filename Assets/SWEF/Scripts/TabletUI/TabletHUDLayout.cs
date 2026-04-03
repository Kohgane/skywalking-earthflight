// TabletHUDLayout.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that manages a tablet-optimised cockpit HUD layout.
    ///
    /// <para>On tablets (<see cref="LayoutMode.Standard"/> or
    /// <see cref="LayoutMode.Expanded"/>) the HUD switches to wider instrument
    /// panel spacing and enables collapsible side panels (minimap, mission info,
    /// nav data).  On phones the compact single-panel layout is used instead.</para>
    ///
    /// <para>Side panels can be collapsed / expanded via swipe gestures or the
    /// public API.  The manager integrates with <see cref="TabletLayoutManager"/>
    /// to react automatically to screen-size changes.</para>
    /// </summary>
    public class TabletHUDLayout : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletHUDLayout Instance { get; private set; }

        // ── Inspector — Panel References ──────────────────────────────────────────
        [Header("HUD Panels")]
        [Tooltip("Root RectTransform of the main instrument panel.")]
        [SerializeField] private RectTransform instrumentPanel;

        [Tooltip("Root RectTransform of the left side panel (e.g. minimap).")]
        [SerializeField] private RectTransform leftSidePanel;

        [Tooltip("Root RectTransform of the right side panel (e.g. nav/mission data).")]
        [SerializeField] private RectTransform rightSidePanel;

        // ── Inspector — Layout Settings ───────────────────────────────────────────
        [Header("Layout Settings")]
        [Tooltip("Width of each side panel in pixels when expanded (tablet mode).")]
        [SerializeField] private float sidePanelExpandedWidth = 280f;

        [Tooltip("Width of each side panel in pixels when collapsed.")]
        [SerializeField] private float sidePanelCollapsedWidth = 0f;

        [Tooltip("Seconds for the collapse/expand animation.")]
        [SerializeField] private float panelAnimationDuration = 0.25f;

        [Header("Swipe Gesture")]
        [Tooltip("Minimum horizontal swipe distance (pixels) to toggle a side panel.")]
        [SerializeField] private float swipeThresholdPx = 60f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool  _leftPanelExpanded  = true;
        private bool  _rightPanelExpanded = true;
        private bool  _isTabletLayout;

        private Vector2 _touchStart;
        private bool    _trackingSwipe;

        // Side panel animation
        private float _leftCurrentWidth;
        private float _rightCurrentWidth;
        private float _leftTargetWidth;
        private float _rightTargetWidth;

        /// <summary>True when the tablet-mode HUD layout is active.</summary>
        public bool IsTabletLayout => _isTabletLayout;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a side panel is toggled. Parameters: panel name, new expanded state.</summary>
        public event Action<string, bool> OnPanelToggled;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (TabletLayoutManager.Instance != null)
                TabletLayoutManager.Instance.OnLayoutModeChanged += OnLayoutModeChanged;

            LayoutMode initial = TabletLayoutManager.Instance != null
                ? TabletLayoutManager.Instance.CurrentMode
                : LayoutMode.Compact;

            ApplyLayout(initial);
        }

        private void OnDestroy()
        {
            if (TabletLayoutManager.Instance != null)
                TabletLayoutManager.Instance.OnLayoutModeChanged -= OnLayoutModeChanged;
        }

        private void Update()
        {
            AnimatePanels();
            HandleSwipeInput();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Expand or collapse the left side panel.</summary>
        public void SetLeftPanelExpanded(bool expanded)
        {
            if (leftSidePanel == null) return;
            _leftPanelExpanded = expanded;
            _leftTargetWidth   = expanded && _isTabletLayout
                ? sidePanelExpandedWidth
                : sidePanelCollapsedWidth;
            OnPanelToggled?.Invoke("Left", expanded);
        }

        /// <summary>Expand or collapse the right side panel.</summary>
        public void SetRightPanelExpanded(bool expanded)
        {
            if (rightSidePanel == null) return;
            _rightPanelExpanded = expanded;
            _rightTargetWidth   = expanded && _isTabletLayout
                ? sidePanelExpandedWidth
                : sidePanelCollapsedWidth;
            OnPanelToggled?.Invoke("Right", expanded);
        }

        /// <summary>Toggle the left side panel.</summary>
        public void ToggleLeftPanel()  => SetLeftPanelExpanded(!_leftPanelExpanded);

        /// <summary>Toggle the right side panel.</summary>
        public void ToggleRightPanel() => SetRightPanelExpanded(!_rightPanelExpanded);

        // ── Internal ──────────────────────────────────────────────────────────────
        private void OnLayoutModeChanged(LayoutMode mode) => ApplyLayout(mode);

        private void ApplyLayout(LayoutMode mode)
        {
            _isTabletLayout = mode == LayoutMode.Standard || mode == LayoutMode.Expanded;

            if (_isTabletLayout)
            {
                _leftTargetWidth  = _leftPanelExpanded  ? sidePanelExpandedWidth : sidePanelCollapsedWidth;
                _rightTargetWidth = _rightPanelExpanded ? sidePanelExpandedWidth : sidePanelCollapsedWidth;
            }
            else
            {
                _leftTargetWidth  = sidePanelCollapsedWidth;
                _rightTargetWidth = sidePanelCollapsedWidth;
            }

            Debug.Log($"[SWEF] TabletHUDLayout: layout mode '{mode}', isTablet={_isTabletLayout}.");
        }

        private void AnimatePanels()
        {
            if (panelAnimationDuration <= 0f)
            {
                _leftCurrentWidth  = _leftTargetWidth;
                _rightCurrentWidth = _rightTargetWidth;
            }
            else
            {
                float step = Time.unscaledDeltaTime / panelAnimationDuration;
                _leftCurrentWidth  = Mathf.MoveTowards(_leftCurrentWidth,  _leftTargetWidth,  step * sidePanelExpandedWidth);
                _rightCurrentWidth = Mathf.MoveTowards(_rightCurrentWidth, _rightTargetWidth, step * sidePanelExpandedWidth);
            }

            SetPanelWidth(leftSidePanel,  _leftCurrentWidth);
            SetPanelWidth(rightSidePanel, _rightCurrentWidth);
        }

        private static void SetPanelWidth(RectTransform rt, float width)
        {
            if (rt == null) return;
            Vector2 size = rt.sizeDelta;
            size.x = width;
            rt.sizeDelta = size;

            bool visible = width > 1f;
            if (rt.gameObject.activeSelf != visible)
                rt.gameObject.SetActive(visible);
        }

        private void HandleSwipeInput()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _touchStart    = touch.position;
                    _trackingSwipe = true;
                }
                else if (_trackingSwipe && touch.phase == TouchPhase.Ended)
                {
                    _trackingSwipe = false;
                    float dx = touch.position.x - _touchStart.x;
                    if (Mathf.Abs(dx) >= swipeThresholdPx)
                    {
                        if (dx > 0) ToggleLeftPanel();
                        else        ToggleRightPanel();
                    }
                }
            }
#endif
        }
    }
}
