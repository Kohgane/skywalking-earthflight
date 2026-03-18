using UnityEngine;
using SWEF.Flight;

namespace SWEF.Core
{
    /// <summary>
    /// Editor-only gizmo drawing for spatial debugging of the player rig.
    /// <list type="bullet">
    ///   <item>Draws a wire sphere at the player position, coloured by altitude.</item>
    ///   <item>Draws a line from the player to the georeference origin.</item>
    ///   <item>Shows an altitude label using <c>Handles.Label</c>.</item>
    /// </list>
    /// Colour key:
    /// <list type="table">
    ///   <listheader><term>Colour</term><description>Altitude range</description></listheader>
    ///   <item><term>Green</term><description>&lt; 2 km</description></item>
    ///   <item><term>Yellow</term><description>2 km – 20 km</description></item>
    ///   <item><term>Red</term><description>20 km – 80 km</description></item>
    ///   <item><term>Magenta</term><description>&gt; 80 km</description></item>
    /// </list>
    /// </summary>
    [ExecuteAlways]
    public class DebugGizmoDrawer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform          playerRig;
        [SerializeField] private AltitudeController altitudeController;
        [SerializeField] private Transform          georeferenceOrigin;

        [Header("Gizmo Settings")]
        [SerializeField] private float sphereRadius = 50f;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (playerRig == null) return;

            float altitudeM = altitudeController != null
                ? altitudeController.CurrentAltitudeMeters
                : playerRig.position.y;

            float altitudeKm = altitudeM / 1000f;

            Gizmos.color = AltitudeColour(altitudeKm);
            Gizmos.DrawWireSphere(playerRig.position, sphereRadius);

            // Line to georeference origin
            Vector3 origin = georeferenceOrigin != null ? georeferenceOrigin.position : Vector3.zero;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerRig.position, origin);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                playerRig.position + Vector3.up * (sphereRadius * 1.5f),
                $"Alt: {altitudeM:F0} m ({altitudeKm:F2} km)");
#endif
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Color AltitudeColour(float altKm)
        {
            if (altKm < 2f)   return Color.green;
            if (altKm < 20f)  return Color.yellow;
            if (altKm < 80f)  return Color.red;
            return Color.magenta;
        }
    }
}
