using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Audio
{
    /// <summary>
    /// Event-driven audio trigger component. Maps named game events to AudioClips with
    /// per-event cooldowns to prevent spam. Supports both 2D and 3D spatial playback.
    /// </summary>
    public class AudioEventTrigger : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        [Serializable]
        public class AudioEventMapping
        {
            public string    eventName;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float     volume   = 1f;
            public bool      spatial  = false;
            [Tooltip("Minimum seconds between successive triggers of this event.")]
            public float     cooldown = 0f;

            [HideInInspector] public float cooldownTimer;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Event Mappings")]
        [SerializeField]
        private List<AudioEventMapping> mappings = new List<AudioEventMapping>()
        {
            new AudioEventMapping { eventName = "Takeoff",          spatial = false, cooldown = 2f },
            new AudioEventMapping { eventName = "Landing",          spatial = false, cooldown = 2f },
            new AudioEventMapping { eventName = "SpeedBoost",       spatial = false, cooldown = 1f },
            new AudioEventMapping { eventName = "AltitudeWarning",  spatial = false, cooldown = 5f },
            new AudioEventMapping { eventName = "Teleport",         spatial = false, cooldown = 1f },
            new AudioEventMapping { eventName = "Screenshot",       spatial = false, cooldown = 0.5f },
            new AudioEventMapping { eventName = "Achievement",      spatial = false, cooldown = 1f },
            new AudioEventMapping { eventName = "FavoriteSave",     spatial = false, cooldown = 0.5f },
        };

        [Header("Refs (auto-found if null)")]
        [SerializeField] private SpatialAudioManager spatialAudioManager;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioSource _2dSource;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (spatialAudioManager == null)
                spatialAudioManager = FindFirstObjectByType<SpatialAudioManager>();

            _2dSource = gameObject.AddComponent<AudioSource>();
            _2dSource.spatialBlend = 0f;
            _2dSource.playOnAwake  = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var m in mappings)
                if (m.cooldownTimer > 0f) m.cooldownTimer -= dt;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Plays the clip mapped to <paramref name="eventName"/> (2D variant).</summary>
        public void TriggerEvent(string eventName)
        {
            var m = FindMapping(eventName);
            if (m == null || m.clip == null || m.cooldownTimer > 0f) return;
            m.cooldownTimer = m.cooldown;

            if (m.spatial)
                spatialAudioManager?.PlayAtPosition(m.clip, transform.position, m.volume);
            else
                _2dSource.PlayOneShot(m.clip, m.volume);
        }

        /// <summary>Plays the clip mapped to <paramref name="eventName"/> at the given world position.</summary>
        public void TriggerEvent(string eventName, Vector3 position)
        {
            var m = FindMapping(eventName);
            if (m == null || m.clip == null || m.cooldownTimer > 0f) return;
            m.cooldownTimer = m.cooldown;

            if (spatialAudioManager != null)
                spatialAudioManager.PlayAtPosition(m.clip, position, m.volume, m.spatial ? 1f : 0f);
            else
                AudioSource.PlayClipAtPoint(m.clip, position, m.volume);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private AudioEventMapping FindMapping(string eventName)
        {
            foreach (var m in mappings)
                if (string.Equals(m.eventName, eventName, StringComparison.OrdinalIgnoreCase))
                    return m;
            return null;
        }
    }
}
