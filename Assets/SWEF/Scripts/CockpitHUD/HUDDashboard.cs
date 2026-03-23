// HUDDashboard.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>Visibility / information-density mode of the HUD.</summary>
    public enum HUDMode
    {
        /// <summary>Only the most critical instruments are shown.</summary>
        Minimal,
        /// <summary>Standard complement of instruments (default).</summary>
        Standard,
        /// <summary>All instruments visible.</summary>
        Full,
        /// <summary>HUD completely hidden for cinematic use.</summary>
        CinematicOff
    }

    /// <summary>
    /// Phase 65 — Singleton master controller for the entire HUD Dashboard system.
    ///
    /// <para>Manages global opacity, mode switching, auto-hide, and per-frame
    /// distribution of <see cref="FlightData"/> to all registered
    /// <see cref="HUDInstrument"/> instances.</para>
    ///
    /// <para>Attach to a persistent canvas GameObject that has a
    /// <see cref="CanvasGroup"/> component.</para>
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HUDDashboard : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static HUDDashboard Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Mode")]
        [Tooltip("Active HUD information-density mode.")]
        [SerializeField] private HUDMode currentMode = HUDMode.Standard;

        [Header("Opacity")]
        [Tooltip("Global HUD opacity (0 = transparent, 1 = fully opaque).")]
        [Range(0f, 1f)]
        [SerializeField] private float hudOpacity = CockpitHUDConfig.DefaultHUDOpacity;

        [Tooltip("Speed at which the HUD opacity fades in or out (alpha/second).")]
        [SerializeField] private float opacityFadeSpeed = 3f;

        [Header("Auto-Hide")]
        [Tooltip("Automatically hide the HUD after a period of no player input.")]
        [SerializeField] private bool autoHide = true;

        [Tooltip("Seconds of idle input before the HUD is hidden automatically.")]
        [SerializeField] private float autoHideDelay = CockpitHUDConfig.AutoHideDelay;

        [Header("Data Source")]
        [Tooltip("FlightDataProvider to pull live data from each frame.")]
        [SerializeField] private FlightDataProvider dataProvider;

        #endregion

        #region Public Properties & Events

        /// <summary>Current HUD display mode.</summary>
        public HUDMode CurrentMode => currentMode;

        /// <summary>Global HUD canvas group used for whole-HUD fade.</summary>
        public CanvasGroup HudCanvasGroup { get; private set; }

        /// <summary>Fired whenever the HUD mode changes.</summary>
        public event Action<HUDMode> OnModeChanged;

        #endregion

        #region Private State

        private readonly List<HUDInstrument> _instruments = new List<HUDInstrument>();
        private float _idleTimer;
        private float _targetOpacity;
        private bool  _hidden;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            HudCanvasGroup = GetComponent<CanvasGroup>();
            _targetOpacity = hudOpacity;
        }

        private void Start()
        {
            if (dataProvider == null)
                dataProvider = FindFirstObjectByType<FlightDataProvider>();

            ApplyMode(currentMode, instant: true);
        }

        private void Update()
        {
            // ── Auto-hide logic ──────────────────────────────────────────────
            if (autoHide)
            {
                bool anyInput = DetectInput();
                if (anyInput)
                {
                    _idleTimer = 0f;
                    if (_hidden) ShowHUD();
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= autoHideDelay && !_hidden)
                        HideHUD();
                }
            }

            // ── Smooth opacity transition ────────────────────────────────────
            if (HudCanvasGroup != null)
            {
                HudCanvasGroup.alpha = Mathf.MoveTowards(
                    HudCanvasGroup.alpha, _targetOpacity, opacityFadeSpeed * Time.deltaTime);
            }

            // ── Distribute flight data to instruments ────────────────────────
            if (dataProvider != null)
            {
                FlightData data = dataProvider.CurrentData;
                for (int i = 0; i < _instruments.Count; i++)
                {
                    if (_instruments[i] != null && _instruments[i].IsVisible)
                        _instruments[i].UpdateInstrument(data);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>Switches the HUD to the specified mode and updates instrument visibility.</summary>
        /// <param name="mode">Target HUD mode.</param>
        public void SetMode(HUDMode mode)
        {
            if (currentMode == mode) return;
            currentMode = mode;
            ApplyMode(mode);
            OnModeChanged?.Invoke(mode);
        }

        /// <summary>Cycles through HUD modes in order: Minimal → Standard → Full → CinematicOff → Minimal.</summary>
        public void ToggleHUD()
        {
            HUDMode next = currentMode switch
            {
                HUDMode.Minimal      => HUDMode.Standard,
                HUDMode.Standard     => HUDMode.Full,
                HUDMode.Full         => HUDMode.CinematicOff,
                HUDMode.CinematicOff => HUDMode.Minimal,
                _                    => HUDMode.Standard
            };
            SetMode(next);
        }

        /// <summary>Registers a new instrument so the dashboard can update it each frame.</summary>
        /// <param name="instrument">The instrument to register.</param>
        public void RegisterInstrument(HUDInstrument instrument)
        {
            if (instrument != null && !_instruments.Contains(instrument))
            {
                _instruments.Add(instrument);
                ApplyModeToInstrument(instrument, currentMode);
            }
        }

        /// <summary>Removes an instrument from the dashboard's update list.</summary>
        /// <param name="instrument">The instrument to unregister.</param>
        public void UnregisterInstrument(HUDInstrument instrument)
        {
            _instruments.Remove(instrument);
        }

        /// <summary>Sets the global HUD opacity target (0–1).</summary>
        /// <param name="opacity">Desired opacity.</param>
        public void SetOpacity(float opacity)
        {
            hudOpacity     = Mathf.Clamp01(opacity);
            _targetOpacity = _hidden ? 0f : hudOpacity;
        }

        #endregion

        #region Private Helpers

        private void ApplyMode(HUDMode mode, bool instant = false)
        {
            foreach (HUDInstrument inst in _instruments)
            {
                if (inst == null) continue;
                ApplyModeToInstrument(inst, mode);
            }

            // CinematicOff hides the whole canvas.
            if (mode == HUDMode.CinematicOff)
                HideHUD();
            else
                ShowHUD();
        }

        private static void ApplyModeToInstrument(HUDInstrument instrument, HUDMode mode)
        {
            if (mode == HUDMode.CinematicOff)
            {
                instrument.Hide();
                return;
            }

            bool shouldShow = (int)mode >= (int)instrument.MinimumMode;
            if (shouldShow) instrument.Show();
            else            instrument.Hide();
        }

        private void ShowHUD()
        {
            _hidden        = false;
            _targetOpacity = hudOpacity;
        }

        private void HideHUD()
        {
            _hidden        = true;
            _targetOpacity = 0f;
        }

        /// <summary>Returns <c>true</c> if any relevant player input was detected this frame.</summary>
        private static bool DetectInput()
        {
            return Input.anyKey
                || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f
                || Mathf.Abs(Input.GetAxisRaw("Vertical"))   > 0.01f;
        }

        #endregion
    }
}
