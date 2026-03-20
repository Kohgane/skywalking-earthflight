using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Decodes received frame data (simulated H.264/VP9 via raw texture updates)
    /// and applies the result to a full-screen <see cref="RawImage"/>.
    /// Provides simple frame interpolation to smooth playback when frames are late.
    /// </summary>
    public class FrameDecoder : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Output")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private int textureWidth  = 1920;
        [SerializeField] private int textureHeight = 1080;

        [Header("Interpolation")]
        [SerializeField] private bool enableInterpolation = true;
        [SerializeField] private float interpolationAlpha = 0.5f;

        // ── Internal state ────────────────────────────────────────────────────────
        private RenderTexture _outputTexture;
        private Texture2D     _decodeBuffer;
        private Texture2D     _prevBuffer;
        private bool          _hasFrame;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The RenderTexture to which decoded frames are written.</summary>
        public RenderTexture OutputTexture => _outputTexture;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Detect color space to choose the correct RenderTexture format
            var format = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.Default;

            _outputTexture = new RenderTexture(textureWidth, textureHeight, 0, format)
            {
                name       = "CloudFrameOutput",
                filterMode = FilterMode.Bilinear,
            };
            _outputTexture.Create();

            _decodeBuffer = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
            _prevBuffer   = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);

            if (displayImage != null)
                displayImage.texture = _outputTexture;
        }

        private void OnDestroy()
        {
            if (_outputTexture != null) _outputTexture.Release();
            if (_decodeBuffer   != null) Destroy(_decodeBuffer);
            if (_prevBuffer     != null) Destroy(_prevBuffer);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Decodes a raw frame payload and pushes it to <see cref="OutputTexture"/>.
        /// In a production system this would invoke a native H.264/VP9 decoder;
        /// here we perform a direct pixel upload as a simulation.
        /// </summary>
        public void DecodeFrame(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            // Preserve previous frame for interpolation
            if (enableInterpolation && _hasFrame)
                CopyTexture(_decodeBuffer, _prevBuffer);

            // Load raw RGB24 bytes directly when they fill the buffer exactly;
            // otherwise fall back to Unity's image decoder (JPEG/PNG stubs)
            int expectedRaw = textureWidth * textureHeight * 3;
            if (data.Length == expectedRaw)
            {
                _decodeBuffer.LoadRawTextureData(data);
            }
            else
            {
                _decodeBuffer.LoadImage(data); // handles JPEG / PNG
            }
            _decodeBuffer.Apply();

            if (enableInterpolation && _hasFrame)
                BlendAndUpload();
            else
                UploadToRenderTexture(_decodeBuffer);

            _hasFrame = true;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void BlendAndUpload()
        {
            // Simple CPU-side lerp — for a real implementation this would use a shader
            var currentPixels = _decodeBuffer.GetPixels32();
            var prevPixels    = _prevBuffer.GetPixels32();
            float alpha = Mathf.Clamp01(interpolationAlpha);

            for (int i = 0; i < currentPixels.Length; i++)
            {
                currentPixels[i] = Color32.Lerp(prevPixels[i], currentPixels[i], alpha);
            }
            _decodeBuffer.SetPixels32(currentPixels);
            _decodeBuffer.Apply();
            UploadToRenderTexture(_decodeBuffer);
        }

        private static void CopyTexture(Texture2D src, Texture2D dst)
        {
            dst.SetPixels32(src.GetPixels32());
            dst.Apply();
        }

        private void UploadToRenderTexture(Texture2D source)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _outputTexture;
            Graphics.Blit(source, _outputTexture);
            RenderTexture.active = prev;
        }
    }
}
