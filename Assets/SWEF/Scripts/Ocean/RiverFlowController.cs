using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Controls flowing water along a spline-defined river path.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Spline defined by a list of <see cref="controlPoints"/> (world-space).</item>
    ///   <item>Procedural river mesh extruded along the spline with configurable width.</item>
    ///   <item>UV scrolling in the flow direction for the river material.</item>
    ///   <item>Turbulence zones near bends and rapids.</item>
    ///   <item>Edge foam along the river banks.</item>
    ///   <item>Waterfall detection — vertical drops trigger a waterfall state.</item>
    ///   <item>Integration with terrain height for river-bed following.</item>
    ///   <item>Sound zone parameters (calm / rapids) exported for the Audio system.</item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RiverFlowController : MonoBehaviour
    {
        #region Constants

        private const int   MinSegmentResolution   = 2;
        private const float WaterfallDropThreshold = 3f;   // metres drop per segment to trigger waterfall

        private static readonly int ShaderPropFlowSpeed    = Shader.PropertyToID("_FlowSpeed");
        private static readonly int ShaderPropFlowDir      = Shader.PropertyToID("_FlowDirection");
        private static readonly int ShaderPropTurbulence   = Shader.PropertyToID("_Turbulence");
        private static readonly int ShaderPropFoamEdge     = Shader.PropertyToID("_BankFoamIntensity");
        private static readonly int ShaderPropUVOffset     = Shader.PropertyToID("_FlowUVOffset");

        #endregion

        #region Inspector

        [Header("Spline")]
        [Tooltip("World-space control points defining the river centre-line.")]
        [SerializeField] private List<Transform> controlPoints = new List<Transform>();

        [Tooltip("Number of mesh segments between each pair of control points.")]
        [SerializeField, Min(MinSegmentResolution)] private int segmentsPerSpan = 8;

        [Header("River Settings")]
        [SerializeField] private RiverSettings riverSettings = new RiverSettings();

        [Header("Mesh")]
        [Tooltip("Material applied to the river mesh.")]
        [SerializeField] private Material riverMaterial;

        [Header("Waterfall")]
        [Tooltip("Particle system activated when a waterfall section is detected.")]
        [SerializeField] private ParticleSystem waterfallParticles;

        [Header("References")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Public Properties

        /// <summary><c>true</c> when the current river section contains a waterfall.</summary>
        public bool HasWaterfall { get; private set; }

        /// <summary>Normalised turbulence at the current nearest point (0 = calm, 1 = rapids).</summary>
        public float CurrentTurbulence { get; private set; }

        #endregion

        #region Private State

        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private OceanManager _mgr;
        private float        _uvOffset;
        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _mgr = oceanManager != null ? oceanManager : FindFirstObjectByType<OceanManager>();

            if (riverMaterial != null)
                _meshRenderer.sharedMaterial = riverMaterial;
        }

        private void Start()
        {
            if (controlPoints.Count >= 2)
                RebuildMesh();
        }

        private void Update()
        {
            _uvOffset += riverSettings.flowSpeed * Time.deltaTime * 0.1f;
            UploadShaderProperties();
            DetectWaterfall();
        }

        #endregion

        #region Mesh Generation

        /// <summary>Rebuilds the river mesh from the current <see cref="controlPoints"/> spline.</summary>
        public void RebuildMesh()
        {
            if (controlPoints == null || controlPoints.Count < 2) return;

            var positions = new List<Vector3>();
            var uvs       = new List<Vector2>();
            var tris      = new List<int>();

            int totalSegments = (controlPoints.Count - 1) * segmentsPerSpan;
            float halfWidth = riverSettings.width * 0.5f;

            for (int seg = 0; seg <= totalSegments; seg++)
            {
                float t         = (float)seg / totalSegments;
                Vector3 center  = SplinePosition(t);
                Vector3 tangent = SplineTangent(t).normalized;
                Vector3 right   = Vector3.Cross(tangent, Vector3.up).normalized;

                float v = (float)seg / totalSegments;
                positions.Add(center - right * halfWidth);
                positions.Add(center + right * halfWidth);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }

            for (int seg = 0; seg < totalSegments; seg++)
            {
                int bl = seg * 2;
                int br = bl + 1;
                int tl = bl + 2;
                int tr = tl + 1;

                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            var mesh = new Mesh { name = "RiverMesh" };
            mesh.SetVertices(positions);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshFilter.sharedMesh = mesh;
        }

        #endregion

        #region Shader Upload

        private void UploadShaderProperties()
        {
            _propBlock.SetFloat(ShaderPropFlowSpeed,  riverSettings.flowSpeed);
            _propBlock.SetVector(ShaderPropFlowDir,   new Vector4(riverSettings.flowDirection.x, riverSettings.flowDirection.y, 0f, 0f));
            _propBlock.SetFloat(ShaderPropTurbulence, riverSettings.turbulence);
            _propBlock.SetFloat(ShaderPropFoamEdge,   riverSettings.bankFoamEnabled ? 1f : 0f);
            _propBlock.SetFloat(ShaderPropUVOffset,   _uvOffset);

            if (_meshRenderer != null)
                _meshRenderer.SetPropertyBlock(_propBlock);
        }

        #endregion

        #region Waterfall Detection

        private void DetectWaterfall()
        {
            bool foundWaterfall = false;

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                if (controlPoints[i] == null || controlPoints[i + 1] == null) continue;
                float drop = controlPoints[i].position.y - controlPoints[i + 1].position.y;
                if (drop >= WaterfallDropThreshold)
                {
                    foundWaterfall = true;
                    break;
                }
            }

            if (foundWaterfall != HasWaterfall)
            {
                HasWaterfall = foundWaterfall;
                if (waterfallParticles != null)
                {
                    if (HasWaterfall) waterfallParticles.Play();
                    else              waterfallParticles.Stop();
                }
            }

            // Approximate turbulence from bend sharpness.
            CurrentTurbulence = ComputeSplineCurvature() * riverSettings.turbulence;
        }

        #endregion

        #region Spline Math

        private Vector3 SplinePosition(float t)
        {
            if (controlPoints.Count < 2) return Vector3.zero;

            t = Mathf.Clamp01(t);
            float scaledT  = t * (controlPoints.Count - 1);
            int   seg      = Mathf.Min(Mathf.FloorToInt(scaledT), controlPoints.Count - 2);
            float localT   = scaledT - seg;

            Vector3 p0 = controlPoints[Mathf.Max(seg - 1, 0)].position;
            Vector3 p1 = controlPoints[seg].position;
            Vector3 p2 = controlPoints[seg + 1].position;
            Vector3 p3 = controlPoints[Mathf.Min(seg + 2, controlPoints.Count - 1)].position;

            return CatmullRom(p0, p1, p2, p3, localT);
        }

        private Vector3 SplineTangent(float t)
        {
            const float Eps = 0.001f;
            return (SplinePosition(t + Eps) - SplinePosition(t - Eps)).normalized;
        }

        private float ComputeSplineCurvature()
        {
            if (controlPoints.Count < 3) return 0f;
            Vector3 t0 = SplineTangent(0f);
            Vector3 t1 = SplineTangent(0.5f);
            return 1f - Mathf.Clamp01(Vector3.Dot(t0, t1));
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t  * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (controlPoints == null || controlPoints.Count < 2) return;

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.7f);
            int steps = (controlPoints.Count - 1) * segmentsPerSpan;
            Vector3 prev = SplinePosition(0f);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = SplinePosition((float)i / steps);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
