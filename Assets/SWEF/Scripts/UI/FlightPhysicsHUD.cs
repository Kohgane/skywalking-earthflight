using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Subscribes to <see cref="SWEF.Flight.FlightPhysicsIntegrator.OnPhysicsSnapshot"/>
    /// and displays advanced aerodynamic and orbital data on the HUD.
    /// All UI field references are optional — missing fields are silently ignored.
    /// </summary>
    public class FlightPhysicsHUD : MonoBehaviour
    {
        // ── Inspector wiring ─────────────────────────────────────────────────
        [Header("Source")]
        [SerializeField] private Flight.FlightPhysicsIntegrator integrator;
        [SerializeField] private Flight.StallWarningSystem stallWarningSystem;

        [Header("Text Fields (all optional)")]
        [SerializeField] private Text machNumberText;
        [SerializeField] private Text gForceText;
        [SerializeField] private Text orbitStateText;
        [SerializeField] private Text dynamicPressureText;
        [SerializeField] private Text aoaText;
        [SerializeField] private Text stallWarningText;
        [SerializeField] private Text airDensityText;

        [Header("Sliders / Fill Bars (all optional)")]
        [SerializeField] private Slider airDensityBar;

        [Header("Orbital Panel")]
        [Tooltip("Root GameObject for the orbital info panel — shown only above Kármán line.")]
        [SerializeField] private GameObject orbitalPanel;

        // ── Colour thresholds ────────────────────────────────────────────────
        private static readonly Color ColourNormal  = new Color(0.2f, 0.9f, 0.2f);  // green
        private static readonly Color ColourCaution = new Color(1.0f, 0.8f, 0.0f);  // yellow
        private static readonly Color ColourDanger  = new Color(1.0f, 0.2f, 0.2f);  // red

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (integrator == null)
                integrator = FindObjectOfType<Flight.FlightPhysicsIntegrator>();
            if (stallWarningSystem == null)
                stallWarningSystem = FindObjectOfType<Flight.StallWarningSystem>();
        }

        private void OnEnable()
        {
            if (integrator != null)
                integrator.OnPhysicsSnapshot += HandleSnapshot;
        }

        private void OnDisable()
        {
            if (integrator != null)
                integrator.OnPhysicsSnapshot -= HandleSnapshot;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Snapshot handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandleSnapshot(Flight.FlightPhysicsSnapshot snap)
        {
            var aero = snap.Aero;

            // ── Mach number ───────────────────────────────────────────────────
            if (machNumberText != null)
            {
                machNumberText.text = $"M {aero.MachNumber:F2}";
                machNumberText.color = aero.IsHypersonic  ? ColourDanger  :
                                       aero.IsSupersonic  ? ColourCaution :
                                                            ColourNormal;
            }

            // ── G-force ───────────────────────────────────────────────────────
            if (gForceText != null)
            {
                gForceText.text  = $"{snap.GForce:F1} G";
                gForceText.color = snap.GForce > 4f || snap.GForce < -1f ? ColourDanger :
                                   snap.GForce > 2.5f                    ? ColourCaution :
                                                                           ColourNormal;
            }

            // ── Orbit state ───────────────────────────────────────────────────
            if (orbitStateText != null)
                orbitStateText.text = snap.Orbit.ToString();

            // ── Dynamic pressure ──────────────────────────────────────────────
            if (dynamicPressureText != null)
            {
                dynamicPressureText.text  = $"q {aero.DynamicPressure:F0} Pa";
                dynamicPressureText.color = aero.DynamicPressure > 50000f ? ColourDanger  :
                                            aero.DynamicPressure > 30000f ? ColourCaution :
                                                                            ColourNormal;
            }

            // ── Angle of attack ───────────────────────────────────────────────
            if (aoaText != null)
            {
                aoaText.text  = $"AoA {aero.AngleOfAttack:F1}°";
                aoaText.color = aero.AngleOfAttack > 15f ? ColourDanger  :
                                aero.AngleOfAttack > 12f ? ColourCaution :
                                                          ColourNormal;
            }

            // ── Air density bar ───────────────────────────────────────────────
            if (airDensityBar != null)
                airDensityBar.value = Mathf.Clamp01(aero.AirDensity / 1.225f);

            if (airDensityText != null)
                airDensityText.text = $"ρ {aero.AirDensity:F4} kg/m³";

            // ── Stall warning — delegate to StallWarningSystem for consistent thresholds ──
            if (stallWarningText != null)
            {
                Flight.StallWarningState warnState = stallWarningSystem != null
                    ? stallWarningSystem.CurrentState
                    : Flight.StallWarningState.None;

                bool isStalling      = warnState == Flight.StallWarningState.Stalling;
                bool isStallImminent = warnState == Flight.StallWarningState.StallImminent;

                stallWarningText.gameObject.SetActive(isStalling || isStallImminent);
                if (isStalling)
                    stallWarningText.text = "STALL";
                else if (isStallImminent)
                    stallWarningText.text = "STALL IMMINENT";
            }

            // ── Orbital panel visibility ──────────────────────────────────────
            if (orbitalPanel != null)
                orbitalPanel.SetActive(!aero.IsInAtmosphere);
        }
    }
}
