using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;

namespace SWEF.UI
{
    /// <summary>
    /// HUD element showing current flight speed in km/h and Mach number at high speeds.
    /// </summary>
    public class SpeedIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FlightController flight;
        [SerializeField] private Text speedText;
        [SerializeField] private Text machText;   // optional — if null, appended to speedText

        private const float MachSpeedMps    = 343f;
        private const float OrbitalSpeedMps = 7900f;

        private void Start()
        {
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();

            if (flight == null)
                Debug.LogWarning("[SWEF] SpeedIndicator — FlightController not found.");
        }

        private void Update()
        {
            if (flight == null || speedText == null) return;

            float speedMps = flight.CurrentSpeedMps;
            float speedKph = speedMps * 3.6f;

            string machInfo = string.Empty;

            if (speedMps >= OrbitalSpeedMps)
            {
                machInfo = "ORBITAL ⚡";
            }
            else if (speedMps >= MachSpeedMps)
            {
                machInfo = $"M {speedMps / MachSpeedMps:0.0}";
            }

            if (machText != null)
            {
                speedText.text = $"SPD {speedKph:0} km/h";
                machText.text  = machInfo;
            }
            else
            {
                // Append Mach info to speed text when no separate machText is assigned
                speedText.text = string.IsNullOrEmpty(machInfo)
                    ? $"SPD {speedKph:0} km/h"
                    : $"SPD {speedKph:0} km/h  {machInfo}";
            }
        }
    }
}
