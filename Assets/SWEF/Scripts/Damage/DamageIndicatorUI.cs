// DamageIndicatorUI.cs — SWEF Damage & Repair System (Phase 66)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Damage
{
    /// <summary>
    /// MonoBehaviour that drives the aircraft damage status HUD.
    ///
    /// <para>Subscribes to <see cref="DamageModel"/> and <see cref="RepairSystem"/>
    /// events and updates the UI in response.  Assign all UI references via the
    /// inspector.</para>
    /// </summary>
    public class DamageIndicatorUI : MonoBehaviour
    {
        #region Inspector

        [Header("Aircraft Silhouette")]
        [Tooltip("Top-down aircraft silhouette image on the HUD.")]
        /// <summary>Top-down aircraft silhouette image.</summary>
        [SerializeField] private Image aircraftSilhouette;

        [Header("Part Indicators")]
        [Tooltip("Coloured overlay images on the silhouette, one per AircraftPart. " +
                 "Must be populated in the same order as the AircraftPart enum.")]
        /// <summary>Coloured overlay images keyed by <see cref="AircraftPart"/>.</summary>
        [SerializeField] private Image[] partIndicatorImages;

        [Header("Overall Health")]
        [Tooltip("Text showing overall health percentage.")]
        /// <summary>Text element showing overall health as a percentage string.</summary>
        [SerializeField] private TextMeshProUGUI overallHealthText;

        [Tooltip("Fillable bar showing overall aircraft health (0–1).")]
        /// <summary>Fill-type <see cref="Image"/> used as the overall health bar.</summary>
        [SerializeField] private Image overallHealthBar;

        [Header("Repair UI")]
        [Tooltip("Radial or fill image showing emergency repair cooldown progress.")]
        /// <summary>Indicator image for emergency repair cooldown.</summary>
        [SerializeField] private Image repairCooldownIndicator;

        [Tooltip("Text showing remaining emergency repair charges.")]
        /// <summary>Text displaying remaining emergency repair charges.</summary>
        [SerializeField] private TextMeshProUGUI repairChargesText;

        [Header("Damage Popup")]
        [Tooltip("Container panel for the transient damage-received popup.")]
        /// <summary>Root panel of the damage popup.</summary>
        [SerializeField] private GameObject damagePopupPanel;

        [Tooltip("Text inside the damage popup.")]
        /// <summary>Text element inside the damage popup.</summary>
        [SerializeField] private TextMeshProUGUI damagePopupText;

        [Tooltip("Seconds the damage popup is visible.")]
        /// <summary>Duration the damage popup stays visible.</summary>
        [SerializeField] private float popupDuration = 2.5f;

        [Header("Pulse Animation")]
        [Tooltip("Pulse animation frequency for recently damaged part indicators (Hz).")]
        /// <summary>Frequency of the pulse animation on recently damaged parts.</summary>
        [SerializeField] private float pulseFrequency = 3f;

        [Tooltip("Seconds a part indicator continues to pulse after taking damage.")]
        /// <summary>Seconds a part indicator pulses after damage.</summary>
        [SerializeField] private float pulseDuration = 2f;

        #endregion

        #region Private State

        private DamageModel  _model;
        private RepairSystem _repair;

        private readonly Dictionary<AircraftPart, Image>    _indicators   = new Dictionary<AircraftPart, Image>();
        private readonly Dictionary<AircraftPart, float>    _pulseEndTime = new Dictionary<AircraftPart, float>();
        private readonly Dictionary<AircraftPart, Color>    _baseColors   = new Dictionary<AircraftPart, Color>();

        private Coroutine _popupCoroutine;

        #endregion

        #region Unity

        private void Awake()
        {
            _model  = GetComponentInParent<DamageModel>();
            _repair = GetComponentInParent<RepairSystem>();

            if (_model == null)
                _model = FindObjectOfType<DamageModel>();
            if (_repair == null)
                _repair = FindObjectOfType<RepairSystem>();

            BuildIndicatorDictionary();
        }

        private void OnEnable()
        {
            if (_model != null)
            {
                _model.OnDamageReceived          += HandleDamageReceived;
                _model.OnPartDamageLevelChanged   += HandlePartLevelChanged;
            }

            if (_repair != null)
            {
                _repair.OnRepairStarted   += HandleRepairStarted;
                _repair.OnRepairCompleted += HandleRepairCompleted;
                _repair.OnPartRepaired    += HandlePartRepaired;
            }
        }

        private void OnDisable()
        {
            if (_model != null)
            {
                _model.OnDamageReceived          -= HandleDamageReceived;
                _model.OnPartDamageLevelChanged   -= HandlePartLevelChanged;
            }

            if (_repair != null)
            {
                _repair.OnRepairStarted   -= HandleRepairStarted;
                _repair.OnRepairCompleted -= HandleRepairCompleted;
                _repair.OnPartRepaired    -= HandlePartRepaired;
            }
        }

        private void Update()
        {
            RefreshOverallHealth();
            RefreshRepairUI();
            AnimatePulses();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Updates the colour of the part indicator overlay for <paramref name="part"/>
        /// to reflect <paramref name="level"/>.
        /// </summary>
        /// <param name="part">Part whose indicator should change.</param>
        /// <param name="level">New damage level.</param>
        public void UpdatePartIndicator(AircraftPart part, DamageLevel level)
        {
            if (!_indicators.TryGetValue(part, out Image img) || img == null) return;

            Color target = DamageConfig.GetLevelColor(level);
            img.color = target;
            _baseColors[part] = target;
        }

        /// <summary>
        /// Briefly shows a damage-received popup with the content from
        /// <paramref name="data"/> then hides it after <see cref="popupDuration"/> seconds.
        /// </summary>
        /// <param name="data">Damage event to display.</param>
        public void ShowDamagePopup(DamageData data)
        {
            if (data == null) return;

            if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
            _popupCoroutine = StartCoroutine(ShowPopupCoroutine(data));
        }

        #endregion

        #region Event Handlers

        private void HandleDamageReceived(DamageData data)
        {
            ShowDamagePopup(data);
            _pulseEndTime[data.affectedPart] = Time.time + pulseDuration;
        }

        private void HandlePartLevelChanged(AircraftPart part, DamageLevel level)
        {
            UpdatePartIndicator(part, level);
        }

        private void HandleRepairStarted(RepairMode mode) { /* future: show repair mode badge */ }

        private void HandleRepairCompleted() { /* future: flash green */ }

        private void HandlePartRepaired(AircraftPart part, float amount)
        {
            if (_model == null) return;
            PartHealth ph = _model.GetPartHealth(part);
            if (ph != null) UpdatePartIndicator(part, ph.damageLevel);
        }

        #endregion

        #region Helpers

        private void BuildIndicatorDictionary()
        {
            AircraftPart[] parts = (AircraftPart[])Enum.GetValues(typeof(AircraftPart));
            for (int i = 0; i < parts.Length; i++)
            {
                Image img = (partIndicatorImages != null && i < partIndicatorImages.Length)
                    ? partIndicatorImages[i]
                    : null;

                _indicators[parts[i]]   = img;
                _baseColors[parts[i]]   = DamageConfig.ColorHealthy;
                _pulseEndTime[parts[i]] = 0f;
            }
        }

        private void RefreshOverallHealth()
        {
            if (_model == null) return;

            float health = _model.GetOverallHealth();

            if (overallHealthText != null)
                overallHealthText.text = $"{health:F0}%";

            if (overallHealthBar != null)
                overallHealthBar.fillAmount = Mathf.Clamp01(health / 100f);
        }

        private void RefreshRepairUI()
        {
            if (_repair == null) return;

            if (repairCooldownIndicator != null)
            {
                float elapsed  = Time.time - _repair.lastEmergencyRepairTime;
                float fill     = Mathf.Clamp01(elapsed / _repair.emergencyRepairCooldown);
                repairCooldownIndicator.fillAmount = fill;
            }

            if (repairChargesText != null)
                repairChargesText.text = _repair.remainingEmergencyCharges.ToString();
        }

        private void AnimatePulses()
        {
            float now = Time.time;
            foreach (AircraftPart part in _pulseEndTime.Keys.ToArray())
            {
                if (!_indicators.TryGetValue(part, out Image img) || img == null) continue;
                if (!_baseColors.TryGetValue(part, out Color baseCol))            continue;

                if (now < _pulseEndTime[part])
                {
                    float alpha = 0.5f + 0.5f * Mathf.Sin(now * pulseFrequency * Mathf.PI * 2f);
                    img.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
                }
                else
                {
                    img.color = baseCol;
                }
            }
        }

        private IEnumerator ShowPopupCoroutine(DamageData data)
        {
            if (damagePopupPanel != null) damagePopupPanel.SetActive(true);
            if (damagePopupText  != null)
            {
                damagePopupText.text = string.IsNullOrEmpty(data.description)
                    ? $"{data.affectedPart}: -{data.damageAmount:F1} HP ({data.source})"
                    : data.description;
            }

            yield return new WaitForSeconds(popupDuration);

            if (damagePopupPanel != null) damagePopupPanel.SetActive(false);
            _popupCoroutine = null;
        }

        #endregion
    }
}
