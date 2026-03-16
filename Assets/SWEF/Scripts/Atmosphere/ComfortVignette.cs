using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Comfort vignette: darkens screen edges during rapid rotation/acceleration.
    /// Drives a CanvasGroup alpha on a vignette overlay Image.
    /// Only active when FlightController.comfortMode is true.
    /// </summary>
    public class ComfortVignette : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private CanvasGroup vignetteOverlay;

        [Header("Thresholds")]
        [SerializeField] private float rotationThreshold = 30f;
        [SerializeField] private float maxRotationForFullVignette = 90f;

        [Header("Tuning")]
        [SerializeField] private float maxAlpha = 0.6f;
        [SerializeField] private float fadeInSpeed = 8f;
        [SerializeField] private float fadeOutSpeed = 4f;

        private Quaternion _prevRotation;
        private float _currentAlpha;

        private void Start()
        {
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();

            _prevRotation = transform.rotation;

            if (vignetteOverlay != null)
                vignetteOverlay.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (flight == null || vignetteOverlay == null) return;

            if (!flight.comfortMode)
            {
                _currentAlpha = 0f;
                vignetteOverlay.alpha = 0f;
                _prevRotation = transform.rotation;
                return;
            }

            float dt = Time.deltaTime;

            Quaternion deltaRot = transform.rotation * Quaternion.Inverse(_prevRotation);
            _prevRotation = transform.rotation;

            deltaRot.ToAngleAxis(out float angleDeg, out _);
            if (angleDeg > 180f) angleDeg = 360f - angleDeg;
            float angularSpeed = dt > 0f ? angleDeg / dt : 0f;

            float targetAlpha = 0f;
            if (angularSpeed > rotationThreshold)
            {
                float normalized = Mathf.InverseLerp(rotationThreshold, maxRotationForFullVignette, angularSpeed);
                targetAlpha = normalized * maxAlpha;
            }

            float speed = targetAlpha > _currentAlpha ? fadeInSpeed : fadeOutSpeed;
            _currentAlpha = ExpSmoothing.ExpLerp(_currentAlpha, targetAlpha, speed, dt);

            vignetteOverlay.alpha = _currentAlpha;
        }
    }
}
