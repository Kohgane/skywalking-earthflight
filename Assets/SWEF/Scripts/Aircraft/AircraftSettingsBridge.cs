using System;
using UnityEngine;
using SWEF.Settings;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Bridges aircraft-specific visual settings with the main
    /// <see cref="SettingsManager"/>.
    ///
    /// PlayerPrefs keys:
    /// <list type="bullet">
    ///   <item><c>SWEF_Aircraft_TrailEnabled</c> — master toggle (default true)</item>
    ///   <item><c>SWEF_Aircraft_ParticleQuality</c> — 0=Off, 1=Low, 2=Medium, 3=High (default 2)</item>
    ///   <item><c>SWEF_Aircraft_ShowOtherPlayerSkins</c> — render remote customisations (default true)</item>
    ///   <item><c>SWEF_Aircraft_AuraEnabled</c> — aura effect toggle (default true)</item>
    /// </list>
    /// </summary>
    public class AircraftSettingsBridge : MonoBehaviour
    {
        // ── Keys ─────────────────────────────────────────────────────────────────

        private const string KeyTrailEnabled          = "SWEF_Aircraft_TrailEnabled";
        private const string KeyParticleQuality       = "SWEF_Aircraft_ParticleQuality";
        private const string KeyShowOtherPlayerSkins  = "SWEF_Aircraft_ShowOtherPlayerSkins";
        private const string KeyAuraEnabled           = "SWEF_Aircraft_AuraEnabled";

        // ── Defaults ─────────────────────────────────────────────────────────────

        private const bool DefaultTrailEnabled         = true;
        private const int  DefaultParticleQuality      = 2;
        private const bool DefaultShowOtherPlayerSkins = true;
        private const bool DefaultAuraEnabled          = true;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised whenever any aircraft setting changes.</summary>
        public event Action OnAircraftSettingsChanged;

        // ── Internal state ────────────────────────────────────────────────────────

        private SettingsManager _settingsManager;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _settingsManager = FindObjectOfType<SettingsManager>();
            if (_settingsManager != null)
                _settingsManager.OnSettingsChanged += HandleGlobalSettingsChanged;
            else
                Debug.LogWarning("[AircraftSettingsBridge] SettingsManager not found in scene.");
        }

        private void OnDestroy()
        {
            if (_settingsManager != null)
                _settingsManager.OnSettingsChanged -= HandleGlobalSettingsChanged;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns whether the contrail trail effect is enabled.</summary>
        public bool GetTrailEnabled() =>
            PlayerPrefs.GetInt(KeyTrailEnabled, DefaultTrailEnabled ? 1 : 0) != 0;

        /// <summary>
        /// Enables or disables the contrail trail effect and persists the setting.
        /// </summary>
        public void SetTrailEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KeyTrailEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnAircraftSettingsChanged?.Invoke();
        }

        /// <summary>Returns the current particle quality level (0=Off … 3=High).</summary>
        public int GetParticleQuality() =>
            PlayerPrefs.GetInt(KeyParticleQuality, DefaultParticleQuality);

        /// <summary>Sets the particle quality level (0–3) and persists the setting.</summary>
        public void SetParticleQuality(int quality)
        {
            PlayerPrefs.SetInt(KeyParticleQuality, Mathf.Clamp(quality, 0, 3));
            PlayerPrefs.Save();
            OnAircraftSettingsChanged?.Invoke();
        }

        /// <summary>Returns whether remote player skins should be rendered.</summary>
        public bool GetShowOtherPlayerSkins() =>
            PlayerPrefs.GetInt(KeyShowOtherPlayerSkins, DefaultShowOtherPlayerSkins ? 1 : 0) != 0;

        /// <summary>Toggles rendering of remote player skins and persists the setting.</summary>
        public void SetShowOtherPlayerSkins(bool show)
        {
            PlayerPrefs.SetInt(KeyShowOtherPlayerSkins, show ? 1 : 0);
            PlayerPrefs.Save();
            OnAircraftSettingsChanged?.Invoke();
        }

        /// <summary>Returns whether the aura effect is enabled.</summary>
        public bool GetAuraEnabled() =>
            PlayerPrefs.GetInt(KeyAuraEnabled, DefaultAuraEnabled ? 1 : 0) != 0;

        /// <summary>Enables or disables the aura effect and persists the setting.</summary>
        public void SetAuraEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KeyAuraEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnAircraftSettingsChanged?.Invoke();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleGlobalSettingsChanged()
        {
            // Propagate any aircraft-relevant global changes.
            OnAircraftSettingsChanged?.Invoke();
        }
    }
}
