// LiveryPreviewRenderer.cs — Phase 115: Advanced Aircraft Livery Editor
// Real-time 3D preview: rotate aircraft, lighting presets, zoom to detail areas.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Controls a 3-D preview of the aircraft with the active livery applied.
    /// Supports manual and auto-rotation, lighting preset selection, and zoom.
    /// </summary>
    public class LiveryPreviewRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Preview Setup")]
        [SerializeField] private Transform aircraftPivot;
        [SerializeField] private Camera    previewCamera;
        [SerializeField] private LiveryEditorConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the lighting preset changes.</summary>
        public event Action<int> OnLightingPresetChanged;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current lighting preset index (0–5).</summary>
        public int CurrentLightingPreset { get; private set; }

        /// <summary>Whether the preview is currently auto-rotating.</summary>
        public bool AutoRotating { get; private set; }

        // ── Internal state ────────────────────────────────────────────────────────
        private float _yaw;
        private float _pitch;
        private float _zoom = 5f;
        private static readonly float[] ZoomLevels = { 10f, 5f, 2.5f, 1.0f };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            CurrentLightingPreset = config != null ? config.DefaultLightingPreset : 0;
        }

        private void Update()
        {
            if (!AutoRotating || aircraftPivot == null) return;
            float speed = config != null ? config.PreviewAutoRotateSpeed : 20f;
            _yaw += speed * Time.deltaTime;
            ApplyTransform();
        }

        // ── Rotation ──────────────────────────────────────────────────────────────

        /// <summary>Rotates the preview by the given delta angles (in degrees).</summary>
        public void Orbit(float deltaYaw, float deltaPitch)
        {
            _yaw   += deltaYaw;
            _pitch  = Mathf.Clamp(_pitch + deltaPitch, -80f, 80f);
            ApplyTransform();
        }

        /// <summary>Resets yaw and pitch to the default front view.</summary>
        public void ResetRotation()
        {
            _yaw   = 0f;
            _pitch = 10f;
            ApplyTransform();
        }

        /// <summary>Starts or stops automatic rotation.</summary>
        public void SetAutoRotate(bool enabled) => AutoRotating = enabled;

        // ── Zoom ──────────────────────────────────────────────────────────────────

        /// <summary>Steps the zoom level in or out.</summary>
        /// <param name="zoomIn"><c>true</c> to zoom in, <c>false</c> to zoom out.</param>
        public void StepZoom(bool zoomIn)
        {
            _zoom = zoomIn
                ? Mathf.Max(1f, _zoom * 0.75f)
                : Mathf.Min(20f, _zoom * 1.33f);
            if (previewCamera != null)
                previewCamera.transform.localPosition = new Vector3(0, 0, -_zoom);
        }

        // ── Lighting ──────────────────────────────────────────────────────────────

        /// <summary>Switches to the given lighting preset (0-indexed).</summary>
        public void SetLightingPreset(int presetIndex)
        {
            CurrentLightingPreset = Mathf.Clamp(presetIndex, 0, 5);
            OnLightingPresetChanged?.Invoke(CurrentLightingPreset);
        }

        // ── Apply livery texture ──────────────────────────────────────────────────

        /// <summary>
        /// Applies the given texture to the aircraft preview mesh's material.
        /// </summary>
        public void ApplyPreviewTexture(Texture2D liveryTexture)
        {
            if (aircraftPivot == null) return;
            var renderer = aircraftPivot.GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            renderer.material.mainTexture = liveryTexture;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyTransform()
        {
            if (aircraftPivot == null) return;
            aircraftPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
