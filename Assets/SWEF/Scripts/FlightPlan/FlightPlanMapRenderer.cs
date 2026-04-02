// FlightPlanMapRenderer.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System.Collections.Generic;
using UnityEngine;

#if SWEF_DISASTER_AVAILABLE
using SWEF.NaturalDisaster;
#endif

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — 3D world-space visualization of the active flight plan route.
    ///
    /// <para>Renders an altitude-coloured <see cref="LineRenderer"/> for each route
    /// segment (green = climb, white = cruise, cyan = descent), category-specific
    /// waypoint markers, procedure-type colouring (SID = magenta, STAR = cyan,
    /// Approach = green), active-leg highlighting, completed-leg dimming, wind-barb
    /// sprites, distance/time labels between waypoints, and hazard-zone overlay
    /// spheres from <see cref="DisasterManager"/>.</para>
    ///
    /// <para>Attach to any scene GameObject.  Assign <see cref="markerParent"/> to
    /// a container for pooled waypoint marker objects.</para>
    /// </summary>
    public class FlightPlanMapRenderer : MonoBehaviour
    {
        #region Inspector

        [Header("Line Renderer")]
        [Tooltip("LineRenderer used for the main route line.")]
        public LineRenderer routeLineRenderer;

        [Tooltip("Line width for the active leg.")]
        public float activeLineWidth  = 0.8f;

        [Tooltip("Line width for completed / inactive legs.")]
        public float dimmedLineWidth  = 0.3f;

        [Header("Segment Colors")]
        public Color colorClimb       = Color.green;
        public Color colorCruise      = Color.white;
        public Color colorDescent     = Color.cyan;
        public Color colorSID         = Color.magenta;
        public Color colorSTAR        = new Color(0f, 0.9f, 1f);
        public Color colorApproach    = Color.green;
        public Color colorCompleted   = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color colorActiveLeg   = Color.yellow;

        [Header("Waypoint Markers")]
        [Tooltip("Parent transform that holds spawned waypoint marker objects.")]
        public Transform markerParent;

        [Tooltip("Prefab used for all waypoint markers. Expected to have a MeshRenderer.")]
        public GameObject waypointMarkerPrefab;

        [Tooltip("Scale of the waypoint marker objects.")]
        public float markerScale = 1.5f;

        [Header("Labels")]
        [Tooltip("Prefab with a TextMeshPro world-space text component for leg labels.")]
        public GameObject legLabelPrefab;

        [Tooltip("Show distance / time labels between waypoints.")]
        public bool showLegLabels = true;

        [Header("Wind Barbs")]
        [Tooltip("Prefab for a wind barb sprite placed along the route.")]
        public GameObject windBarbPrefab;

        [Tooltip("Spacing in nautical miles between wind barbs.")]
        public float windBarbSpacingNm = 50f;

        [Header("Hazard Overlay")]
        [Tooltip("Prefab used to visualise disaster no-fly zones.")]
        public GameObject hazardSpherePrefab;

        #endregion

        #region Private State

        private readonly List<GameObject> _markerPool  = new List<GameObject>();
        private readonly List<GameObject> _labelPool   = new List<GameObject>();
        private readonly List<GameObject> _barbPool    = new List<GameObject>();
        private readonly List<GameObject> _hazardPool  = new List<GameObject>();

        private FlightPlanRoute _lastRenderedPlan;
        private int             _lastActiveIndex;

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            var mgr = FlightPlanManager.Instance;
            if (mgr?.activePlan == null
                || mgr.activePlan.status == FlightPlanStatus.Draft)
            {
                ClearAll();
                return;
            }

            // Only rebuild if plan or active waypoint changed
            if (mgr.activePlan != _lastRenderedPlan
                || mgr.activeWaypointIndex != _lastActiveIndex)
            {
                Render(mgr);
                _lastRenderedPlan = mgr.activePlan;
                _lastActiveIndex  = mgr.activeWaypointIndex;
            }
        }

        #endregion

        #region Render

        private void Render(FlightPlanManager mgr)
        {
            var plan = mgr.activePlan;
            var wps  = plan.waypoints;

            ClearAll();
            if (wps == null || wps.Count < 2) return;

            RenderRouteLines(plan, mgr.activeWaypointIndex);
            RenderWaypointMarkers(plan, mgr.activeWaypointIndex);
            if (showLegLabels) RenderLegLabels(plan, mgr.activeWaypointIndex);
            RenderWindBarbs(plan);
            RenderHazardOverlays();
        }

        #endregion

        #region Route Lines

        private void RenderRouteLines(FlightPlanRoute plan, int activeIdx)
        {
            var wps = plan.waypoints;
            if (routeLineRenderer == null) return;

            // Build positions list
            var positions = new Vector3[wps.Count];
            for (int i = 0; i < wps.Count; i++)
                positions[i] = WaypointToWorld(wps[i]);

            routeLineRenderer.positionCount = wps.Count;
            routeLineRenderer.SetPositions(positions);

            // Set per-segment colour via gradient
            if (wps.Count >= 2)
            {
                var gradient = new Gradient();
                var colKeys  = new GradientColorKey[wps.Count];
                var alphaKeys = new GradientAlphaKey[2]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };

                for (int i = 0; i < wps.Count; i++)
                {
                    float t   = wps.Count > 1 ? (float)i / (wps.Count - 1) : 0f;
                    Color col = GetSegmentColor(plan, i, activeIdx);
                    colKeys[i] = new GradientColorKey(col, t);
                }
                gradient.SetKeys(colKeys, alphaKeys);
                routeLineRenderer.colorGradient = gradient;
                routeLineRenderer.startWidth    = activeLineWidth;
                routeLineRenderer.endWidth      = activeLineWidth;
            }
        }

        private Color GetSegmentColor(FlightPlanRoute plan, int segIndex, int activeIdx)
        {
            if (segIndex < activeIdx) return colorCompleted;
            if (segIndex == activeIdx) return colorActiveLeg;

            var wps = plan.waypoints;
            if (segIndex >= wps.Count) return colorCruise;

            var wp = wps[segIndex];
            switch (wp.category)
            {
                case WaypointCategory.SID:      return colorSID;
                case WaypointCategory.STAR:     return colorSTAR;
                case WaypointCategory.Approach:
                case WaypointCategory.Missed:   return colorApproach;
            }

            // Altitude-based colouring
            if (segIndex > 0)
            {
                float prevAlt = wps[segIndex - 1].altitude;
                float thisAlt = wp.altitude;
                if (thisAlt > prevAlt + 500f) return colorClimb;
                if (thisAlt < prevAlt - 500f) return colorDescent;
            }
            return colorCruise;
        }

        #endregion

        #region Waypoint Markers

        private void RenderWaypointMarkers(FlightPlanRoute plan, int activeIdx)
        {
            if (waypointMarkerPrefab == null || markerParent == null) return;

            var wps = plan.waypoints;
            for (int i = 0; i < wps.Count; i++)
            {
                var go = GetPoolObject(_markerPool, waypointMarkerPrefab, markerParent);
                go.transform.position   = WaypointToWorld(wps[i]);
                go.transform.localScale = Vector3.one * markerScale;

                // Colour the marker
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var mat   = rend.material;
                    Color col = i < activeIdx  ? colorCompleted
                              : i == activeIdx ? colorActiveLeg
                              : GetSegmentColor(plan, i, activeIdx);
                    mat.color = col;
                }

                // Shape by category
                float yRot = wps[i].category switch
                {
                    WaypointCategory.VOR         => 45f,
                    WaypointCategory.Airport     =>  0f,
                    WaypointCategory.NDB         => 30f,
                    _                            =>  0f
                };
                go.transform.eulerAngles = new Vector3(0f, yRot, 0f);
            }
        }

        #endregion

        #region Leg Labels

        private void RenderLegLabels(FlightPlanRoute plan, int activeIdx)
        {
            if (legLabelPrefab == null || markerParent == null) return;
            var wps = plan.waypoints;

            for (int i = 0; i < wps.Count - 1; i++)
            {
                double distNm = NavigationDatabase.HaversineNm(
                    wps[i].latitude, wps[i].longitude,
                    wps[i + 1].latitude, wps[i + 1].longitude);

                float speedKts = plan.cruiseSpeed > 0f ? plan.cruiseSpeed
                               : FlightPlanConfig.DefaultCruiseSpeedKts;
                float etaMin   = (float)(distNm / speedKts) * 60f;

                var go = GetPoolObject(_labelPool, legLabelPrefab, markerParent);

                // Place label at midpoint of leg
                var mid = (WaypointToWorld(wps[i]) + WaypointToWorld(wps[i + 1])) * 0.5f;
                go.transform.position = mid + Vector3.up * 200f;

                var tmp = go.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmp != null)
                    tmp.text = $"{distNm:F0}nm  {etaMin:F0}min";
            }
        }

        #endregion

        #region Wind Barbs

        private void RenderWindBarbs(FlightPlanRoute plan)
        {
            if (windBarbPrefab == null) return;
            var wps = plan.waypoints;

            for (int i = 0; i < wps.Count - 1; i++)
            {
                double totalNm = NavigationDatabase.HaversineNm(
                    wps[i].latitude, wps[i].longitude,
                    wps[i + 1].latitude, wps[i + 1].longitude);

                int barbCount = Mathf.Max(1, Mathf.FloorToInt((float)totalNm / windBarbSpacingNm));
                for (int b = 1; b <= barbCount; b++)
                {
                    float t = (float)b / (barbCount + 1);
                    var   pos = Vector3.Lerp(WaypointToWorld(wps[i]), WaypointToWorld(wps[i + 1]), t);

                    var go = GetPoolObject(_barbPool, windBarbPrefab, markerParent != null
                                          ? markerParent : transform);
                    go.transform.position = pos + Vector3.up * 100f;

#if SWEF_WEATHER_AVAILABLE
                    if (Weather.WeatherManager.Instance != null)
                    {
                        float windDir = Weather.WeatherManager.Instance.GetAverageWindDirection();
                        go.transform.eulerAngles = new Vector3(0f, windDir, 0f);
                    }
#endif
                }
            }
        }

        #endregion

        #region Hazard Overlays

        private void RenderHazardOverlays()
        {
#if SWEF_DISASTER_AVAILABLE
            if (hazardSpherePrefab == null || DisasterManager.Instance == null) return;

            foreach (var disaster in DisasterManager.Instance.activeDisasters)
            {
                if (disaster == null) continue;
                var go = GetPoolObject(_hazardPool, hazardSpherePrefab,
                                       markerParent != null ? markerParent : transform);
                go.transform.position   = disaster.epicenter;

                float radius = disaster.GetCurrentRadius();
                go.transform.localScale = Vector3.one * radius * 2f;

                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    Color col = Color.red;
                    col.a = 0.2f;
                    rend.material.color = col;
                }
            }
#endif
        }

        #endregion

        #region Pool Helpers

        private static Vector3 WaypointToWorld(FlightPlanWaypoint wp)
        {
            return new Vector3(
                (float)(wp.longitude * 111320.0),
                wp.altitude * 0.3048f,
                (float)(wp.latitude  * 111320.0));
        }

        private static GameObject GetPoolObject(List<GameObject> pool,
                                                 GameObject prefab,
                                                 Transform parent)
        {
            // Find first inactive
            foreach (var obj in pool)
            {
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }
            var go = Instantiate(prefab, parent);
            pool.Add(go);
            return go;
        }

        private static void ReturnToPool(List<GameObject> pool)
        {
            foreach (var obj in pool)
                if (obj != null) obj.SetActive(false);
        }

        private void ClearAll()
        {
            ReturnToPool(_markerPool);
            ReturnToPool(_labelPool);
            ReturnToPool(_barbPool);
            ReturnToPool(_hazardPool);

            if (routeLineRenderer != null)
                routeLineRenderer.positionCount = 0;
        }

        #endregion
    }
}
