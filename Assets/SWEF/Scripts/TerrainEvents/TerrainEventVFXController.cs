// TerrainEventVFXController.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Manages visual effects (particle systems, prefabs, lighting) for
    /// all active terrain events.
    ///
    /// <para>Subscribes to <see cref="TerrainEventManager"/> events and spawns / despawns
    /// VFX GameObjects in sync with the event lifecycle.</para>
    /// </summary>
    public sealed class TerrainEventVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("VFX Settings")]
        [Tooltip("Parent transform under which all VFX objects are placed. Defaults to this transform.")]
        [SerializeField] private Transform _vfxRoot;

        [Tooltip("Fallback material applied to placeholder VFX objects when no prefab is found.")]
        [SerializeField] private Material _fallbackMaterial;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly Dictionary<TerrainEvent, GameObject> _vfxInstances =
            new Dictionary<TerrainEvent, GameObject>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_vfxRoot == null) _vfxRoot = transform;
        }

        private void Start()
        {
            if (TerrainEventManager.Instance != null)
            {
                TerrainEventManager.Instance.OnEventSpawned      += SpawnVFX;
                TerrainEventManager.Instance.OnEventPhaseChanged += UpdateVFX;
                TerrainEventManager.Instance.OnEventEnded        += DespawnVFX;
            }
        }

        private void OnDestroy()
        {
            if (TerrainEventManager.Instance != null)
            {
                TerrainEventManager.Instance.OnEventSpawned      -= SpawnVFX;
                TerrainEventManager.Instance.OnEventPhaseChanged -= UpdateVFX;
                TerrainEventManager.Instance.OnEventEnded        -= DespawnVFX;
            }
        }

        private void LateUpdate()
        {
            // Sync VFX position and scale each frame to match event state
            foreach (var kvp in _vfxInstances)
            {
                TerrainEvent ev = kvp.Key;
                GameObject   go = kvp.Value;
                if (ev == null || go == null) continue;
                go.transform.position   = ev.origin;
                float scale = ev.currentRadius / Mathf.Max(1f, ev.config?.effectRadius ?? 1f);
                go.transform.localScale = Vector3.one * scale;
            }
        }

        // ── VFX Lifecycle ─────────────────────────────────────────────────────────

        private void SpawnVFX(TerrainEvent ev)
        {
            if (ev?.config == null) return;

            GameObject vfxGo = TryLoadVFXPrefab(ev.config.vfxPrefabPath);
            if (vfxGo == null)
            {
                // Create a placeholder sphere for editor/testing visibility
                vfxGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vfxGo.name = $"VFX_{ev.config.eventName}";
                if (vfxGo.TryGetComponent<Collider>(out var col))
                    Destroy(col);
                if (_fallbackMaterial != null)
                    vfxGo.GetComponent<Renderer>().sharedMaterial = _fallbackMaterial;
            }

            vfxGo.transform.SetParent(_vfxRoot, worldPositionStays: true);
            vfxGo.transform.position   = ev.origin;
            vfxGo.transform.localScale = Vector3.one;
            vfxGo.SetActive(true);

            _vfxInstances[ev] = vfxGo;
            Debug.Log($"[SWEF] TerrainEventVFXController: spawned VFX for '{ev.config.eventName}'.");
        }

        private void UpdateVFX(TerrainEvent ev)
        {
            if (!_vfxInstances.TryGetValue(ev, out GameObject go) || go == null) return;

            // Toggle active state based on phase
            bool shouldBeVisible = ev.phase >= TerrainEventPhase.BuildUp && ev.phase <= TerrainEventPhase.Aftermath;
            go.SetActive(shouldBeVisible);

            // Type-specific VFX updates
            if (ev is AuroraEvent aurora)
                ApplyAuroraColor(go, aurora.currentColor);
        }

        private void DespawnVFX(TerrainEvent ev)
        {
            if (_vfxInstances.TryGetValue(ev, out GameObject go))
            {
                if (go != null) Destroy(go);
                _vfxInstances.Remove(ev);
                Debug.Log($"[SWEF] TerrainEventVFXController: despawned VFX for '{ev.config?.eventName}'.");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static GameObject TryLoadVFXPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            GameObject prefab = Resources.Load<GameObject>(path);
            return prefab != null ? Instantiate(prefab) : null;
        }

        private static void ApplyAuroraColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            // Use a property block to avoid modifying the shared material
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            rend.SetPropertyBlock(block);
        }
    }
}
