// VesselRenderer.cs — Phase 117: Advanced Ocean & Maritime System
// Vessel rendering: LOD models, wake generation, running lights, flags.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages rendering of a maritime vessel including LOD switching,
    /// wake particle effects, running lights at night, and flag display.
    /// </summary>
    public class VesselRenderer : MonoBehaviour
    {
        // ── LOD Distance Thresholds ───────────────────────────────────────────────

        private const float LodDistanceLow  = 2000f;
        private const float LodDistanceMed  = 800f;
        private const float LodDistanceHigh = 300f;

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("LOD Models")]
        [SerializeField] private GameObject highDetailModel;
        [SerializeField] private GameObject medDetailModel;
        [SerializeField] private GameObject lowDetailModel;
        [SerializeField] private GameObject billboardSprite;

        [Header("Wake")]
        [SerializeField] private TrailRenderer[] wakeTrails;
        [SerializeField] private ParticleSystem bowWave;

        [Header("Running Lights")]
        [SerializeField] private Light portLight;    // red
        [SerializeField] private Light starboardLight; // green
        [SerializeField] private Light mastLight;    // white
        [SerializeField] private Light sternLight;   // white

        [Header("Flag")]
        [SerializeField] private Renderer flagRenderer;
        [SerializeField] private Texture2D flagTexture;

        // ── Private state ─────────────────────────────────────────────────────────

        private Camera _mainCamera;
        private bool   _nightLightsActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            _mainCamera = Camera.main;
            ApplyFlag();
        }

        private void Update()
        {
            UpdateLOD();
            UpdateRunningLights();
            UpdateWakeEmission();
        }

        // ── LOD ───────────────────────────────────────────────────────────────────

        private void UpdateLOD()
        {
            if (_mainCamera == null) return;
            float dist = Vector3.Distance(transform.position, _mainCamera.transform.position);

            bool high   = dist <= LodDistanceHigh;
            bool med    = !high && dist <= LodDistanceMed;
            bool low    = !high && !med && dist <= LodDistanceLow;
            bool billboard = dist > LodDistanceLow;

            if (highDetailModel  != null) highDetailModel.SetActive(high);
            if (medDetailModel   != null) medDetailModel.SetActive(med);
            if (lowDetailModel   != null) lowDetailModel.SetActive(low);
            if (billboardSprite  != null) billboardSprite.SetActive(billboard);
        }

        // ── Running Lights ────────────────────────────────────────────────────────

        private void UpdateRunningLights()
        {
            // Night = 20:00 – 06:00 (approximation)
            float hour = (Time.time / 60f) % 24f; // rudimentary time-of-day stand-in
            bool  night = hour >= 20f || hour < 6f;

            if (night == _nightLightsActive) return;
            _nightLightsActive = night;

            SetLight(portLight,      night, Color.red);
            SetLight(starboardLight, night, Color.green);
            SetLight(mastLight,      night, Color.white);
            SetLight(sternLight,     night, Color.white);
        }

        private static void SetLight(Light l, bool on, Color colour)
        {
            if (l == null) return;
            l.enabled = on;
            l.color   = colour;
        }

        // ── Wake ──────────────────────────────────────────────────────────────────

        private void UpdateWakeEmission()
        {
            var vc = GetComponent<VesselController>();
            if (vc == null) return;
            bool moving = vc.CurrentSpeedKnots > 1f;

            if (wakeTrails != null)
                foreach (var t in wakeTrails)
                    if (t != null) t.emitting = moving;

            if (bowWave != null)
            {
                if (moving && !bowWave.isPlaying) bowWave.Play();
                else if (!moving && bowWave.isPlaying) bowWave.Stop();
            }
        }

        // ── Flag ──────────────────────────────────────────────────────────────────

        private void ApplyFlag()
        {
            if (flagRenderer != null && flagTexture != null)
                flagRenderer.material.mainTexture = flagTexture;
        }
    }
}
