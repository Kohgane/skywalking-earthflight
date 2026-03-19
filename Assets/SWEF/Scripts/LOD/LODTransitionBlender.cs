using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Terrain;

namespace SWEF.LOD
{
    /// <summary>
    /// Handles smooth visual cross-fade transitions between LOD levels using
    /// alpha dithering on the chunk's MeshRenderer material.
    /// Prevents hard popping when LOD level changes.
    /// </summary>
    public class LODTransitionBlender : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Transition")]
        [SerializeField] private float defaultDuration = 0.5f;

        // ── Internal state ───────────────────────────────────────────────────────
        private readonly Dictionary<TerrainChunk, Coroutine> _activeTransitions =
            new Dictionary<TerrainChunk, Coroutine>();

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Starts an alpha-based cross-fade transition for <paramref name="chunk"/>
        /// between LOD levels <paramref name="from"/> and <paramref name="to"/>.
        /// If a transition is already running for this chunk it is cancelled first.
        /// </summary>
        public void StartTransition(
            TerrainChunk   chunk,
            TerrainLODLevel from,
            TerrainLODLevel to,
            float           duration = -1f)
        {
            if (chunk == null) return;
            if (duration < 0f) duration = defaultDuration;

            // Cancel any existing transition
            if (_activeTransitions.TryGetValue(chunk, out var existing) && existing != null)
                StopCoroutine(existing);

            var cr = StartCoroutine(TransitionCoroutine(chunk, from, to, duration));
            _activeTransitions[chunk] = cr;
        }

        /// <summary>Returns <c>true</c> if <paramref name="chunk"/> is currently transitioning.</summary>
        public bool IsTransitioning(TerrainChunk chunk)
        {
            return chunk != null && _activeTransitions.TryGetValue(chunk, out var cr) && cr != null;
        }

        // ── Coroutine ────────────────────────────────────────────────────────────
        private IEnumerator TransitionCoroutine(
            TerrainChunk    chunk,
            TerrainLODLevel from,
            TerrainLODLevel to,
            float           duration)
        {
            MeshRenderer renderer = chunk.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                chunk.UpdateLOD(to);
                _activeTransitions.Remove(chunk);
                yield break;
            }

            // Switch to destination LOD immediately (alpha fade handled on material)
            chunk.UpdateLOD(to);

            // Fade the material from transparent to opaque over duration
            Material mat = renderer.material; // instance copy for modification
            if (mat == null)
            {
                _activeTransitions.Remove(chunk);
                yield break;
            }

            // Attempt to use _BaseColor alpha if the shader supports it
            bool hasBaseColor = mat.HasProperty("_BaseColor");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (hasBaseColor)
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = t;
                    mat.SetColor("_BaseColor", c);
                }

                yield return null;
            }

            // Ensure fully opaque at end
            if (hasBaseColor)
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
            }

            _activeTransitions.Remove(chunk);
        }
    }
}
