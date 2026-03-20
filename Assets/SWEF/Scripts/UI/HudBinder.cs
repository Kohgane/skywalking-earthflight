using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;
using SWEF.Audio;

namespace SWEF.UI
{
    /// <summary>
    /// Binds HUD UI elements to FlightController and AltitudeController.
    /// Wire all serialized fields in the Inspector.
    /// </summary>
    public class HudBinder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FlightController flight;
        [SerializeField] private AltitudeController altitude;
        [SerializeField] private CameraController cameraController;

        [Header("UI Elements")]
        [SerializeField] private Slider throttleSlider;   // Range 0..1
        [SerializeField] private Slider altitudeSlider;   // Range 0..120000
        [SerializeField] private Toggle comfortToggle;
        [SerializeField] private Text altitudeText;       // Switch to TMP_Text if using TextMeshPro
        [SerializeField] private Button cameraCycleButton;

        [Header("Phase 28 — Spatial Audio (optional)")]
        [SerializeField] private Text machNumberText;
        [SerializeField] private SonicBoomController sonicBoomController;

        private void Awake()
        {
            if (flight == null)   flight   = FindFirstObjectByType<FlightController>();
            if (altitude == null) altitude = FindFirstObjectByType<AltitudeController>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (sonicBoomController == null) sonicBoomController = FindFirstObjectByType<SonicBoomController>();

            if (throttleSlider != null)
                throttleSlider.onValueChanged.AddListener(OnThrottle);
            if (altitudeSlider != null)
                altitudeSlider.onValueChanged.AddListener(OnAltitude);
            if (comfortToggle != null)
                comfortToggle.onValueChanged.AddListener(OnComfort);
            if (cameraCycleButton != null)
                cameraCycleButton.onClick.AddListener(OnCameraCycle);
        }

        private void Start()
        {
            // Sync UI to current values without triggering callbacks
            if (flight != null && throttleSlider != null)
                throttleSlider.SetValueWithoutNotify(flight.Throttle01);
            if (altitude != null && altitudeSlider != null)
                altitudeSlider.SetValueWithoutNotify(altitude.TargetAltitudeMeters);
            if (flight != null && comfortToggle != null)
                comfortToggle.SetIsOnWithoutNotify(flight.comfortMode);
        }

        private void Update()
        {
            if (altitudeText != null && altitude != null)
                altitudeText.text = $"ALT {altitude.CurrentAltitudeMeters:0} m";

            if (machNumberText != null && sonicBoomController != null)
                machNumberText.text = $"M {sonicBoomController.CurrentMach:F2}";
        }

        private void OnThrottle(float v)
        {
            if (flight != null) flight.SetThrottle(v);
        }

        private void OnAltitude(float v)
        {
            if (altitude != null) altitude.SetTargetAltitude(v);
        }

        private void OnComfort(bool on)
        {
            if (flight != null) flight.comfortMode = on;
        }

        private void OnCameraCycle()
        {
            cameraController?.CycleCamera();
        }
    }
}
