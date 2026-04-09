using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;
using SWEF.CloudRendering;

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
        [SerializeField] private MonoBehaviour sonicBoomController;

        [Header("Phase 29 — Cloud Rendering (optional)")]
        [SerializeField] private GameObject cloudStatusIndicator;
        [SerializeField] private Text       cloudLatencyBadge;
        [SerializeField] private Image      cloudStatusDot;

        private CloudRenderingManager _cloudManager;
        private StreamingClient       _streamingClient;

        private void Awake()
        {
            if (flight == null)   flight   = FindFirstObjectByType<FlightController>();
            if (altitude == null) altitude = FindFirstObjectByType<AltitudeController>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (sonicBoomController == null)
            {
                var t = System.Type.GetType("SWEF.Audio.SonicBoomController, SWEF.Audio");
                if (t != null) sonicBoomController = FindFirstObjectByType(t) as MonoBehaviour;
            }
            if (sonicBoomController != null && machNumberText != null &&
                sonicBoomController.GetType().GetProperty("CurrentMach") == null)
                Debug.LogWarning("[SWEF] HudBinder: SonicBoomController does not have a 'CurrentMach' property — mach number display disabled.");
            _cloudManager    = FindFirstObjectByType<CloudRenderingManager>();
            _streamingClient = FindFirstObjectByType<StreamingClient>();

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
            {
                var mach = sonicBoomController.GetType().GetProperty("CurrentMach")?.GetValue(sonicBoomController);
                if (mach != null) machNumberText.text = $"M {(float)mach:F2}";
            }

            // Phase 29 — cloud streaming status
            bool isCloud = _cloudManager != null && _cloudManager.IsCloudMode;
            cloudStatusIndicator?.SetActive(isCloud);
            if (isCloud)
            {
                if (cloudLatencyBadge != null && _streamingClient != null)
                    cloudLatencyBadge.text = $"{_streamingClient.LatencyMs:F0} ms";

                if (cloudStatusDot != null)
                {
                    cloudStatusDot.color = _cloudManager.CurrentConnectionStatus switch
                    {
                        CloudRenderingManager.ConnectionStatus.Streaming  => new Color(0.18f, 0.80f, 0.44f),
                        CloudRenderingManager.ConnectionStatus.Connected  => new Color(0.95f, 0.77f, 0.06f),
                        CloudRenderingManager.ConnectionStatus.Connecting => new Color(0.95f, 0.77f, 0.06f),
                        _                                                  => new Color(0.91f, 0.30f, 0.24f),
                    };
                }
            }
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
