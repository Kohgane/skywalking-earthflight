using UnityEngine;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Renders a rotatable, zoomable 3-D preview of the player's aircraft inside
    /// the Hangar UI.  A dedicated <see cref="Camera"/> renders the model to a
    /// <c>RenderTexture</c> displayed in the UI; the preview model is driven by
    /// a separate <see cref="AircraftVisualController"/> instance.
    /// </summary>
    public class AircraftPreviewController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Preview References")]
        [SerializeField] private Transform previewPivot;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private AircraftVisualController previewVisualController;

        [Header("Orbit Controls")]
        [SerializeField] private float orbitSpeed = 180f;
        [SerializeField] private float zoomSpeed  = 5f;
        [SerializeField] private float minZoom    = 2f;
        [SerializeField] private float maxZoom    = 20f;

        [Header("Auto-Rotate")]
        [SerializeField] private float autoRotateSpeed = 20f;
        [SerializeField] private float idleTimeBeforeAutoRotate = 3f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private float _yaw   = 0f;
        private float _pitch = 15f;
        private float _zoom  = 8f;

        private float _idleTimer = 0f;
        private bool  _isDragging = false;

        private Vector2 _lastPointerPos;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            HandleInput();
            ApplyCameraTransform();

            if (!_isDragging)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleTimeBeforeAutoRotate)
                    _yaw += autoRotateSpeed * Time.deltaTime;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Applies the full <paramref name="loadout"/> to the preview model.</summary>
        public void ShowPreview(AircraftLoadout loadout)
        {
            if (previewVisualController != null)
                previewVisualController.ApplyLoadout(loadout);
        }

        /// <summary>
        /// Temporarily previews a single skin on the preview model without
        /// changing the player's actual equipped loadout.
        /// </summary>
        public void PreviewSingleSkin(AircraftPartType part, string skinId)
        {
            if (previewVisualController != null)
                previewVisualController.ApplyPart(part, skinId);
        }

        /// <summary>Resets the preview model to the currently equipped loadout.</summary>
        public void ResetPreview()
        {
            var mgr = AircraftCustomizationManager.Instance;
            if (mgr != null && mgr.ActiveLoadout != null)
                ShowPreview(mgr.ActiveLoadout);
        }

        /// <summary>Manually positions the preview camera orbit angles.</summary>
        public void SetRotation(float yaw, float pitch)
        {
            _yaw   = yaw;
            _pitch = Mathf.Clamp(pitch, -80f, 80f);
        }

        /// <summary>Manually sets the preview camera distance.</summary>
        public void SetZoom(float distance)
        {
            _zoom = Mathf.Clamp(distance, minZoom, maxZoom);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Mouse drag to orbit
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging    = true;
                _idleTimer     = 0f;
                _lastPointerPos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
                _isDragging = false;

            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastPointerPos;
                _yaw   += delta.x * orbitSpeed * Time.deltaTime;
                _pitch -= delta.y * orbitSpeed * Time.deltaTime;
                _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
                _lastPointerPos = Input.mousePosition;
                _idleTimer = 0f;
            }

            // Scroll wheel to zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _zoom -= scroll * zoomSpeed;
                _zoom  = Mathf.Clamp(_zoom, minZoom, maxZoom);
                _idleTimer = 0f;
            }
#else
            // Touch input: one finger = orbit, two fingers = pinch zoom
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _isDragging = true;
                    _idleTimer  = 0f;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _isDragging = false;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    _yaw   += touch.deltaPosition.x * orbitSpeed * Time.deltaTime;
                    _pitch -= touch.deltaPosition.y * orbitSpeed * Time.deltaTime;
                    _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
                    _idleTimer = 0f;
                }
            }
            else if (Input.touchCount == 2)
            {
                _isDragging = false;
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);

                float prevMag = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                float curMag  = (t0.position - t1.position).magnitude;
                float diff    = prevMag - curMag;

                _zoom += diff * zoomSpeed * Time.deltaTime;
                _zoom  = Mathf.Clamp(_zoom, minZoom, maxZoom);
                _idleTimer = 0f;
            }
            else
            {
                _isDragging = false;
            }
#endif
        }

        private void ApplyCameraTransform()
        {
            if (previewCamera == null || previewPivot == null) return;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_zoom);
            previewCamera.transform.position = previewPivot.position + offset;
            previewCamera.transform.LookAt(previewPivot.position);
        }
    }
}
