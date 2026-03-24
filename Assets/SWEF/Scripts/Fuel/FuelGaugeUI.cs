// FuelGaugeUI.cs — SWEF Fuel & Energy Management System (Phase 69)
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Dedicated fuel HUD panel that extends the basic
    /// <c>ThrottleFuelGauge</c> (Phase 65) with per-tank bars, consumption readouts,
    /// refuelling progress, and warning animations.
    ///
    /// <para>Subscribes to <see cref="FuelManager"/> events at runtime; assign the
    /// manager reference in the inspector or let it auto-discover the singleton.</para>
    /// </summary>
    public class FuelGaugeUI : MonoBehaviour
    {
        #region Inspector

        [Header("Manager Reference")]
        [Tooltip("FuelManager to observe. Leave null to use FuelManager.Instance.")]
        [SerializeField] private FuelManager fuelManager;

        [Header("Per-Tank Bars")]
        [Tooltip("Fill-image bars, one per tank (order matches FuelManager.Tanks).")]
        [SerializeField] private Image[] tankBars;

        [Header("Text Readouts")]
        [Tooltip("Shows total fuel in litres, e.g. \"1,234 L\".")]
        [SerializeField] private TextMeshProUGUI totalFuelText;

        [Tooltip("Shows estimated flight time remaining, e.g. \"Est. 00:45:32\".")]
        [SerializeField] private TextMeshProUGUI estimatedTimeText;

        [Tooltip("Shows current consumption rate, e.g. \"2.3 L/s\".")]
        [SerializeField] private TextMeshProUGUI consumptionRateText;

        [Tooltip("Shows cumulative fuel used this flight, e.g. \"Used: 567 L\".")]
        [SerializeField] private TextMeshProUGUI fuelUsedText;

        [Header("Warning")]
        [Tooltip("Warning icon that blinks when fuel is Low or Critical.")]
        [SerializeField] private Image warningIcon;

        [Tooltip("Blink frequency in Hz for Low/Critical warnings.")]
        [SerializeField] private float warningBlinkRate = 2f;

        [Header("Colors")]
        [Tooltip("Bar color at normal fuel level.")]
        [SerializeField] private Color normalColor   = FuelConfig.ColorNormal;

        [Tooltip("Bar color at low fuel level.")]
        [SerializeField] private Color lowColor      = FuelConfig.ColorLow;

        [Tooltip("Bar color at critical fuel level.")]
        [SerializeField] private Color criticalColor = FuelConfig.ColorCritical;

        [Header("Tank Selector")]
        [Tooltip("RectTransform used to highlight the active tank in the UI (optional).")]
        [SerializeField] private RectTransform tankSelector;

        [Header("Refuel Panel")]
        [Tooltip("Root panel shown while a refuelling operation is in progress.")]
        [SerializeField] private GameObject refuelPanel;

        [Tooltip("Slider showing refuelling progress (0 = empty, 1 = full).")]
        [SerializeField] private Slider refuelProgressSlider;

        #endregion

        #region Private State

        private float       _blinkTimer;
        private bool        _warningActive;
        private Coroutine   _pulseCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (fuelManager == null)
                fuelManager = FuelManager.Instance;

            if (fuelManager != null)
            {
                fuelManager.OnFuelWarningChanged += HandleWarningChanged;
                fuelManager.OnTankSwitched       += HandleTankSwitched;
                fuelManager.OnFuelConsumed       += _ => RefreshReadouts();
            }

            HideRefuelPanel();
        }

        private void OnDestroy()
        {
            if (fuelManager != null)
            {
                fuelManager.OnFuelWarningChanged -= HandleWarningChanged;
                fuelManager.OnTankSwitched       -= HandleTankSwitched;
                fuelManager.OnFuelConsumed       -= _ => RefreshReadouts();
            }
        }

        private void Update()
        {
            RefreshTankBars();
            RefreshReadouts();
            TickWarningBlink();
        }

        #endregion

        #region Public API

        /// <summary>Shows the refuelling progress panel.</summary>
        public void ShowRefuelPanel()
        {
            if (refuelPanel != null)
                refuelPanel.SetActive(true);
        }

        /// <summary>Hides the refuelling progress panel.</summary>
        public void HideRefuelPanel()
        {
            if (refuelPanel != null)
                refuelPanel.SetActive(false);
        }

        #endregion

        #region Private — Update Helpers

        private void RefreshTankBars()
        {
            if (fuelManager == null || tankBars == null) return;

            int count = Mathf.Min(tankBars.Length, fuelManager.Tanks.Count);
            for (int i = 0; i < count; i++)
            {
                var bar  = tankBars[i];
                var tank = fuelManager.Tanks[i];
                if (bar == null) continue;

                bar.fillAmount = tank.fuelPercent;
                bar.color      = GetTankColor(tank.fuelPercent);
            }

            // Update tank selector position if assigned.
            if (tankSelector != null && fuelManager.ActiveTank != null)
            {
                int activeIdx = -1;
                for (int i = 0; i < fuelManager.Tanks.Count; i++)
                    if (fuelManager.Tanks[i] == fuelManager.ActiveTank) { activeIdx = i; break; }

                if (activeIdx >= 0 && activeIdx < tankBars.Length && tankBars[activeIdx] != null)
                    tankSelector.position = tankBars[activeIdx].transform.position;
            }
        }

        private void RefreshReadouts()
        {
            if (fuelManager == null) return;

            if (totalFuelText != null)
                totalFuelText.text = $"{fuelManager.TotalFuel:N0} L";

            if (estimatedTimeText != null)
            {
                float secs = fuelManager.EstimatedFlightTime;
                if (float.IsInfinity(secs) || secs > 99f * 3600f)
                    estimatedTimeText.text = "Est. --:--:--";
                else
                    estimatedTimeText.text =
                        $"Est. {TimeSpan.FromSeconds(secs):hh\\:mm\\:ss}";
            }

            if (consumptionRateText != null)
                consumptionRateText.text = $"{fuelManager.CurrentConsumptionRate:F1} L/s";

            if (fuelUsedText != null)
                fuelUsedText.text = $"Used: {fuelManager.FuelUsedThisFlight:N0} L";

            // Refuel slider.
            if (refuelProgressSlider != null && fuelManager != null)
                refuelProgressSlider.value = fuelManager.TotalFuelPercent;
        }

        private void TickWarningBlink()
        {
            if (warningIcon == null || !_warningActive) return;

            _blinkTimer += Time.deltaTime;
            bool visible = Mathf.Sin(_blinkTimer * warningBlinkRate * Mathf.PI * 2f) >= 0f;
            warningIcon.enabled = visible;
        }

        #endregion

        #region Private — Event Handlers

        private void HandleWarningChanged(FuelWarningLevel level)
        {
            _warningActive = level == FuelWarningLevel.Low ||
                             level == FuelWarningLevel.Critical ||
                             level == FuelWarningLevel.Empty;

            if (warningIcon != null)
            {
                warningIcon.enabled = _warningActive;
                warningIcon.color   = FuelConfig.GetWarningColor(level);
            }

            // Play pulse animation when warning level escalates.
            if (_warningActive)
            {
                if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(PulseWarningIcon());
            }
        }

        private void HandleTankSwitched(FuelTank tank)
        {
            RefreshTankBars();
        }

        #endregion

        #region Private — Animations

        private IEnumerator PulseWarningIcon()
        {
            if (warningIcon == null) yield break;

            float elapsed  = 0f;
            float duration = 0.4f;
            Vector3 origin = warningIcon.rectTransform.localScale;
            Vector3 target = origin * 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed / (duration * 0.5f), 1f);
                warningIcon.rectTransform.localScale = Vector3.Lerp(origin, target, t);
                yield return null;
            }

            warningIcon.rectTransform.localScale = origin;
            _pulseCoroutine = null;
        }

        #endregion

        #region Private — Helpers

        private Color GetTankColor(float pct)
        {
            if (pct < FuelConfig.CriticalFuelThreshold) return criticalColor;
            if (pct < FuelConfig.LowFuelThreshold)      return lowColor;
            return normalColor;
        }

        #endregion
    }
}
