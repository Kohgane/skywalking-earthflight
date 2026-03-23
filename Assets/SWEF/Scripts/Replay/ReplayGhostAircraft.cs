using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Controls the transparent ghost-aircraft visual during replay
    /// playback.  Supports adjustable opacity, particle trail, multi-ghost
    /// colour coding, and optional control-surface animation mirroring.
    /// </summary>
    public class ReplayGhostAircraft : MonoBehaviour
    {
        #region Constants

        private const float DefaultOpacity       = 0.45f;
        private const float InterpolationSpeed   = 10f;
        private const string OpacityShaderParam  = "_BaseColor";   // URP Lit; fallback: _Color
        private const string ColorShaderParam    = "_BaseColor";

        #endregion

        #region Inspector

        [Header("Visuals")]
        [Tooltip("Base opacity of the ghost material [0, 1].")]
        [SerializeField, Range(0f, 1f)] private float opacity = DefaultOpacity;

        [Tooltip("Ghost tint colour (used for multi-ghost comparison).")]
        [SerializeField] private Color ghostColor = new Color(0.3f, 0.8f, 1f, DefaultOpacity);

        [Header("Trail")]
        [SerializeField] private ParticleSystem trailParticles;

        [Header("Control Surfaces (optional)")]
        [SerializeField] private Transform aileronLeft;
        [SerializeField] private Transform aileronRight;
        [SerializeField] private Transform elevator;
        [SerializeField] private Transform rudder;
        [SerializeField] private float controlSurfaceMaxAngle = 20f;

        [Header("Multi-Ghost")]
        [SerializeField] private bool showInMultiGhostMode = true;

        #endregion

        #region Events

        /// <summary>Fired when the ghost's visibility is toggled.</summary>
        public event Action<bool> OnVisibilityChanged;

        #endregion

        #region Public Properties

        /// <summary>The recording this ghost is replaying.</summary>
        public FlightRecording Recording { get; private set; }

        /// <summary>Whether the ghost mesh is currently visible.</summary>
        public bool IsVisible { get; private set; } = true;

        #endregion

        #region Private State

        private Renderer[]   _renderers;
        private List<Material> _ghostMaterials = new List<Material>();
        private bool         _initialised;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnDestroy()
        {
            // Clean up instanced materials.
            foreach (var mat in _ghostMaterials)
                if (mat != null) Destroy(mat);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures the ghost for the given <paramref name="recording"/>.
        /// Call once after the GameObject is instantiated.
        /// </summary>
        public void Initialise(FlightRecording recording)
        {
            Recording    = recording;
            _initialised = true;
            ApplyMaterials();
        }

        /// <summary>Shows or hides the ghost aircraft renderers.</summary>
        public void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            foreach (var r in _renderers)
                if (r != null) r.enabled = visible;
            if (trailParticles != null)
            {
                if (visible) trailParticles.Play();
                else trailParticles.Stop();
            }
            OnVisibilityChanged?.Invoke(visible);
        }

        /// <summary>Changes the ghost colour and opacity.</summary>
        public void SetColor(Color color)
        {
            ghostColor = color;
            ApplyMaterials();
        }

        /// <summary>Sets opacity independently of colour hue.</summary>
        public void SetOpacity(float alpha)
        {
            opacity            = Mathf.Clamp01(alpha);
            ghostColor.a       = opacity;
            ApplyMaterials();
        }

        /// <summary>
        /// Applies the interpolated control-surface rotations from the given frame.
        /// </summary>
        public void ApplyControlSurfaces(FlightFrame frame)
        {
            if (frame == null) return;

            float maxAng = controlSurfaceMaxAngle;
            ApplySurface(aileronLeft,   Vector3.forward,  -frame.rollInput  * maxAng);
            ApplySurface(aileronRight,  Vector3.forward,   frame.rollInput  * maxAng);
            ApplySurface(elevator,      Vector3.right,     frame.pitchInput * maxAng);
            ApplySurface(rudder,        Vector3.up,        frame.yawInput   * maxAng);
        }

        #endregion

        #region Private — Material Helpers

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        private void ApplyMaterials()
        {
            if (_renderers == null) CacheRenderers();
            _ghostMaterials.Clear();

            foreach (var r in _renderers)
            {
                if (r == null) continue;
                // Create instanced copies so we don't affect the original materials.
                var mats  = r.materials;
                var newMats = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) { newMats[i] = null; continue; }
                    var inst = Instantiate(mats[i]);
                    SetupGhostMaterial(inst);
                    newMats[i] = inst;
                    _ghostMaterials.Add(inst);
                }
                r.materials = newMats;
            }
        }

        private void SetupGhostMaterial(Material mat)
        {
            if (mat == null) return;

            // Enable alpha blending.
            mat.SetFloat("_Surface",    1f); // URP: 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend",      0f); // Alpha
            mat.SetFloat("_SrcBlend",   (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend",   (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",     0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            Color c = ghostColor;
            c.a = opacity;

            // Try URP _BaseColor first, fall back to legacy _Color.
            if (mat.HasProperty(OpacityShaderParam))
                mat.SetColor(OpacityShaderParam, c);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
        }

        private static void ApplySurface(Transform surface, Vector3 axis, float angle)
        {
            if (surface == null) return;
            Vector3 euler = surface.localEulerAngles;
            if (axis == Vector3.forward)  euler.z = angle;
            else if (axis == Vector3.right) euler.x = angle;
            else if (axis == Vector3.up)    euler.y = angle;
            surface.localEulerAngles = euler;
        }

        #endregion
    }
}
