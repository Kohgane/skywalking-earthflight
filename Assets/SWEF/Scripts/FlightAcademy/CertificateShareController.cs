using UnityEngine;

// Optional integration compile guards
#if SWEF_SOCIAL_AVAILABLE
using SWEF.Social;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Renders a certificate to a <see cref="RenderTexture"/> and exposes
    /// share / save operations via the native platform share sheet.
    /// </summary>
    public class CertificateShareController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private Camera _certificateCamera;
        [SerializeField] private Canvas _certificateCanvas;
        [SerializeField] private int _textureWidth  = 1200;
        [SerializeField] private int _textureHeight = 800;

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders <paramref name="certificate"/> to a texture and triggers the
        /// native share sheet. Falls back to clipboard copy if sharing is unavailable.
        /// </summary>
        public void ShareCertificate(Certificate certificate)
        {
            if (certificate == null) return;
            var tex = RenderCertificate(certificate);

#if SWEF_SOCIAL_AVAILABLE
            var shareManager = FindObjectOfType<ShareManager>();
            if (shareManager != null)
            {
                shareManager.ShareTexture(tex, FormatShareCaption(certificate));
                return;
            }
#endif
            // Clipboard fallback
            GUIUtility.systemCopyBuffer = CertificateGenerator.FormatCertificateText(certificate);
            Debug.Log("[CertificateShareController] Certificate text copied to clipboard.");

            if (tex != null)
                Destroy(tex);
        }

        /// <summary>
        /// Renders <paramref name="certificate"/> to a PNG and saves it to the
        /// device gallery / persistent data path.
        /// </summary>
        public void SaveCertificateImage(Certificate certificate)
        {
            if (certificate == null) return;
            var tex = RenderCertificate(certificate);
            if (tex == null) return;

            byte[] png = tex.EncodeToPNG();
            string path = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"certificate_{certificate.certificateId}.png");
            System.IO.File.WriteAllBytes(path, png);
            Debug.Log($"[CertificateShareController] Certificate saved to {path}");
            Destroy(tex);
        }

        // ── Private helpers ───────────────────────────────────────────────────────
        private Texture2D RenderCertificate(Certificate certificate)
        {
            if (_certificateCamera == null)
            {
                Debug.LogWarning("[CertificateShareController] No certificate camera assigned.");
                return null;
            }

            var rt = new RenderTexture(_textureWidth, _textureHeight, 24);
            _certificateCamera.targetTexture = rt;
            _certificateCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(_textureWidth, _textureHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _textureWidth, _textureHeight), 0, 0);
            tex.Apply();

            _certificateCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            return tex;
        }

        private static string FormatShareCaption(Certificate certificate)
        {
            return $"I just earned my {certificate.licenseGrade} pilot license in SkyWalking EarthFlight! ✈️ #{certificate.licenseGrade}";
        }
    }
}
