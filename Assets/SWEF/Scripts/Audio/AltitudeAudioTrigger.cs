using UnityEngine;
using SWEF.Flight;

namespace SWEF.Audio
{
    /// <summary>
    /// Plays an altitude-warning sound effect when the player crosses configured
    /// altitude thresholds (default: Kármán line at 100 km and space at 120 km).
    /// A per-threshold cooldown prevents rapid re-triggering.
    /// Requires AudioManager.Instance to be present in the scene.
    /// </summary>
    public class AltitudeAudioTrigger : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField] private AltitudeController altitudeSource;

        [Header("Thresholds (meters)")]
        [SerializeField] private float[] altitudeThresholds = { 100000f, 120000f };

        [Header("Cooldown")]
        [SerializeField] private float cooldownSeconds = 10f;

        private float[] _cooldownTimers;
        private float   _previousAltitude;

        private void Awake()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            _cooldownTimers = new float[altitudeThresholds.Length];
        }

        private void Start()
        {
            if (altitudeSource != null)
                _previousAltitude = altitudeSource.CurrentAltitudeMeters;
        }

        private void Update()
        {
            if (altitudeSource == null || altitudeThresholds.Length == 0) return;

            float current = altitudeSource.CurrentAltitudeMeters;
            float dt      = Time.deltaTime;

            for (int i = 0; i < altitudeThresholds.Length; i++)
            {
                // Tick down cooldown
                if (_cooldownTimers[i] > 0f)
                    _cooldownTimers[i] -= dt;

                // Detect crossing in either direction
                bool crossedUp   = _previousAltitude < altitudeThresholds[i] && current >= altitudeThresholds[i];
                bool crossedDown = _previousAltitude > altitudeThresholds[i] && current <= altitudeThresholds[i];

                if ((crossedUp || crossedDown) && _cooldownTimers[i] <= 0f)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(4); // AltitudeWarning

                    _cooldownTimers[i] = cooldownSeconds;
                }
            }

            _previousAltitude = current;
        }
    }
}
