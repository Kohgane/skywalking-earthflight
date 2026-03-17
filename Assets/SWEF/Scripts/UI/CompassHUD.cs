using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;

namespace SWEF.UI
{
    /// <summary>
    /// HUD compass showing heading direction (N/S/E/W) based on PlayerRig's Y rotation.
    /// </summary>
    public class CompassHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerRig;
        [SerializeField] private Text headingText;
        [SerializeField] private RectTransform compassNeedle;   // optional

        private static readonly string[] Cardinals =
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "NW"
        };

        private void Start()
        {
            if (playerRig == null)
            {
                var fc = FindFirstObjectByType<FlightController>();
                if (fc != null)
                    playerRig = fc.transform;
            }

            if (playerRig == null)
                Debug.LogWarning("[SWEF] CompassHUD — playerRig not found.");
        }

        private void Update()
        {
            if (playerRig == null || headingText == null) return;

            float heading = playerRig.eulerAngles.y;
            // Normalize to 0–360
            heading = (heading % 360f + 360f) % 360f;

            string cardinal = GetCardinal(heading);
            headingText.text = $"HDG {heading:000}° {cardinal}";

            if (compassNeedle != null)
                compassNeedle.localEulerAngles = new Vector3(0f, 0f, -heading);
        }

        private static string GetCardinal(float heading)
        {
            // Each of the 8 sectors spans 45°; offset by 22.5° so North is centred at 0°
            int index = Mathf.RoundToInt(heading / 45f) % 8;
            return Cardinals[index];
        }
    }
}
