using UnityEngine;
using SWEF.Util;

namespace SWEF.Flight
{
    /// <summary>
    /// Maps an altitude slider value (meters) to local Y position.
    /// MVP uses experiential altitude (local Y), not geodetic height.
    /// Range: 0 m (ground) to 120,000 m (edge of space / Kármán line x1.2).
    /// </summary>
    public class AltitudeController : MonoBehaviour
    {
        [SerializeField] private Transform rig;

        [Header("Altitude")]
        [SerializeField] private float minAltitude = 0f;
        [SerializeField] private float maxAltitude = 120000f;
        [SerializeField] private float altitudeSmoothing = 4f;

        public float TargetAltitudeMeters { get; private set; } = 30f;
        public float CurrentAltitudeMeters => rig != null ? rig.localPosition.y : 0f;

        private void Awake()
        {
            if (rig == null) rig = transform;
            TargetAltitudeMeters = Mathf.Clamp(rig.localPosition.y, minAltitude, maxAltitude);
        }

        public void SetTargetAltitude(float meters)
        {
            TargetAltitudeMeters = Mathf.Clamp(meters, minAltitude, maxAltitude);
        }

        private void Update()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;
            var p = rig.localPosition;
            float y = ExpSmoothing.ExpLerp(p.y, TargetAltitudeMeters, altitudeSmoothing, dt);
            rig.localPosition = new Vector3(p.x, y, p.z);
        }
    }
}
