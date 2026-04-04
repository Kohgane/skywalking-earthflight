// FlightRouteRenderer.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using UnityEngine;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Renders a great-circle arc between a flight's departure and arrival airports
    /// using a <see cref="LineRenderer"/> with dashed-line shader support.
    ///
    /// <para>Call <see cref="ShowRoute"/> with a populated <see cref="FlightRoute"/>
    /// to display the route.  The solid portion represents the traveled section;
    /// the dashed portion the predicted remaining route.</para>
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class FlightRouteRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private LiveFlightConfig config;

        [Header("Rendering")]
        [SerializeField] private int   greatCircleSegments   = 64;
        [SerializeField] private float lineWidth             = 500f;
        [SerializeField] private float dashLength            = 2000f;
        [SerializeField] private float gapLength             = 1000f;
        [SerializeField] private Color traveledColor         = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color predictedColor        = new Color(1f,   0.8f, 0.2f, 0.7f);

        // ── State ─────────────────────────────────────────────────────────────────
        private LineRenderer _lineRenderer;
        private bool         _routeVisible;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _lineRenderer           = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth    = lineWidth;
            _lineRenderer.endWidth      = lineWidth;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds and displays the route arc.  If <paramref name="route"/> has
        /// pre-computed <see cref="FlightRoute.waypoints"/> they are used directly;
        /// otherwise a great-circle path is computed from the departure / arrival
        /// airports' known coordinates.
        /// </summary>
        public void ShowRoute(FlightRoute route)
        {
            Vector3[] points = (route.waypoints != null && route.waypoints.Length >= 2)
                ? route.waypoints
                : ComputeGreatCirclePath(route, greatCircleSegments);

            if (points == null || points.Length < 2)
            {
                HideRoute();
                return;
            }

            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
            _lineRenderer.startColor = traveledColor;
            _lineRenderer.endColor   = predictedColor;
            _lineRenderer.enabled    = true;
            _routeVisible            = true;
        }

        /// <summary>Hides the route arc without destroying the component.</summary>
        public void HideRoute()
        {
            _lineRenderer.enabled = false;
            _routeVisible         = false;
        }

        /// <summary>Toggles all route line renderers on this component.</summary>
        public void ToggleRoutes(bool show)
        {
            if (show && _lineRenderer.positionCount > 0)
                _lineRenderer.enabled = true;
            else
                _lineRenderer.enabled = false;

            _routeVisible = _lineRenderer.enabled;
        }

        /// <summary><c>true</c> while the route arc is being displayed.</summary>
        public bool IsVisible => _routeVisible;

        // ── Great-circle calculation ──────────────────────────────────────────────

        /// <summary>
        /// Computes <paramref name="segments"/>+1 evenly-spaced points along the
        /// great-circle path between the departure and arrival airports.
        ///
        /// <para>Airport positions are approximated from their ICAO codes using a
        /// minimal built-in lookup; a real project would query a navigation database.</para>
        /// </summary>
        public static Vector3[] ComputeGreatCirclePath(FlightRoute route, int segments)
        {
            // Resolve approximate lat/lon for the two airports.
            if (!TryGetAirportLatLon(route.departureICAO, out double depLat, out double depLon) ||
                !TryGetAirportLatLon(route.arrivalICAO,   out double arrLat, out double arrLon))
            {
                return null;
            }

            return SampleGreatCircle(depLat, depLon, arrLat, arrLon, segments, 10000f);
        }

        /// <summary>
        /// Returns <paramref name="segments"/>+1 world-space points along the
        /// great-circle arc from (lat1, lon1) to (lat2, lon2).
        /// <paramref name="arcAltitude"/> is the added altitude at the mid-point
        /// (metres) to give the line visual depth.
        /// </summary>
        public static Vector3[] SampleGreatCircle(
            double lat1, double lon1,
            double lat2, double lon2,
            int segments, float arcAltitude)
        {
            // Convert to radians
            double φ1 = lat1 * Mathf.Deg2Rad;
            double λ1 = lon1 * Mathf.Deg2Rad;
            double φ2 = lat2 * Mathf.Deg2Rad;
            double λ2 = lon2 * Mathf.Deg2Rad;

            // Haversine angular distance
            double Δφ = φ2 - φ1;
            double Δλ = λ2 - λ1;
            double a  = System.Math.Sin(Δφ / 2) * System.Math.Sin(Δφ / 2)
                      + System.Math.Cos(φ1) * System.Math.Cos(φ2)
                      * System.Math.Sin(Δλ / 2) * System.Math.Sin(Δλ / 2);
            double d  = 2.0 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));

            var pts = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                double f   = (double)i / segments;
                double sinD = System.Math.Sin(d);

                double A, B;
                if (sinD < 1e-10)
                {
                    A = 1.0 - f;
                    B = f;
                }
                else
                {
                    A = System.Math.Sin((1.0 - f) * d) / sinD;
                    B = System.Math.Sin(f * d) / sinD;
                }

                double x = A * System.Math.Cos(φ1) * System.Math.Cos(λ1)
                         + B * System.Math.Cos(φ2) * System.Math.Cos(λ2);
                double y = A * System.Math.Cos(φ1) * System.Math.Sin(λ1)
                         + B * System.Math.Cos(φ2) * System.Math.Sin(λ2);
                double z = A * System.Math.Sin(φ1) + B * System.Math.Sin(φ2);

                double latI = System.Math.Atan2(z, System.Math.Sqrt(x * x + y * y));
                double lonI = System.Math.Atan2(y, x);

                // Arc altitude: sinusoidal bump highest at mid-point
                float altBump = arcAltitude * (float)System.Math.Sin(f * System.Math.PI);

                pts[i] = LatLonToWorld(latI * Mathf.Rad2Deg, lonI * Mathf.Rad2Deg, altBump);
            }

            return pts;
        }

        // ── Coordinate helpers ────────────────────────────────────────────────────

        private static Vector3 LatLonToWorld(double lat, double lon, float alt)
        {
            const double R = 6_371_000.0;
            float x = (float)(lon * Mathf.Deg2Rad * R);
            float z = (float)(lat * Mathf.Deg2Rad * R);
            return new Vector3(x, alt, z);
        }

        /// <summary>
        /// Very small hard-coded lookup for a handful of major hub airports so that
        /// mock routes can be drawn without a full navigation database.
        /// </summary>
        private static bool TryGetAirportLatLon(string icao, out double lat, out double lon)
        {
            switch ((icao ?? "").ToUpperInvariant())
            {
                case "KLAX": lat =  33.9425; lon = -118.4081; return true;
                case "KJFK": lat =  40.6413; lon =  -73.7781; return true;
                case "EGLL": lat =  51.4700; lon =   -0.4543; return true;
                case "EDDF": lat =  50.0333; lon =    8.5706; return true;
                case "RJTT": lat =  35.5494; lon =  139.7798; return true;
                case "YSSY": lat = -33.9399; lon =  151.1753; return true;
                case "ZBAA": lat =  40.0799; lon =  116.6031; return true;
                case "OMDB": lat =  25.2532; lon =   55.3657; return true;
                default:
                    lat = 0; lon = 0;
                    return false;
            }
        }
    }
}
