// UnderwaterEffects.cs — Phase 117: Advanced Ocean & Maritime System
// Below-surface effects: murky water, light shafts, bubble trails.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Manages underwater visual and audio effects when the
    /// player camera goes below the ocean surface. Applies murky water
    /// post-processing, god-ray light shafts, and bubble trails.
    /// </summary>
    public class UnderwaterEffects : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        [Header("Underwater Visuals")]
        [SerializeField] private Color underwaterFogColour = new Color(0.04f, 0.25f, 0.35f);
        [SerializeField] private float underwaterFogDensity = 0.05f;
        [SerializeField] private float underwaterFogStart   = 0f;
        [SerializeField] private float underwaterFogEnd     = 80f;

        [Header("Light Shafts")]
        [SerializeField] private GameObject lightShaftsRoot;

        [Header("Bubble Trail")]
        [SerializeField] private ParticleSystem bubbleTrail;

        [Header("Audio")]
        [SerializeField] private AudioSource underwaterAudio;

        // ── Private state ─────────────────────────────────────────────────────────

        private bool  _isUnderwater;
        private Color _originalFogColour;
        private float _originalFogDensity;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            _originalFogColour  = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;

            if (playerCamera == null) playerCamera = Camera.main;
        }

        private void Update()
        {
            CheckUnderwaterState();
        }

        // ── Underwater Detection ──────────────────────────────────────────────────

        private void CheckUnderwaterState()
        {
            if (playerCamera == null) return;

            var mgr = OceanSystemManager.Instance;
            float surfaceY = mgr != null
                ? mgr.GetSurfaceHeight(new Vector2(playerCamera.transform.position.x,
                                                     playerCamera.transform.position.z))
                : 0f;

            bool underwater = playerCamera.transform.position.y < surfaceY;
            if (underwater == _isUnderwater) return;

            _isUnderwater = underwater;
            if (underwater)
                EnterUnderwater();
            else
                ExitUnderwater();
        }

        private void EnterUnderwater()
        {
            RenderSettings.fogColor    = underwaterFogColour;
            RenderSettings.fogDensity  = underwaterFogDensity;
            RenderSettings.fogMode     = FogMode.Linear;
            RenderSettings.fogStartDistance = underwaterFogStart;
            RenderSettings.fogEndDistance   = underwaterFogEnd;

            if (lightShaftsRoot != null) lightShaftsRoot.SetActive(true);
            if (bubbleTrail     != null) bubbleTrail.Play();
            if (underwaterAudio != null) underwaterAudio.Play();
        }

        private void ExitUnderwater()
        {
            RenderSettings.fogColor   = _originalFogColour;
            RenderSettings.fogDensity = _originalFogDensity;
            RenderSettings.fogMode    = FogMode.Exponential;

            if (lightShaftsRoot != null) lightShaftsRoot.SetActive(false);
            if (bubbleTrail     != null) bubbleTrail.Stop();
            if (underwaterAudio != null) underwaterAudio.Stop();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns whether the camera is currently underwater.</summary>
        public bool IsUnderwater => _isUnderwater;
    }
}
