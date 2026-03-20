using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Audio
{
    /// <summary>
    /// Lightweight audio occlusion system. Each frame it raycasts from the top N closest
    /// active spatial audio sources to the listener and applies a lowpass filter and volume
    /// reduction when the path is blocked by terrain/obstacles.
    /// </summary>
    public class AudioOcclusionSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Occlusion")]
        [SerializeField] private LayerMask occlusionLayers = ~0;
        [SerializeField] private float maxRayDistance  = 2000f;
        [SerializeField] private float filterCutoff    = 800f;      // Hz when fully occluded
        [SerializeField] private float volumeReduction = 0.4f;      // multiplier when blocked

        [Header("Performance")]
        [SerializeField] private int   checkTopN       = 8;         // sources per frame
        [SerializeField] private float skipDistance    = 5000f;     // skip checks beyond this

        [Header("Refs (auto-found if null)")]
        [SerializeField] private SpatialAudioManager spatialAudioManager;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioListener _listener;
        private readonly List<AudioSource> _candidates = new List<AudioSource>();

        // Per-source lowpass filters, allocated on demand
        private readonly Dictionary<AudioSource, AudioLowPassFilter> _filters =
            new Dictionary<AudioSource, AudioLowPassFilter>();

        // Baseline volumes stored before occlusion is applied
        private readonly Dictionary<AudioSource, float> _baselineVolumes =
            new Dictionary<AudioSource, float>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (spatialAudioManager == null)
                spatialAudioManager = FindFirstObjectByType<SpatialAudioManager>();
        }

        private void Update()
        {
            if (_listener == null)
                _listener = FindFirstObjectByType<AudioListener>();
            if (_listener == null) return;

            Vector3 listenerPos = _listener.transform.position;

            // Gather all active sources from pool (access via public API)
            GatherCandidates(listenerPos);

            foreach (var src in _candidates)
            {
                if (src == null || !src.isPlaying) continue;

                float dist = Vector3.Distance(src.transform.position, listenerPos);
                if (dist > skipDistance)
                {
                    ResetOcclusion(src);
                    continue;
                }

                Vector3 direction = listenerPos - src.transform.position;
                bool blocked = Physics.Raycast(src.transform.position, direction.normalized,
                    Mathf.Min(direction.magnitude, maxRayDistance), occlusionLayers);

                if (blocked)
                    ApplyOcclusion(src);
                else
                    ResetOcclusion(src);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void GatherCandidates(Vector3 listenerPos)
        {
            _candidates.Clear();

            if (spatialAudioManager == null) return;

            // Iterate child AudioSources of the SpatialAudioManager
            spatialAudioManager.GetComponentsInChildren<AudioSource>(false, _candidates);

            // Sort by distance (closest first) then take top N
            _candidates.Sort((a, b) =>
            {
                float da = Vector3.SqrMagnitude(a.transform.position - listenerPos);
                float db = Vector3.SqrMagnitude(b.transform.position - listenerPos);
                return da.CompareTo(db);
            });

            if (_candidates.Count > checkTopN)
                _candidates.RemoveRange(checkTopN, _candidates.Count - checkTopN);
        }

        private void ApplyOcclusion(AudioSource src)
        {
            // Store baseline once per source (before any occlusion touches it)
            if (!_baselineVolumes.ContainsKey(src))
                _baselineVolumes[src] = src.volume;

            var lpf = GetOrAddFilter(src);
            if (lpf != null) lpf.cutoffFrequency = filterCutoff;
            src.volume = _baselineVolumes[src] * volumeReduction;
        }

        private void ResetOcclusion(AudioSource src)
        {
            if (_filters.TryGetValue(src, out var lpf) && lpf != null)
                lpf.cutoffFrequency = 22000f;

            if (_baselineVolumes.TryGetValue(src, out float baseline))
            {
                src.volume = baseline;
                _baselineVolumes.Remove(src);
            }
        }

        private AudioLowPassFilter GetOrAddFilter(AudioSource src)
        {
            if (_filters.TryGetValue(src, out var existing)) return existing;
            var lpf = src.GetComponent<AudioLowPassFilter>();
            if (lpf == null) lpf = src.gameObject.AddComponent<AudioLowPassFilter>();
            lpf.cutoffFrequency = 22000f;
            _filters[src] = lpf;
            return lpf;
        }
    }
}
