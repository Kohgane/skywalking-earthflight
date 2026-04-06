// AirportNavaidPlacer.cs — Phase 113: Procedural City & Airport Generation
// Navigation aid placement: ILS, VOR, PAPI lights, approach lighting systems.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Places navigation aids (ILS, VOR, PAPI) at the correct positions
    /// relative to each runway in an <see cref="AirportLayout"/>.
    /// </summary>
    public class AirportNavaidPlacer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Navaid Prefabs")]
        [SerializeField] private GameObject vorPrefab;
        [SerializeField] private GameObject ilsLocalizerPrefab;
        [SerializeField] private GameObject ilsGlislopePrefab;
        [SerializeField] private GameObject papiPrefab;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<GameObject> _navaids = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Places all navaids for the given airport.</summary>
        public void PlaceNavaids(AirportLayout airport, Transform parent)
        {
            ClearNavaids();
            PlaceVOR(airport, parent);
            foreach (var runway in airport.runways)
            {
                if (runway.hasILS) PlaceILS(runway, parent);
                PlacePAPI(runway, parent);
            }
        }

        /// <summary>Removes all placed navaid objects.</summary>
        public void ClearNavaids()
        {
            foreach (var go in _navaids)
                if (go != null) Destroy(go);
            _navaids.Clear();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void PlaceVOR(AirportLayout airport, Transform parent)
        {
            // VOR is typically 1–2 km from the airport reference point
            Vector3 vorPos = airport.referencePoint + new Vector3(0f, 0f, -1500f);
            var go = SpawnNavaid(vorPrefab, vorPos, "VOR", parent);
            _navaids.Add(go);
        }

        private void PlaceILS(RunwayData runway, Transform parent)
        {
            Quaternion rot = Quaternion.Euler(0f, runway.heading, 0f);

            // Localiser — beyond the far end of the runway
            Vector3 locPos = runway.thresholdPosition + rot * Vector3.forward * (runway.lengthMetres + 300f);
            _navaids.Add(SpawnNavaid(ilsLocalizerPrefab, locPos, $"LOC_{runway.designator}", parent));

            // Glideslope — 300 m from threshold, offset to the side
            Vector3 gsPos = runway.thresholdPosition + rot * Vector3.forward * 300f
                            + rot * Vector3.right * 120f;
            _navaids.Add(SpawnNavaid(ilsGlislopePrefab, gsPos, $"GS_{runway.designator}", parent));
        }

        private void PlacePAPI(RunwayData runway, Transform parent)
        {
            Quaternion rot = Quaternion.Euler(0f, runway.heading, 0f);
            // PAPI is 300 m from threshold, on the left side looking down the runway
            Vector3 papiPos = runway.thresholdPosition
                + rot * Vector3.forward * 300f
                - rot * Vector3.right * (runway.widthMetres * 0.5f + 10f);
            _navaids.Add(SpawnNavaid(papiPrefab, papiPos, $"PAPI_{runway.designator}", parent));
        }

        private GameObject SpawnNavaid(GameObject prefab, Vector3 pos, string name, Transform parent)
        {
            if (prefab != null)
                return Instantiate(prefab, pos, Quaternion.identity, parent);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 3f;
            return go;
        }
    }
}
