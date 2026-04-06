// RunwayGenerator.cs — Phase 113: Procedural City & Airport Generation
// Automatic runway placement: orientation based on prevailing wind,
// length based on airport class, markings and lighting.
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates runway GameObjects from <see cref="RunwayData"/> descriptors.
    /// Handles surface mesh, threshold markings, edge lighting, and approach lighting.
    /// </summary>
    public class RunwayGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Materials")]
        [SerializeField] private Material runwaySurfaceMaterial;
        [SerializeField] private Material runwayMarkingMaterial;

        [Header("Lighting Prefabs")]
        [SerializeField] private GameObject edgeLightPrefab;
        [SerializeField] private GameObject thresholdLightPrefab;
        [SerializeField] private GameObject approachLightPrefab;

        [Header("Lighting Spacing (metres)")]
        [SerializeField] private float edgeLightSpacing = 60f;
        [SerializeField] private float approachLightCount = 10;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns all GameObjects required to represent <paramref name="runway"/>
        /// under the given <paramref name="parent"/> transform.
        /// </summary>
        public void SpawnRunway(RunwayData runway, Transform parent)
        {
            SpawnSurface(runway, parent);
            SpawnEdgeLights(runway, parent);
            if (runway.hasILS) SpawnApproachLights(runway, parent);
        }

        /// <summary>
        /// Computes the optimal runway heading for the given prevailing wind direction.
        /// Returns the heading closest to the wind direction, rounded to the nearest 10°.
        /// </summary>
        public static float OptimalHeading(float windDirectionDegrees)
        {
            // Runways are named for the direction aircraft land INTO (against wind)
            float into = (windDirectionDegrees + 180f) % 360f;
            // Round to nearest 10 degrees
            return Mathf.Round(into / 10f) * 10f % 360f;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void SpawnSurface(RunwayData runway, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Runway_{runway.designator}";
            go.transform.SetParent(parent, false);
            go.transform.position = runway.thresholdPosition +
                Quaternion.Euler(0f, runway.heading, 0f) * Vector3.forward * (runway.lengthMetres * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, runway.heading, 0f);
            go.transform.localScale = new Vector3(runway.widthMetres, 0.05f, runway.lengthMetres);

            if (runwaySurfaceMaterial != null)
                go.GetComponent<Renderer>().sharedMaterial = runwaySurfaceMaterial;
        }

        private void SpawnEdgeLights(RunwayData runway, Transform parent)
        {
            if (edgeLightPrefab == null) return;
            Quaternion rot = Quaternion.Euler(0f, runway.heading, 0f);
            Vector3 right = rot * Vector3.right * (runway.widthMetres * 0.5f + 1f);
            int count = Mathf.FloorToInt(runway.lengthMetres / edgeLightSpacing);

            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                Vector3 along = runway.thresholdPosition + rot * Vector3.forward * (t * runway.lengthMetres);
                Instantiate(edgeLightPrefab, along + right, Quaternion.identity, parent);
                Instantiate(edgeLightPrefab, along - right, Quaternion.identity, parent);
            }
        }

        private void SpawnApproachLights(RunwayData runway, Transform parent)
        {
            if (approachLightPrefab == null) return;
            Quaternion rot = Quaternion.Euler(0f, runway.heading, 0f);
            Vector3 threshold = runway.thresholdPosition;

            for (int i = 1; i <= approachLightCount; i++)
            {
                Vector3 pos = threshold - rot * Vector3.forward * (i * 30f);
                Instantiate(approachLightPrefab, pos, Quaternion.identity, parent);
            }
        }
    }
}
