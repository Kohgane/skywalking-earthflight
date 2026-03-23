using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>360° panoramic photo capture controller for in-flight panorama photography.</summary>
    public class PanoramaCaptureController : MonoBehaviour
    {
        #region Constants

        private const int CUBE_FACE_COUNT = 6;
        private const string PANORAMA_FOLDER = "Panoramas";

        #endregion

        #region Enums

        /// <summary>Type of panoramic capture to perform.</summary>
        public enum CaptureType
        {
            Spherical360,
            Cylindrical180,
            WidePanorama
        }

        #endregion

        #region Events

        /// <summary>Fired when a panorama capture sequence begins.</summary>
        public static event Action OnPanoramaCaptureStarted;

        /// <summary>Fired with progress value 0-1 as cube faces are captured.</summary>
        public static event Action<float> OnPanoramaCaptureProgress;

        /// <summary>Fired when all faces are captured and panorama is assembled. Supplies the output path.</summary>
        public static event Action<string> OnPanoramaCaptureCompleted;

        #endregion

        #region Inspector

        [Header("Capture Settings")]
        [SerializeField, Tooltip("Type of panoramic projection to use.")]
        private CaptureType _captureType = CaptureType.Spherical360;

        [SerializeField, Tooltip("Resolution (pixels) of each cube face.")]
        private int _faceResolution = 2048;

        [SerializeField, Tooltip("Anti-aliasing sample count per face (1 = off, 2, 4, 8).")]
        private int _antiAliasingSamples = 4;

        [SerializeField, Tooltip("Output image format.")]
        private PhotoFormat _outputFormat = PhotoFormat.PNG;

        [SerializeField, Range(1, 100), Tooltip("JPEG quality if JPG format is selected.")]
        private int _jpegQuality = 90;

        [Header("References")]
        [SerializeField, Tooltip("Camera used for panorama cube-face rendering. Falls back to main camera.")]
        private Camera _captureCamera;

        [SerializeField]
        private PhotoGalleryManager _galleryManager;

        [Header("Preview")]
        [SerializeField, Tooltip("Sphere mesh used for the in-UI 360 viewer. Optional.")]
        private GameObject _previewSphere;

        #endregion

        #region Private State

        private bool _isCapturing;
        private Texture2D _panoramaTexture;

        // Cube-face rotation offsets (right, left, up, down, forward, back)
        private static readonly Quaternion[] s_faceRotations =
        {
            Quaternion.Euler(0f,  90f, 0f),
            Quaternion.Euler(0f, -90f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler( 90f, 0f, 0f),
            Quaternion.Euler(0f,   0f, 0f),
            Quaternion.Euler(0f, 180f, 0f)
        };

        private static readonly string[] s_faceNames =
            { "right", "left", "up", "down", "forward", "back" };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_captureCamera == null)
                _captureCamera = Camera.main;

            if (_galleryManager == null)
                _galleryManager = FindObjectOfType<PhotoGalleryManager>();
        }

        #endregion

        #region Public API

        /// <summary>Start a panoramic capture sequence. Returns immediately; capture is async.</summary>
        public void BeginCapture()
        {
            if (_isCapturing)
            {
                Debug.LogWarning("[PanoramaCaptureController] Capture already in progress.");
                return;
            }

            StartCoroutine(CaptureRoutine());
        }

        /// <summary>Show the last captured panorama in the preview sphere (if assigned).</summary>
        public void ShowPreview()
        {
            if (_panoramaTexture == null || _previewSphere == null) return;

            var renderer = _previewSphere.GetComponent<Renderer>();
            if (renderer == null) return;

            renderer.material.mainTexture = _panoramaTexture;
            _previewSphere.SetActive(true);
        }

        /// <summary>Hide the 360 preview sphere.</summary>
        public void HidePreview()
        {
            if (_previewSphere != null)
                _previewSphere.SetActive(false);
        }

        /// <summary>Whether a panorama capture is currently running.</summary>
        public bool IsCapturing => _isCapturing;

        /// <summary>Returns the panorama texture from the last successful capture, or null.</summary>
        public Texture2D LastPanoramaTexture => _panoramaTexture;

        #endregion

        #region Capture Coroutine

        private IEnumerator CaptureRoutine()
        {
            _isCapturing = true;
            OnPanoramaCaptureStarted?.Invoke();

            // Pause time so the scene doesn't change during multi-frame capture
            float savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            Texture2D[] faces = new Texture2D[CUBE_FACE_COUNT];
            Quaternion originalRotation = _captureCamera.transform.rotation;

            for (int i = 0; i < CUBE_FACE_COUNT; i++)
            {
                _captureCamera.transform.rotation = originalRotation * s_faceRotations[i];

                yield return new WaitForEndOfFrame();

                faces[i] = CaptureFace();
                OnPanoramaCaptureProgress?.Invoke((i + 1f) / CUBE_FACE_COUNT);
            }

            // Restore camera and time
            _captureCamera.transform.rotation = originalRotation;
            Time.timeScale = savedTimeScale;

            // Assemble equirectangular panorama
            _panoramaTexture = AssemblePanorama(faces, _captureType);

            // Dispose face textures
            foreach (var face in faces)
                Destroy(face);

            string outputPath = SavePanorama(_panoramaTexture);

            _galleryManager?.RefreshGallery();

            _isCapturing = false;
            OnPanoramaCaptureCompleted?.Invoke(outputPath);
        }

        private Texture2D CaptureFace()
        {
            var rt = new RenderTexture(_faceResolution, _faceResolution, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.antiAliasing = Mathf.Clamp(_antiAliasingSamples, 1, 8);

            var previousTarget = _captureCamera.targetTexture;
            float previousFov = _captureCamera.fieldOfView;

            _captureCamera.fieldOfView = 90f;
            _captureCamera.targetTexture = rt;
            _captureCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(_faceResolution, _faceResolution, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _faceResolution, _faceResolution), 0, 0);
            tex.Apply();

            _captureCamera.targetTexture = previousTarget;
            _captureCamera.fieldOfView = previousFov;
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);

            return tex;
        }

        #endregion

        #region Panorama Assembly

        private Texture2D AssemblePanorama(Texture2D[] faces, CaptureType type)
        {
            int outWidth, outHeight;

            switch (type)
            {
                case CaptureType.Cylindrical180:
                    outWidth  = _faceResolution * 2;
                    outHeight = _faceResolution;
                    break;
                case CaptureType.WidePanorama:
                    outWidth  = _faceResolution * 3;
                    outHeight = _faceResolution;
                    break;
                default: // Spherical360
                    outWidth  = _faceResolution * 4;
                    outHeight = _faceResolution * 2;
                    break;
            }

            var result = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);

            for (int y = 0; y < outHeight; y++)
            {
                for (int x = 0; x < outWidth; x++)
                {
                    float u = (x + 0.5f) / outWidth;
                    float v = (y + 0.5f) / outHeight;
                    Color sample = SampleCubemap(faces, u, v);
                    result.SetPixel(x, y, sample);
                }
            }

            result.Apply();
            return result;
        }

        private Color SampleCubemap(Texture2D[] faces, float u, float v)
        {
            // Convert equirectangular UV to spherical direction
            float theta = u * 2f * Mathf.PI - Mathf.PI;   // longitude  [-π, π]
            float phi   = v * Mathf.PI;                     // colatitude [0, π]

            float dx = Mathf.Sin(phi) * Mathf.Sin(theta);
            float dy = Mathf.Cos(phi);
            float dz = Mathf.Sin(phi) * Mathf.Cos(theta);

            // Select cube face
            float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy), az = Mathf.Abs(dz);
            float fu, fv;
            int faceIndex;

            if (ax >= ay && ax >= az)
            {
                if (dx > 0) { faceIndex = 0; fu = -dz / ax; fv = -dy / ax; }   // +X right
                else        { faceIndex = 1; fu =  dz / ax; fv = -dy / ax; }   // -X left
            }
            else if (ay >= ax && ay >= az)
            {
                if (dy > 0) { faceIndex = 2; fu =  dx / ay; fv =  dz / ay; }   // +Y up
                else        { faceIndex = 3; fu =  dx / ay; fv = -dz / ay; }   // -Y down
            }
            else
            {
                if (dz > 0) { faceIndex = 4; fu =  dx / az; fv = -dy / az; }   // +Z forward
                else        { faceIndex = 5; fu = -dx / az; fv = -dy / az; }   // -Z back
            }

            fu = (fu + 1f) * 0.5f;
            fv = (fv + 1f) * 0.5f;

            return faces[faceIndex].GetPixelBilinear(fu, fv);
        }

        #endregion

        #region File IO

        private string SavePanorama(Texture2D tex)
        {
            string folder = Path.Combine(Application.persistentDataPath, PANORAMA_FOLDER);
            Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ext       = _outputFormat == PhotoFormat.JPEG ? "jpg" : "png";
            string fileName  = $"panorama_{timestamp}.{ext}";
            string path      = Path.Combine(folder, fileName);

            byte[] bytes = _outputFormat == PhotoFormat.JPEG
                ? tex.EncodeToJPG(_jpegQuality)
                : tex.EncodeToPNG();

            try
            {
                File.WriteAllBytes(path, bytes);
                Debug.Log($"[PanoramaCaptureController] Panorama saved: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PanoramaCaptureController] Save failed: {ex.Message}");
            }

            return path;
        }

        #endregion
    }
}
