using System.Collections;
using UnityEngine;
using SWEF.Core;
using SWEF.Flight;

namespace SWEF.Util
{
    /// <summary>
    /// Static utility class providing helpers for automated and manual testing of SWEF systems.
    /// All members are <c>public static</c>; this is not a <see cref="MonoBehaviour"/>.
    /// </summary>
    public static class SWEFTestHelpers
    {
        // ── Session helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Creates and returns a new <see cref="SWEFSession"/> snapshot populated with
        /// the provided coordinates, without modifying the live global session.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees.</param>
        /// <param name="lon">Longitude in decimal degrees.</param>
        /// <param name="alt">Altitude in metres.</param>
        /// <returns>
        /// A plain data object containing the provided coordinates with <c>HasFix = true</c>.
        /// </returns>
        public static MockSession CreateMockSession(double lat, double lon, double alt)
        {
            return new MockSession { Lat = lat, Lon = lon, Alt = alt, HasFix = true };
        }

        // ── Altitude simulation ───────────────────────────────────────────────

        /// <summary>
        /// Coroutine that linearly lerps an <see cref="AltitudeController"/>'s target altitude
        /// to <paramref name="targetAlt"/> over <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="controller">The <see cref="AltitudeController"/> to drive.</param>
        /// <param name="targetAlt">Target altitude in metres.</param>
        /// <param name="duration">Total lerp duration in seconds.</param>
        public static IEnumerator SimulateAltitudeChange(AltitudeController controller,
                                                          float              targetAlt,
                                                          float              duration)
        {
            if (controller == null)
            {
                Debug.LogWarning("[SWEF] SWEFTestHelpers.SimulateAltitudeChange: controller is null.");
                yield break;
            }

            float startAlt  = controller.CurrentAltitudeMeters;
            float elapsed   = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                controller.SetTargetAltitude(Mathf.Lerp(startAlt, targetAlt, t));
                yield return null;
            }

            controller.SetTargetAltitude(targetAlt);
        }

        // ── Player rig factory ────────────────────────────────────────────────

        /// <summary>
        /// Creates a minimal <see cref="GameObject"/> hierarchy that mirrors the SWEF player rig
        /// and is suitable for unit tests. The root has <see cref="FlightController"/>,
        /// <see cref="TouchInputRouter"/>, and <see cref="AltitudeController"/> attached.
        /// </summary>
        /// <returns>The root <see cref="GameObject"/> of the test rig.</returns>
        public static GameObject CreateTestPlayerRig()
        {
            var root = new GameObject("TestPlayerRig");
            root.AddComponent<FlightController>();
            root.AddComponent<TouchInputRouter>();

            var altGo  = new GameObject("AltitudeNode");
            altGo.transform.SetParent(root.transform, false);
            var alt    = altGo.AddComponent<AltitudeController>();

            Debug.Log("[SWEF] SWEFTestHelpers: test player rig created.");
            return root;
        }

        // ── PlayerPrefs ───────────────────────────────────────────────────────

        /// <summary>
        /// Clears <b>all</b> PlayerPrefs keys and saves.
        /// Issues a warning because this is a destructive, irreversible operation.
        /// </summary>
        public static void ResetAllPlayerPrefs()
        {
            Debug.LogWarning("[SWEF] SWEFTestHelpers.ResetAllPlayerPrefs: deleting ALL PlayerPrefs — " +
                             "this is destructive and cannot be undone.");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        // ── Path helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the default SWEF save file path:
        /// <c>Application.persistentDataPath/swef_save.json</c>.
        /// </summary>
        public static string GetSaveFilePath()
            => System.IO.Path.Combine(Application.persistentDataPath, "swef_save.json");
    }

    // ── Supporting types ──────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight snapshot of session coordinates returned by
    /// <see cref="SWEFTestHelpers.CreateMockSession"/>.
    /// </summary>
    public class MockSession
    {
        /// <summary>Latitude in decimal degrees.</summary>
        public double Lat;
        /// <summary>Longitude in decimal degrees.</summary>
        public double Lon;
        /// <summary>Altitude in metres.</summary>
        public double Alt;
        /// <summary>Whether a GPS fix is available.</summary>
        public bool HasFix;
    }
}
