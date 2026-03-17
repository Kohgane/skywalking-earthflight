using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Renders a top-down mini-map in a corner of the screen using a secondary camera
    /// and a <see cref="RenderTexture"/>.
    /// Supports configurable zoom, position, and altitude-adaptive orthographic size.
    /// Show/hide state and position are persisted via PlayerPrefs.
    /// </summary>
    public class MiniMapController : MonoBehaviour
    {
        public enum MiniMapPosition { TopLeft, TopRight, BottomLeft, BottomRight }

        private const string PrefVisible  = "SWEF_MiniMapVisible";
        private const string PrefPosition = "SWEF_MiniMapPosition";

        [SerializeField] private Camera miniMapCamera;
        [SerializeField] private RenderTexture miniMapTexture;
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private Transform playerMarker;

        [Header("Mini-Map Settings")]
        [SerializeField] private float miniMapHeight = 500f;
        [SerializeField] private float miniMapSize   = 1000f;
        [SerializeField] private bool  showMiniMap   = true;

        /// <summary>Raised when the mini-map is toggled on or off.</summary>
        public event Action<bool> OnMiniMapToggled;

        /// <summary>Whether the mini-map is currently visible.</summary>
        public bool IsVisible => showMiniMap;

        private Transform _playerTransform;
        private MiniMapPosition _currentPosition = MiniMapPosition.TopRight;

        private void Awake()
        {
            // Auto-create render texture if not assigned
            if (miniMapTexture == null)
            {
                miniMapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
                miniMapTexture.Create();
            }

            if (miniMapCamera != null)
            {
                miniMapCamera.orthographic     = true;
                miniMapCamera.targetTexture    = miniMapTexture;
                miniMapCamera.orthographicSize = miniMapSize;
            }

            if (miniMapImage != null)
                miniMapImage.texture = miniMapTexture;

            // Restore persisted state
            showMiniMap      = PlayerPrefs.GetInt(PrefVisible, 1) == 1;
            _currentPosition = (MiniMapPosition)PlayerPrefs.GetInt(PrefPosition, (int)MiniMapPosition.TopRight);

            ApplyVisibility();
            ApplyPosition();

            // Find player
            var flight = FindFirstObjectByType<Flight.FlightController>();
            if (flight != null) _playerTransform = flight.transform;
        }

        private void LateUpdate()
        {
            if (!showMiniMap || miniMapCamera == null || _playerTransform == null) return;

            // Position camera directly above player
            Vector3 above = _playerTransform.position + Vector3.up * miniMapHeight;
            miniMapCamera.transform.position = above;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Scale orthographic size with altitude
            var altCtrl = _playerTransform.GetComponent<Flight.AltitudeController>();
            float altitude = altCtrl != null ? altCtrl.CurrentAltitudeMeters : 0f;
            float scaledSize = miniMapSize + altitude * 0.05f;
            miniMapCamera.orthographicSize = scaledSize;

            // Update optional player marker
            if (playerMarker != null)
                playerMarker.rotation = Quaternion.Euler(0f, 0f, -_playerTransform.eulerAngles.y);
        }

        /// <summary>Toggles the mini-map on or off.</summary>
        public void ToggleMiniMap()
        {
            showMiniMap = !showMiniMap;
            PlayerPrefs.SetInt(PrefVisible, showMiniMap ? 1 : 0);
            ApplyVisibility();
            OnMiniMapToggled?.Invoke(showMiniMap);
        }

        /// <summary>Sets the orthographic zoom size of the mini-map camera.</summary>
        public void SetMiniMapZoom(float size)
        {
            miniMapSize = size;
            if (miniMapCamera != null)
                miniMapCamera.orthographicSize = size;
        }

        /// <summary>Positions the mini-map overlay in a screen corner.</summary>
        public void SetMiniMapPosition(MiniMapPosition pos)
        {
            _currentPosition = pos;
            PlayerPrefs.SetInt(PrefPosition, (int)pos);
            ApplyPosition();
        }

        private void ApplyVisibility()
        {
            if (miniMapImage != null) miniMapImage.gameObject.SetActive(showMiniMap);
            if (miniMapCamera != null) miniMapCamera.gameObject.SetActive(showMiniMap);
        }

        private void ApplyPosition()
        {
            if (miniMapImage == null) return;

            RectTransform rt = miniMapImage.rectTransform;
            float w = rt.rect.width;
            float h = rt.rect.height;

            switch (_currentPosition)
            {
                case MiniMapPosition.TopLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(w * 0.5f, -h * 0.5f);
                    break;
                case MiniMapPosition.TopRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-w * 0.5f, -h * 0.5f);
                    break;
                case MiniMapPosition.BottomLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(w * 0.5f, h * 0.5f);
                    break;
                case MiniMapPosition.BottomRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-w * 0.5f, h * 0.5f);
                    break;
            }
        }
    }
}
