using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Events
{
    /// <summary>
    /// MonoBehaviour that manages the visual representation of active world events.
    /// Instantiates prefabs from Resources, scales them in on spawn, maintains particle
    /// systems during the active phase, then fades them out before destruction.
    /// </summary>
    public class EventVisualController : MonoBehaviour
    {
        // ── Inner types ───────────────────────────────────────────────────────────
        private class VisualEntry
        {
            public Guid             instanceId;
            public GameObject       root;
            public ParticleSystem[] particles;
            public int[]            originalMaxParticles; // cached to avoid compounding on SetVisualIntensity
            public Coroutine        lifecycleCoroutine;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Animation")]
        [Tooltip("Seconds taken to scale visuals in on spawn.")]
        [SerializeField] private float scaleInDuration  = 2f;

        [Tooltip("Seconds taken to fade visuals out when expiring.")]
        [SerializeField] private float fadeOutDuration  = 3f;

        [Tooltip("Maximum scale multiplier applied to the visual root.")]
        [SerializeField] private float maxScale = 1f;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly Dictionary<string, VisualEntry> _visuals =
            new Dictionary<string, VisualEntry>();

        private EventScheduler _scheduler;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            _scheduler = FindFirstObjectByType<EventScheduler>();
            if (_scheduler != null)
            {
                _scheduler.OnEventSpawned += SpawnVisual;
                _scheduler.OnEventExpired += inst => DespawnVisual(inst.instanceId);
            }
        }

        private void OnDisable()
        {
            if (_scheduler != null)
            {
                _scheduler.OnEventSpawned -= SpawnVisual;
                _scheduler.OnEventExpired -= inst => DespawnVisual(inst.instanceId);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Instantiates the visual prefab for the given event instance and begins the
        /// scale-in animation.
        /// </summary>
        /// <param name="instance">The event instance for which to spawn visuals.</param>
        public void SpawnVisual(WorldEventInstance instance)
        {
            if (instance == null) return;

            string key = instance.instanceId.ToString();
            if (_visuals.ContainsKey(key))
            {
                Debug.LogWarning($"[SWEF] EventVisualController: visual for {key} already exists.");
                return;
            }

            GameObject prefab = null;
            if (!string.IsNullOrEmpty(instance.eventData?.visualPrefabPath))
                prefab = Resources.Load<GameObject>(instance.eventData.visualPrefabPath);

            if (prefab == null)
            {
                // Fallback: create a placeholder sphere so the position is still visible in editor
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefab.name = "EventVisualPlaceholder";
                prefab.SetActive(false);
            }

            var root = Instantiate(prefab, instance.spawnPosition, Quaternion.identity);
            root.name = $"EventVisual_{instance.eventData?.eventId}_{key.Substring(0, 8)}";

            var entry = new VisualEntry
            {
                instanceId            = instance.instanceId,
                root                  = root,
                particles             = root.GetComponentsInChildren<ParticleSystem>()
            };
            entry.originalMaxParticles = new int[entry.particles.Length];
            for (int i = 0; i < entry.particles.Length; i++)
                entry.originalMaxParticles[i] = entry.particles[i].main.maxParticles;

            entry.lifecycleCoroutine = StartCoroutine(LifecycleCoroutine(entry, instance));
            _visuals[key] = entry;

            Debug.Log($"[SWEF] EventVisualController: spawned visual for event '{instance.eventData?.eventId}'.");
        }

        /// <summary>
        /// Begins the fade-out and destroys the visual for the specified instance.
        /// </summary>
        /// <param name="instanceId">Guid of the event instance whose visual should be removed.</param>
        public void DespawnVisual(Guid instanceId)
        {
            string key = instanceId.ToString();
            if (!_visuals.TryGetValue(key, out var entry)) return;

            if (entry.lifecycleCoroutine != null)
                StopCoroutine(entry.lifecycleCoroutine);

            entry.lifecycleCoroutine = StartCoroutine(FadeOutAndDestroy(entry.root, fadeOutDuration));
            _visuals.Remove(key);
        }

        /// <summary>
        /// Sets the visual intensity of particle systems and renderers for the given instance.
        /// </summary>
        /// <param name="instanceId">Target event instance Guid.</param>
        /// <param name="intensity">Normalised intensity value in [0, 1].</param>
        public void SetVisualIntensity(Guid instanceId, float intensity)
        {
            string key = instanceId.ToString();
            if (!_visuals.TryGetValue(key, out var entry)) return;

            intensity = Mathf.Clamp01(intensity);
            for (int i = 0; i < entry.particles.Length; i++)
            {
                var main = entry.particles[i].main;
                main.maxParticles = Mathf.RoundToInt(entry.originalMaxParticles[i] * intensity);
            }

            // Scale root by intensity
            entry.root.transform.localScale = Vector3.one * maxScale * intensity;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private IEnumerator LifecycleCoroutine(VisualEntry entry, WorldEventInstance instance)
        {
            // Scale in
            yield return ScaleOverTime(entry.root.transform, Vector3.zero, Vector3.one * maxScale, scaleInDuration);

            // Kick off particles
            foreach (var ps in entry.particles)
                ps.Play();

            // Wait until the event begins expiring
            while (instance.state == WorldEventState.Active)
                yield return null;

            // Fade out
            yield return FadeOutAndDestroy(entry.root, fadeOutDuration);
        }

        private IEnumerator ScaleOverTime(Transform t, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            t.localScale = to;
        }

        private IEnumerator FadeOutAndDestroy(GameObject root, float duration)
        {
            if (root == null) yield break;

            var renderers = root.GetComponentsInChildren<Renderer>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - elapsed / duration;
                foreach (var r in renderers)
                {
                    foreach (var mat in r.materials)
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
                yield return null;
            }

            Destroy(root);
        }
    }
}
