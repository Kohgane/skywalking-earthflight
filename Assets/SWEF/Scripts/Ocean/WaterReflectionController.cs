using System.Collections;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Manages water surface reflections.
    ///
    /// <para>Supports three strategies, selected by <see cref="OceanManager.GlobalReflectionMode"/>:
    /// <list type="bullet">
    ///   <item><see cref="ReflectionMode.PlanarSimple"/> / <see cref="ReflectionMode.PlanarHQ"/> —
    ///         a mirrored camera renders into a <see cref="RenderTexture"/> each frame (or on-demand).
    ///   </item>
    ///   <item><see cref="ReflectionMode.CubeMap"/> — a baked or real-time cubemap is used as
    ///         a lightweight fallback for low-end devices.</item>
    ///   <item><see cref="ReflectionMode.None"/> / <see cref="ReflectionMode.ScreenSpace"/> —
    ///         no render texture managed here; SSR parameters are uploaded to the material.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WaterReflectionController : MonoBehaviour
    {
        #region Constants

        private const int   MinTextureSize    = 64;
        private const int   MaxTextureSize    = 2048;
        private const float ClipPlaneOffset   = 0.07f;

        private static readonly int ShaderPropReflTex      = Shader.PropertyToID("_ReflectionTex");
        private static readonly int ShaderPropReflTint      = Shader.PropertyToID("_ReflectionTint");
        private static readonly int ShaderPropReflDistortion = Shader.PropertyToID("_ReflectionDistortion");

        #endregion

        #region Inspector

        [Header("Planar Reflection")]
        [Tooltip("Texture resolution (square) for the planar reflection render texture.")]
        [SerializeField, Range(MinTextureSize, MaxTextureSize)] private int reflectionTextureSize = 512;

        [Tooltip("Layer mask controlling which layers appear in the reflection.")]
        [SerializeField] private LayerMask reflectionLayerMask = ~0;

        [Tooltip("Offset to move the reflection clip plane to prevent under-water rendering artefacts.")]
        [SerializeField] private float clipPlaneOffset = ClipPlaneOffset;

        [Header("Update Frequency")]
        [Tooltip("Reflection update mode:\n0 = every frame\n>0 = update every N frames")]
        [SerializeField, Min(0)] private int updateEveryNFrames = 0;

        [Header("Cubemap Fallback")]
        [Tooltip("Cubemap used when ReflectionMode == CubeMap.")]
        [SerializeField] private Cubemap fallbackCubemap;

        [Header("Time-of-Day Tint")]
        [Tooltip("Reflection tint colour curve over 24 h (X = normalised hour 0–1, Y unused; use gradient).")]
        [SerializeField] private Gradient timeOfDayTint;

        [Header("Distortion")]
        [Tooltip("Maximum UV distortion of the reflection based on wave height.")]
        [SerializeField, Range(0f, 0.1f)] private float maxReflectionDistortion = 0.02f;

        [Header("Material")]
        [Tooltip("Water material that receives the reflection texture.")]
        [SerializeField] private Material waterMaterial;

        [Header("References")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Private State

        private Camera         _mainCam;
        private Camera         _reflectionCam;
        private RenderTexture  _reflectionRT;
        private OceanManager   _mgr;
        private int            _frameCounter;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mgr = oceanManager != null ? oceanManager : FindFirstObjectByType<OceanManager>();
        }

        private void Start()
        {
            _mainCam = Camera.main;
            ApplyCurrentMode();

            if (_mgr != null)
                _mgr.OnQualityChanged += HandleQualityChanged;
        }

        private void OnDestroy()
        {
            if (_mgr != null)
                _mgr.OnQualityChanged -= HandleQualityChanged;

            DestroyPlanarAssets();
        }

        private void LateUpdate()
        {
            if (_mainCam == null) return;

            ReflectionMode mode = _mgr != null ? _mgr.GlobalReflectionMode : ReflectionMode.None;

            switch (mode)
            {
                case ReflectionMode.PlanarSimple:
                case ReflectionMode.PlanarHQ:
                    if (ShouldUpdateThisFrame())
                        RenderPlanarReflection();
                    break;

                case ReflectionMode.CubeMap:
                    UploadCubemapFallback();
                    break;
            }

            UploadDistortionAndTint();
        }

        #endregion

        #region Planar Reflection

        private void RenderPlanarReflection()
        {
            if (_reflectionCam == null || _reflectionRT == null)
                ApplyCurrentMode();

            if (_reflectionCam == null) return;

            // Mirror the main camera across the water plane.
            Vector3 camPos = _mainCam.transform.position;
            float   waterY = transform.position.y;
            float   dist   = camPos.y - waterY;
            _reflectionCam.transform.position = new Vector3(camPos.x, camPos.y - 2f * dist, camPos.z);

            Vector3 eulerAngles = _mainCam.transform.eulerAngles;
            _reflectionCam.transform.eulerAngles = new Vector3(-eulerAngles.x, eulerAngles.y, eulerAngles.z);

            // Oblique clip plane at water surface.
            _reflectionCam.projectionMatrix = _mainCam.projectionMatrix;

            Vector4 clipPlane = new Vector4(0f, 1f, 0f, -(waterY + clipPlaneOffset));
            _reflectionCam.projectionMatrix = _reflectionCam.CalculateObliqueMatrix(
                CameraSpacePlane(_reflectionCam, clipPlane));

            _reflectionCam.targetTexture = _reflectionRT;
            _reflectionCam.cullingMask   = reflectionLayerMask;
            _reflectionCam.Render();

            if (waterMaterial != null)
                waterMaterial.SetTexture(ShaderPropReflTex, _reflectionRT);
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector4 worldPlane)
        {
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3   n = m.MultiplyVector(new Vector3(worldPlane.x, worldPlane.y, worldPlane.z));
            float     d = worldPlane.w - Vector3.Dot(m.MultiplyPoint(Vector3.zero), new Vector3(worldPlane.x, worldPlane.y, worldPlane.z));
            return new Vector4(n.x, n.y, n.z, d);
        }

        #endregion

        #region Setup / Teardown

        private void ApplyCurrentMode()
        {
            DestroyPlanarAssets();

            ReflectionMode mode = _mgr != null ? _mgr.GlobalReflectionMode : ReflectionMode.None;

            if (mode == ReflectionMode.PlanarSimple || mode == ReflectionMode.PlanarHQ)
                CreatePlanarAssets();
        }

        private void CreatePlanarAssets()
        {
            int sz = Mathf.Clamp(reflectionTextureSize, MinTextureSize, MaxTextureSize);
            _reflectionRT = new RenderTexture(sz, sz, 16, RenderTextureFormat.Default)
            {
                name      = "OceanReflectionRT",
                hideFlags = HideFlags.DontSave
            };
            _reflectionRT.Create();

            var go = new GameObject("OceanReflectionCamera") { hideFlags = HideFlags.HideAndDontSave };
            _reflectionCam = go.AddComponent<Camera>();
            _reflectionCam.enabled = false;   // we call Render() manually
        }

        private void DestroyPlanarAssets()
        {
            if (_reflectionCam != null)
            {
                if (Application.isPlaying)
                    Destroy(_reflectionCam.gameObject);
                else
                    DestroyImmediate(_reflectionCam.gameObject);
                _reflectionCam = null;
            }

            if (_reflectionRT != null)
            {
                _reflectionRT.Release();
                if (Application.isPlaying)
                    Destroy(_reflectionRT);
                else
                    DestroyImmediate(_reflectionRT);
                _reflectionRT = null;
            }
        }

        #endregion

        #region Fallback & Auxiliary

        private void UploadCubemapFallback()
        {
            if (waterMaterial == null || fallbackCubemap == null) return;
            // Set cubemap on the material; the exact property name depends on the ocean shader.
            waterMaterial.SetTexture("_ReflectionCube", fallbackCubemap);
        }

        private void UploadDistortionAndTint()
        {
            if (waterMaterial == null || _mgr == null) return;

            // Wave-height driven distortion.
            var body = _mgr.GetNearestWaterBody(transform.position);
            float waveAmp = body?.oceanSettings?.waves?.amplitude ?? 0f;
            float distortion = Mathf.Clamp01(waveAmp / 10f) * maxReflectionDistortion;
            waterMaterial.SetFloat(ShaderPropReflDistortion, distortion);

            // Time-of-day tint.
            if (timeOfDayTint != null)
            {
                float tod = (Time.time / 86400f) % 1f; // placeholder; wire SWEF.TimeOfDay if available
                waterMaterial.SetColor(ShaderPropReflTint, timeOfDayTint.Evaluate(tod));
            }
        }

        private bool ShouldUpdateThisFrame()
        {
            if (updateEveryNFrames <= 0) return true;
            _frameCounter++;
            if (_frameCounter >= updateEveryNFrames)
            {
                _frameCounter = 0;
                return true;
            }
            return false;
        }

        private void HandleQualityChanged(WaveQuality wave, ReflectionMode reflection)
        {
            ApplyCurrentMode();
        }

        #endregion
    }
}
