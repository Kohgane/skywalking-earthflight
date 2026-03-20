using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Audio
{
    /// <summary>
    /// Manages reverb zones/filters that transition between presets as altitude changes.
    /// Uses Unity's <see cref="AudioReverbFilter"/> on the listener. Can be disabled via
    /// performance settings.
    /// </summary>
    public class EnvironmentReverbController : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        [Serializable]
        public struct ReverbPreset
        {
            public string name;
            public float minAltitude;
            public float maxAltitude;
            [Range(-10000f, 0f)]
            public float dryLevel;
            [Range(-10000f, 0f)]
            public float room;
            [Range(-10000f, 0f)]
            public float roomHF;
            [Range(0.1f, 20f)]
            public float decayTime;
            [Range(-10000f, 1000f)]
            public float reflections;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Reverb Presets (ordered low → high)")]
        [SerializeField] private ReverbPreset[] presets = new ReverbPreset[]
        {
            new ReverbPreset { name="Ground",    minAltitude=     0f, maxAltitude=  500f, dryLevel=    0f, room= -1000f, roomHF= -100f, decayTime=1.5f, reflections= -300f },
            new ReverbPreset { name="MidAir",    minAltitude=   500f, maxAltitude=10000f, dryLevel=    0f, room= -3000f, roomHF= -300f, decayTime=0.5f, reflections=-2000f },
            new ReverbPreset { name="HighAlt",   minAltitude= 10000f, maxAltitude=80000f, dryLevel=    0f, room= -5000f, roomHF= -800f, decayTime=4.0f, reflections=-4000f },
            new ReverbPreset { name="Space",     minAltitude= 80000f, maxAltitude=9999999f, dryLevel=-1000f, room=-10000f, roomHF=-10000f, decayTime=0.1f, reflections=-10000f },
        };

        [Header("Transition")]
        [SerializeField] private float blendSpeed = 2f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private AltitudeController altitudeController;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioReverbFilter _filter;

        // Smoothed values
        private float _curDry, _curRoom, _curRoomHF, _curDecay, _curReflections;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<AltitudeController>();

            // Attach filter to the AudioListener (or this GameObject as fallback)
            var listenerGo = FindFirstObjectByType<AudioListener>()?.gameObject ?? gameObject;
            _filter = listenerGo.GetComponent<AudioReverbFilter>();
            if (_filter == null)
                _filter = listenerGo.AddComponent<AudioReverbFilter>();

            _filter.reverbPreset = AudioReverbPreset.Off;
        }

        private void Update()
        {
            float alt = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float dt  = Time.deltaTime;

            BlendedValues(alt, out float tDry, out float tRoom, out float tRoomHF,
                              out float tDecay, out float tRefl);

            _curDry         = ExpSmoothing.ExpLerp(_curDry,         tDry,   blendSpeed, dt);
            _curRoom        = ExpSmoothing.ExpLerp(_curRoom,        tRoom,  blendSpeed, dt);
            _curRoomHF      = ExpSmoothing.ExpLerp(_curRoomHF,      tRoomHF, blendSpeed, dt);
            _curDecay       = ExpSmoothing.ExpLerp(_curDecay,       tDecay, blendSpeed, dt);
            _curReflections = ExpSmoothing.ExpLerp(_curReflections, tRefl,  blendSpeed, dt);

            _filter.reverbPreset = AudioReverbPreset.User;
            _filter.dryLevel     = _curDry;
            _filter.room         = _curRoom;
            _filter.roomHF       = _curRoomHF;
            _filter.decayTime    = Mathf.Max(0.1f, _curDecay);
            _filter.reflections  = _curReflections;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void BlendedValues(float alt,
            out float dry, out float room, out float roomHF,
            out float decay, out float refl)
        {
            // Find the two surrounding presets and lerp
            ReverbPreset lo = presets[0], hi = presets[presets.Length - 1];
            float t = 0f;

            for (int i = 0; i < presets.Length - 1; i++)
            {
                if (alt >= presets[i].minAltitude && alt < presets[i + 1].minAltitude)
                {
                    lo = presets[i];
                    hi = presets[i + 1];
                    float range = presets[i + 1].minAltitude - presets[i].minAltitude;
                    t = range > 0f ? (alt - presets[i].minAltitude) / range : 0f;
                    break;
                }
            }

            dry    = Mathf.Lerp(lo.dryLevel,     hi.dryLevel,     t);
            room   = Mathf.Lerp(lo.room,         hi.room,         t);
            roomHF = Mathf.Lerp(lo.roomHF,       hi.roomHF,       t);
            decay  = Mathf.Lerp(lo.decayTime,    hi.decayTime,    t);
            refl   = Mathf.Lerp(lo.reflections,  hi.reflections,  t);
        }

        /// <summary>Enables or disables the reverb filter component.</summary>
        public void SetEnabled(bool enabled)
        {
            if (_filter != null) _filter.enabled = enabled;
            this.enabled = enabled;
        }
    }
}
