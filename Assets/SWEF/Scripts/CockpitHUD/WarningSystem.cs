// WarningSystem.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>Severity level for HUD warnings.</summary>
    public enum WarningLevel { Info, Caution, Warning, Critical }

    /// <summary>A single active warning entry.</summary>
    [Serializable]
    public struct WarningMessage
    {
        /// <summary>Short identifier code (e.g., "STALL", "OVERSPEED").</summary>
        public string code;
        /// <summary>Human-readable warning description.</summary>
        public string message;
        /// <summary>Severity of this warning.</summary>
        public WarningLevel level;
        /// <summary>Time (Time.time) when this warning was first triggered.</summary>
        public float timestamp;
    }

    /// <summary>
    /// Phase 65 — Centralized warning and caution system for the HUD.
    ///
    /// <para>Evaluates <see cref="FlightData"/> every frame, raises and clears
    /// warnings automatically, and displays the most critical active warning on
    /// a text panel.  Audio cues are played per severity level.</para>
    ///
    /// <para>Hook up to a <see cref="FlightDataProvider"/> via
    /// <see cref="FlightDataProvider.OnFlightDataUpdated"/> or call
    /// <see cref="EvaluateFlightData"/> manually.</para>
    /// </summary>
    public class WarningSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Warning System — Display")]
        [Tooltip("Text element that shows the most critical active warning.")]
        [SerializeField] private TextMeshProUGUI warningText;

        [Tooltip("Background panel image that changes color by warning level.")]
        [SerializeField] private Image warningPanel;

        [Header("Warning System — Audio")]
        [Tooltip("AudioSource used for warning tones.")]
        [SerializeField] private AudioSource warningAudio;

        [Tooltip("Audio clips played for each severity level (index matches WarningLevel enum).")]
        [SerializeField] private AudioClip[] warningClips;

        [Header("Warning System — Data")]
        [Tooltip("FlightDataProvider to monitor. Auto-resolved on Start if null.")]
        [SerializeField] private FlightDataProvider dataProvider;

        [Header("Warning System — Thresholds")]
        [Tooltip("AGL altitude (m) below which a LOW ALTITUDE warning is issued.")]
        [SerializeField] private float lowAltitudeWarning = 100f;

        [Tooltip("Descent rate (m/s) below which a HIGH DESCENT warning is issued.")]
        [SerializeField] private float highDescentRate = 15f;

        [Tooltip("G-force above which a HIGH-G warning is issued.")]
        [SerializeField] private float highGWarning = 5f;

        [Tooltip("Fuel fraction (0–1) below which a LOW FUEL caution is issued.")]
        [SerializeField] private float lowFuelWarning = CockpitHUDConfig.DefaultLowFuel;

        #endregion

        #region Public State & Events

        /// <summary>All currently active warnings (read-only snapshot).</summary>
        public IReadOnlyList<WarningMessage> ActiveWarnings => _activeWarnings;

        /// <summary>Fired each time a new warning is added.</summary>
        public event Action<WarningMessage> OnWarningTriggered;

        /// <summary>Fired each time a warning is cleared.</summary>
        public event Action<string> OnWarningCleared;

        #endregion

        #region Private State

        private readonly List<WarningMessage> _activeWarnings = new List<WarningMessage>();
        private bool _masterCautionAcknowledged;
        private float _panelAlpha;

        // Warning code constants.
        private const string CodeStall       = "STALL";
        private const string CodeOverspeed   = "OVERSPEED";
        private const string CodeLowAlt      = "LOW ALT";
        private const string CodeHighDescent = "PULL UP";
        private const string CodeHighG       = "HIGH G";
        private const string CodeLowFuel     = "FUEL LOW";

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (dataProvider == null)
                dataProvider = FindFirstObjectByType<FlightDataProvider>();

            if (dataProvider != null)
                dataProvider.OnFlightDataUpdated += EvaluateFlightData;
        }

        private void OnDestroy()
        {
            if (dataProvider != null)
                dataProvider.OnFlightDataUpdated -= EvaluateFlightData;
        }

        private void Update()
        {
            UpdateDisplay();
        }

        #endregion

        #region Public API

        /// <summary>Manually adds a warning to the active list.</summary>
        /// <param name="code">Unique warning code.</param>
        /// <param name="message">Human-readable message.</param>
        /// <param name="level">Severity level.</param>
        public void AddWarning(string code, string message, WarningLevel level)
        {
            // Do not duplicate.
            for (int i = 0; i < _activeWarnings.Count; i++)
            {
                if (_activeWarnings[i].code == code) return;
            }

            var warn = new WarningMessage
            {
                code      = code,
                message   = message,
                level     = level,
                timestamp = Time.time
            };
            _activeWarnings.Add(warn);
            _masterCautionAcknowledged = false;
            PlayAudio(level);
            OnWarningTriggered?.Invoke(warn);
        }

        /// <summary>Removes the warning with the specified code.</summary>
        /// <param name="code">Code of the warning to clear.</param>
        public void ClearWarning(string code)
        {
            for (int i = _activeWarnings.Count - 1; i >= 0; i--)
            {
                if (_activeWarnings[i].code == code)
                {
                    _activeWarnings.RemoveAt(i);
                    OnWarningCleared?.Invoke(code);
                    return;
                }
            }
        }

        /// <summary>Acknowledges the master caution — suppresses further audio until a new warning occurs.</summary>
        public void AcknowledgeWarning()
        {
            _masterCautionAcknowledged = true;
            if (warningAudio != null && warningAudio.isPlaying)
                warningAudio.Stop();
        }

        #endregion

        #region Flight Data Evaluation

        /// <summary>Evaluates flight data and updates the active warning list.</summary>
        /// <param name="data">Latest flight data snapshot.</param>
        public void EvaluateFlightData(FlightData data)
        {
            EvaluateFlag(data.isStalling,
                CodeStall, "STALL WARNING", WarningLevel.Critical);

            EvaluateFlag(data.isOverspeed,
                CodeOverspeed, "OVERSPEED", WarningLevel.Warning);

            EvaluateFlag(data.altitudeAGL < lowAltitudeWarning,
                CodeLowAlt, "LOW ALTITUDE", WarningLevel.Warning);

            EvaluateFlag(data.verticalSpeed < -highDescentRate,
                CodeHighDescent, "PULL UP", WarningLevel.Critical);

            EvaluateFlag(data.gForce > highGWarning,
                CodeHighG, "HIGH G-FORCE", WarningLevel.Caution);

            EvaluateFlag(data.fuelPercent < lowFuelWarning,
                CodeLowFuel, "FUEL LOW", WarningLevel.Caution);
        }

        private void EvaluateFlag(bool condition, string code, string message, WarningLevel level)
        {
            if (condition)
                AddWarning(code, message, level);
            else
                ClearWarning(code);
        }

        #endregion

        #region Display

        private void UpdateDisplay()
        {
            if (_activeWarnings.Count == 0)
            {
                if (warningText  != null) warningText.gameObject.SetActive(false);
                if (warningPanel != null) warningPanel.gameObject.SetActive(false);
                return;
            }

            // Find the most critical warning.
            WarningMessage top = _activeWarnings[0];
            for (int i = 1; i < _activeWarnings.Count; i++)
            {
                if ((int)_activeWarnings[i].level > (int)top.level)
                    top = _activeWarnings[i];
            }

            if (warningText != null)
            {
                warningText.gameObject.SetActive(true);
                warningText.text  = top.message;
                warningText.color = LevelColor(top.level);
            }

            if (warningPanel != null)
            {
                warningPanel.gameObject.SetActive(true);
                Color panelColor = LevelColor(top.level);
                panelColor.a      = 0.35f;
                warningPanel.color = panelColor;
            }
        }

        private static Color LevelColor(WarningLevel level) => level switch
        {
            WarningLevel.Info     => CockpitHUDConfig.SafeColor,
            WarningLevel.Caution  => CockpitHUDConfig.CautionColor,
            WarningLevel.Warning  => CockpitHUDConfig.WarningColor,
            WarningLevel.Critical => CockpitHUDConfig.CriticalColor,
            _                     => Color.white
        };

        #endregion

        #region Audio

        private void PlayAudio(WarningLevel level)
        {
            if (_masterCautionAcknowledged) return;
            if (warningAudio == null || warningClips == null) return;
            int idx = (int)level;
            if (idx < warningClips.Length && warningClips[idx] != null)
            {
                warningAudio.Stop();
                warningAudio.clip = warningClips[idx];
                warningAudio.Play();
            }
        }

        #endregion
    }
}
