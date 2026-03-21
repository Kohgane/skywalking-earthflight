using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Minimap
{
    /// <summary>
    /// UI panel that exposes minimap and radar configuration controls to the player.
    /// All settings are persisted via <c>PlayerPrefs</c> with the <c>SWEF_Minimap_</c> prefix
    /// and applied immediately when any control changes.
    /// Subscribe to <see cref="OnSettingsChanged"/> to react to changes in other components.
    /// </summary>
    public class MinimapSettingsUI : MonoBehaviour
    {
        // ── PlayerPrefs keys ───────────────────────────────────────────────────────
        private const string PrefVisible         = "SWEF_Minimap_Visible";
        private const string PrefShape           = "SWEF_Minimap_Shape";
        private const string PrefMode            = "SWEF_Minimap_Mode";       // 0=minimap, 1=radar
        private const string PrefZoom            = "SWEF_Minimap_Zoom";
        private const string PrefOpacity         = "SWEF_Minimap_Opacity";
        private const string PrefIconSize        = "SWEF_Minimap_IconSize";
        private const string PrefShowWeather     = "SWEF_Minimap_ShowWeather";
        private const string PrefShowPOI         = "SWEF_Minimap_ShowPOI";
        private const string PrefShowEvents      = "SWEF_Minimap_ShowEvents";
        private const string PrefShowOtherPlayers= "SWEF_Minimap_ShowOtherPlayers";
        private const string PrefShowFormation   = "SWEF_Minimap_ShowFormation";

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever any setting changes. Listeners should re-read all settings.</summary>
        public event Action OnSettingsChanged;

        // ── Inspector — UI references ──────────────────────────────────────────────
        [Header("Panel")]
        [Tooltip("Root GameObject of the settings panel (toggled by OpenPanel/ClosePanel).")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Controls")]
        [SerializeField] private Toggle     toggleVisible;
        [SerializeField] private Toggle     toggleShapeCircular;
        [SerializeField] private Toggle     toggleShapeSquare;
        [SerializeField] private Toggle     toggleModeMap;
        [SerializeField] private Toggle     toggleModeRadar;
        [SerializeField] private Slider     sliderZoom;
        [SerializeField] private Slider     sliderOpacity;
        [SerializeField] private Slider     sliderIconSize;
        [SerializeField] private Toggle     toggleShowWeather;
        [SerializeField] private Toggle     toggleShowPOI;
        [SerializeField] private Toggle     toggleShowEvents;
        [SerializeField] private Toggle     toggleShowOtherPlayers;
        [SerializeField] private Toggle     toggleShowFormation;

        [Header("Target Components")]
        [Tooltip("MinimapRenderer to update when settings change.")]
        [SerializeField] private MinimapRenderer minimapRenderer;

        [Tooltip("RadarOverlay to update when settings change.")]
        [SerializeField] private RadarOverlay radarOverlay;

        // ── Cached settings ────────────────────────────────────────────────────────
        private bool         _visible;
        private MinimapShape _shape;
        private bool         _radarMode;
        private float        _zoom;
        private float        _opacity;
        private float        _iconSize;
        private bool         _showWeather;
        private bool         _showPOI;
        private bool         _showEvents;
        private bool         _showOtherPlayers;
        private bool         _showFormation;

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void Awake()
        {
            LoadSettings();
        }

        private void Start()
        {
            BindControls();
            RefreshControlValues();
            ApplySettings();
        }

        // ── Settings persistence ───────────────────────────────────────────────────
        private void LoadSettings()
        {
            _visible          = PlayerPrefs.GetInt(PrefVisible,          1)    == 1;
            _shape            = (MinimapShape)PlayerPrefs.GetInt(PrefShape, 0);
            _radarMode        = PlayerPrefs.GetInt(PrefMode,             0)    == 1;
            _zoom             = PlayerPrefs.GetFloat(PrefZoom,           1000f);
            _opacity          = PlayerPrefs.GetFloat(PrefOpacity,        1f);
            _iconSize         = PlayerPrefs.GetFloat(PrefIconSize,       1f);
            _showWeather      = PlayerPrefs.GetInt(PrefShowWeather,      1)    == 1;
            _showPOI          = PlayerPrefs.GetInt(PrefShowPOI,          1)    == 1;
            _showEvents       = PlayerPrefs.GetInt(PrefShowEvents,       1)    == 1;
            _showOtherPlayers = PlayerPrefs.GetInt(PrefShowOtherPlayers, 1)    == 1;
            _showFormation    = PlayerPrefs.GetInt(PrefShowFormation,    1)    == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(PrefVisible,          _visible          ? 1 : 0);
            PlayerPrefs.SetInt(PrefShape,            (int)_shape);
            PlayerPrefs.SetInt(PrefMode,             _radarMode        ? 1 : 0);
            PlayerPrefs.SetFloat(PrefZoom,           _zoom);
            PlayerPrefs.SetFloat(PrefOpacity,        _opacity);
            PlayerPrefs.SetFloat(PrefIconSize,       _iconSize);
            PlayerPrefs.SetInt(PrefShowWeather,      _showWeather      ? 1 : 0);
            PlayerPrefs.SetInt(PrefShowPOI,          _showPOI          ? 1 : 0);
            PlayerPrefs.SetInt(PrefShowEvents,       _showEvents       ? 1 : 0);
            PlayerPrefs.SetInt(PrefShowOtherPlayers, _showOtherPlayers ? 1 : 0);
            PlayerPrefs.SetInt(PrefShowFormation,    _showFormation    ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Control binding ────────────────────────────────────────────────────────
        private void BindControls()
        {
            if (toggleVisible         != null) toggleVisible.onValueChanged.AddListener(v => { _visible = v; Commit(); });
            if (toggleShapeCircular   != null) toggleShapeCircular.onValueChanged.AddListener(v => { if (v) { _shape = MinimapShape.Circular; Commit(); } });
            if (toggleShapeSquare     != null) toggleShapeSquare.onValueChanged.AddListener(v => { if (v) { _shape = MinimapShape.Square; Commit(); } });
            if (toggleModeMap         != null) toggleModeMap.onValueChanged.AddListener(v => { if (v) { _radarMode = false; Commit(); } });
            if (toggleModeRadar       != null) toggleModeRadar.onValueChanged.AddListener(v => { if (v) { _radarMode = true; Commit(); } });

            if (sliderZoom     != null)
            {
                sliderZoom.minValue = 50f;
                sliderZoom.maxValue = 10000f;
                sliderZoom.onValueChanged.AddListener(v => { _zoom = v; Commit(); });
            }
            if (sliderOpacity  != null)
            {
                sliderOpacity.minValue = 0.3f;
                sliderOpacity.maxValue = 1f;
                sliderOpacity.onValueChanged.AddListener(v => { _opacity = v; Commit(); });
            }
            if (sliderIconSize != null)
            {
                sliderIconSize.minValue = 0.5f;
                sliderIconSize.maxValue = 2f;
                sliderIconSize.onValueChanged.AddListener(v => { _iconSize = v; Commit(); });
            }

            if (toggleShowWeather      != null) toggleShowWeather.onValueChanged.AddListener(v      => { _showWeather      = v; Commit(); });
            if (toggleShowPOI          != null) toggleShowPOI.onValueChanged.AddListener(v          => { _showPOI          = v; Commit(); });
            if (toggleShowEvents       != null) toggleShowEvents.onValueChanged.AddListener(v       => { _showEvents       = v; Commit(); });
            if (toggleShowOtherPlayers != null) toggleShowOtherPlayers.onValueChanged.AddListener(v => { _showOtherPlayers = v; Commit(); });
            if (toggleShowFormation    != null) toggleShowFormation.onValueChanged.AddListener(v    => { _showFormation    = v; Commit(); });
        }

        private void RefreshControlValues()
        {
            if (toggleVisible         != null) toggleVisible.isOn          = _visible;
            if (toggleShapeCircular   != null) toggleShapeCircular.isOn    = _shape == MinimapShape.Circular;
            if (toggleShapeSquare     != null) toggleShapeSquare.isOn      = _shape == MinimapShape.Square;
            if (toggleModeMap         != null) toggleModeMap.isOn          = !_radarMode;
            if (toggleModeRadar       != null) toggleModeRadar.isOn        = _radarMode;
            if (sliderZoom            != null) sliderZoom.value            = _zoom;
            if (sliderOpacity         != null) sliderOpacity.value         = _opacity;
            if (sliderIconSize        != null) sliderIconSize.value        = _iconSize;
            if (toggleShowWeather     != null) toggleShowWeather.isOn      = _showWeather;
            if (toggleShowPOI         != null) toggleShowPOI.isOn          = _showPOI;
            if (toggleShowEvents      != null) toggleShowEvents.isOn       = _showEvents;
            if (toggleShowOtherPlayers!= null) toggleShowOtherPlayers.isOn = _showOtherPlayers;
            if (toggleShowFormation   != null) toggleShowFormation.isOn    = _showFormation;
        }

        /// <summary>Saves settings, pushes them to MinimapRenderer/RadarOverlay and fires the event.</summary>
        private void Commit()
        {
            SaveSettings();
            ApplySettings();
            OnSettingsChanged?.Invoke();
        }

        private void ApplySettings()
        {
            // Apply to MinimapRenderer
            if (minimapRenderer != null)
            {
                minimapRenderer.gameObject.SetActive(_visible && !_radarMode);
                minimapRenderer.SetShape(_shape);
                minimapRenderer.SetZoom(_zoom);
                minimapRenderer.SetOpacity(_opacity);
                minimapRenderer.SetIconSizeMultiplier(_iconSize);
            }

            // Apply to RadarOverlay
            if (radarOverlay != null)
                radarOverlay.SetRadarMode(_visible && _radarMode);

            // Apply category filters to MinimapManager blips
            ApplyCategoryFilters();
        }

        private void ApplyCategoryFilters()
        {
            if (MinimapManager.Instance == null) return;

            var blips = MinimapManager.Instance.GetAllBlips();
            foreach (var blip in blips)
            {
                switch (blip.iconType)
                {
                    case MinimapIconType.WeatherZone:
                        blip.isActive = _showWeather;
                        break;
                    case MinimapIconType.PointOfInterest:
                        blip.isActive = _showPOI;
                        break;
                    case MinimapIconType.WorldEvent:
                        blip.isActive = _showEvents;
                        break;
                    case MinimapIconType.OtherPlayer:
                        blip.isActive = _showOtherPlayers;
                        break;
                    case MinimapIconType.FormationSlot:
                        blip.isActive = _showFormation;
                        break;
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Opens the settings panel.</summary>
        public void OpenPanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        /// <summary>Closes the settings panel.</summary>
        public void ClosePanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        /// <summary>Toggles minimap visibility on/off.</summary>
        public void ToggleVisible()
        {
            _visible = !_visible;
            if (toggleVisible != null) toggleVisible.isOn = _visible;
            Commit();
        }

        // ── Accessors for other components ─────────────────────────────────────────
        /// <summary>Whether the minimap is currently set to visible.</summary>
        public bool IsVisible => _visible;

        /// <summary>Current minimap shape setting.</summary>
        public MinimapShape Shape => _shape;

        /// <summary>Whether radar mode is active.</summary>
        public bool IsRadarMode => _radarMode;

        /// <summary>Current zoom range in world units.</summary>
        public float ZoomRange => _zoom;

        /// <summary>Whether weather zones are shown.</summary>
        public bool ShowWeatherZones => _showWeather;

        /// <summary>Whether points of interest are shown.</summary>
        public bool ShowPointsOfInterest => _showPOI;

        /// <summary>Whether world events are shown.</summary>
        public bool ShowWorldEvents => _showEvents;

        /// <summary>Whether other players are shown.</summary>
        public bool ShowOtherPlayers => _showOtherPlayers;

        /// <summary>Whether formation slots are shown.</summary>
        public bool ShowFormationSlots => _showFormation;
    }
}
