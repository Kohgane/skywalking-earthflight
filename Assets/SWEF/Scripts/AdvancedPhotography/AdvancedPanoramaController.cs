// AdvancedPanoramaController.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections;
using UnityEngine;

#if SWEF_PHOTOMODE_AVAILABLE
using SWEF.PhotoMode;
#endif

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — MonoBehaviour that captures multi-face or multi-strip panoramas and
    /// exports them as equirectangular or stereographic (Little Planet) textures.
    ///
    /// <para>Supports Horizontal, Vertical, Full360, and LittlePlanet panorama types.
    /// Progress is reported via events; the result texture is saved to the gallery when
    /// <c>SWEF_PHOTOMODE_AVAILABLE</c> is defined.</para>
    /// </summary>
    public sealed class AdvancedPanoramaController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when a panorama capture sequence begins.</summary>
        public event Action OnPanoramaCaptureStarted;

        /// <summary>Fired periodically with normalised progress in [0, 1].</summary>
        public event Action<float> OnPanoramaCaptureProgress;

        /// <summary>Fired when capture and stitching is complete, passing the result texture.</summary>
        public event Action<Texture2D> OnPanoramaCaptureComplete;

        /// <summary>Fired if an error occurs during capture, passing an error message.</summary>
        public event Action<string> OnPanoramaCaptureFailed;

        #endregion

        #region Inspector

        [Header("Capture Settings")]
        [Tooltip("Camera used to render panorama faces. Defaults to Camera.main if null.")]
        [SerializeField] private Camera _captureCamera;

        [Tooltip("Resolution (pixels) of each cubemap face or strip frame.")]
        [SerializeField] [Min(64)] private int _faceResolution = AdvancedPhotographyConfig.PanoramaDefaultFaceResolution;

        [Header("Little Planet")]
        [Tooltip("Stereographic scale factor for Little Planet output.")]
        [SerializeField] [Range(0.1f, 3f)] private float _littlePlanetScale = 1f;

        #endregion

        #region Private State

        private bool _capturing = false;
        private float _progress = 0f;
        private Coroutine _captureCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_captureCamera == null)
                _captureCamera = Camera.main;
        }

        #endregion

        #region Public API

        /// <summary>Begins a panorama capture of the specified type.</summary>
        public void StartCapture(PanoramaType type)
        {
            if (_capturing)
            {
                Debug.LogWarning("[SWEF] AdvancedPanoramaController: capture already in progress.");
                return;
            }

            _captureCoroutine = StartCoroutine(CaptureCoroutine(type));
        }

        /// <summary>Cancels an in-progress capture.</summary>
        public void CancelCapture()
        {
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
            }
            _capturing = false;
            _progress  = 0f;
        }

        /// <summary>Returns the current capture progress as a normalised value in [0, 1].</summary>
        public float GetCaptureProgress() => _progress;

        /// <summary>Sets the cubemap face resolution for future captures.</summary>
        public void SetFaceResolution(int resolution)
        {
            _faceResolution = Mathf.Max(64, resolution);
        }

        #endregion

        #region Private — Capture Pipeline

        private IEnumerator CaptureCoroutine(PanoramaType type)
        {
            _capturing = true;
            _progress  = 0f;
            OnPanoramaCaptureStarted?.Invoke();
            Debug.Log($"[SWEF] AdvancedPanoramaController: starting {type} capture");

            yield return null;

            try
            {
                Texture2D result = null;

                switch (type)
                {
                    case PanoramaType.Full360:
                    case PanoramaType.LittlePlanet:
                        result = CaptureCubemapEquirectangular(type == PanoramaType.LittlePlanet);
                        break;

                    case PanoramaType.Horizontal:
                        result = CaptureStrip(AdvancedPhotographyConfig.PanoramaHorizontalSteps, horizontal: true);
                        break;

                    case PanoramaType.Vertical:
                        result = CaptureStrip(AdvancedPhotographyConfig.PanoramaVerticalSteps, horizontal: false);
                        break;

                    default:
                        OnPanoramaCaptureFailed?.Invoke($"Unsupported PanoramaType: {type}");
                        _capturing = false;
                        yield break;
                }

                _progress = 1f;
                OnPanoramaCaptureProgress?.Invoke(1f);

                if (result != null)
                {
                    SaveToGallery(result, type);
                    OnPanoramaCaptureComplete?.Invoke(result);
                    AdvancedPhotographyAnalytics.RecordPanoramaCaptured(type);
                    Debug.Log($"[SWEF] AdvancedPanoramaController: {type} capture complete ({result.width}×{result.height})");
                }
                else
                {
                    OnPanoramaCaptureFailed?.Invoke("Result texture was null.");
                }
            }
            catch (Exception ex)
            {
                OnPanoramaCaptureFailed?.Invoke(ex.Message);
                Debug.LogError($"[SWEF] AdvancedPanoramaController: capture failed — {ex.Message}");
            }
            finally
            {
                _capturing = false;
            }
        }

        // ── Full360 / LittlePlanet ────────────────────────────────────────────────

        private Texture2D CaptureCubemapEquirectangular(bool littlePlanet)
        {
            if (_captureCamera == null) return null;

            int    size    = _faceResolution;
            var    cubemap = new Cubemap(size, TextureFormat.RGB24, false);
            _captureCamera.RenderToCubemap(cubemap);

            int   outW   = size * 4;
            int   outH   = size * 2;
            var   result = new Texture2D(outW, outH, TextureFormat.RGB24, false);

            Color[] pixels = new Color[outW * outH];

            for (int y = 0; y < outH; y++)
            {
                for (int x = 0; x < outW; x++)
                {
                    float u     = (x + 0.5f) / outW;
                    float v     = (y + 0.5f) / outH;
                    float theta = (u * 2f - 1f) * Mathf.PI;
                    float phi   = (v - 0.5f) * Mathf.PI;

                    float px = Mathf.Cos(phi) * Mathf.Cos(theta);
                    float py = Mathf.Sin(phi);
                    float pz = Mathf.Cos(phi) * Mathf.Sin(theta);

                    if (littlePlanet)
                    {
                        // Stereographic re-projection
                        float scale = _littlePlanetScale;
                        float rho   = Mathf.Sqrt(px * px + pz * pz);
                        float alpha = Mathf.Atan2(py, rho);
                        float r     = Mathf.Tan(Mathf.PI / 4f - alpha / 2f) * scale;
                        px = r * px / (rho + 1e-6f);
                        pz = r * pz / (rho + 1e-6f);
                        py = 1f;
                    }

                    pixels[y * outW + x] = cubemap.GetPixel(CubeFaceFromDirection(px, py, pz),
                        SampleCubeFaceU(px, py, pz, size),
                        SampleCubeFaceV(px, py, pz, size));
                }
            }

            result.SetPixels(pixels);
            result.Apply();
            UnityEngine.Object.Destroy(cubemap);
            return result;
        }

        private CubemapFace CubeFaceFromDirection(float x, float y, float z)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z);
            if (ax >= ay && ax >= az) return x > 0 ? CubemapFace.PositiveX : CubemapFace.NegativeX;
            if (ay >= ax && ay >= az) return y > 0 ? CubemapFace.PositiveY : CubemapFace.NegativeY;
            return z > 0 ? CubemapFace.PositiveZ : CubemapFace.NegativeZ;
        }

        private int SampleCubeFaceU(float x, float y, float z, int size)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z);
            float sc, tc, ma;
            if (ax >= ay && ax >= az) { sc = x > 0 ? -z : z; tc = -y; ma = ax; }
            else if (ay >= az)        { sc = x;                tc = y > 0 ? z : -z; ma = ay; }
            else                      { sc = z > 0 ? x : -x;  tc = -y; ma = az; }
            return Mathf.Clamp((int)(((sc / ma + 1f) / 2f) * size), 0, size - 1);
        }

        private int SampleCubeFaceV(float x, float y, float z, int size)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z);
            float tc, ma;
            if (ax >= ay && ax >= az)      { tc = -y; ma = ax; }
            else if (ay >= az)             { tc = y > 0 ? z : -z; ma = ay; }
            else                           { tc = -y; ma = az; }
            return Mathf.Clamp((int)(((tc / ma + 1f) / 2f) * size), 0, size - 1);
        }

        // ── Strip Panoramas ───────────────────────────────────────────────────────

        private Texture2D CaptureStrip(int steps, bool horizontal)
        {
            if (_captureCamera == null) return null;

            float fov   = _captureCamera.fieldOfView;
            float step  = (360f / steps) * (1f - AdvancedPhotographyConfig.PanoramaOverlapPercent / 100f);
            int   faceW = _faceResolution;
            int   faceH = (int)(_faceResolution * (horizontal ? 1f : 1.5f));
            int   totalW = horizontal ? faceW * steps : faceW;
            int   totalH = horizontal ? faceH : faceH * steps;

            var result = new Texture2D(totalW, totalH, TextureFormat.RGB24, false);
            var rt     = new RenderTexture(faceW, faceH, 24);

            Quaternion originalRot = _captureCamera.transform.rotation;

            for (int i = 0; i < steps; i++)
            {
                float angle = i * step;
                _captureCamera.transform.rotation = Quaternion.Euler(
                    horizontal ? 0f : angle,
                    horizontal ? angle : 0f,
                    0f);

                _captureCamera.targetTexture = rt;
                _captureCamera.Render();
                _captureCamera.targetTexture = null;

                RenderTexture.active = rt;
                var strip = new Texture2D(faceW, faceH, TextureFormat.RGB24, false);
                strip.ReadPixels(new Rect(0, 0, faceW, faceH), 0, 0);
                strip.Apply();
                RenderTexture.active = null;

                int ox = horizontal ? i * faceW : 0;
                int oy = horizontal ? 0 : i * faceH;
                result.SetPixels(ox, oy, faceW, faceH, strip.GetPixels());
                UnityEngine.Object.Destroy(strip);

                _progress = (float)(i + 1) / steps;
                OnPanoramaCaptureProgress?.Invoke(_progress);
            }

            _captureCamera.transform.rotation = originalRot;
            result.Apply();
            UnityEngine.Object.Destroy(rt);
            return result;
        }

        // ── Gallery Save ──────────────────────────────────────────────────────────

        private void SaveToGallery(Texture2D texture, PanoramaType type)
        {
#if SWEF_PHOTOMODE_AVAILABLE
            PhotoGalleryManager.Instance?.SaveTexture(texture, $"panorama_{type}_{DateTime.UtcNow:yyyyMMddHHmmss}.png");
#endif
        }

        #endregion
    }
}
