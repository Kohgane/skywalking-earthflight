using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Places famous and procedurally-defined landmarks in the world.
    ///
    /// <para>Loads <see cref="LandmarkDefinition"/> entries sorted by
    /// <see cref="LandmarkDefinition.importanceRating"/> (highest first).  Each
    /// landmark is instantiated when the camera comes within its
    /// <see cref="LandmarkDefinition.visibilityDistance"/> and destroyed when
    /// it moves beyond that range.</para>
    ///
    /// <para>When the player first approaches a landmark
    /// <see cref="OnLandmarkDiscovered"/> is fired and, if a narration trigger ID
    /// is configured, the <c>SWEF.Narration.NarrationManager</c> is asked to
    /// play the associated track.</para>
    /// </summary>
    public class LandmarkPlacer : MonoBehaviour
    {
        #region Inspector

        [Header("Landmark Database")]
        [Tooltip("All landmark definitions. Sorted by importance at startup.")]
        [SerializeField] private List<LandmarkDefinition> landmarks = new List<LandmarkDefinition>();

        [Header("References")]
        [Tooltip("Camera used for visibility checks. Resolved at runtime if null.")]
        [SerializeField] private Camera visibilityCamera;

        [Header("Visuals")]
        [Tooltip("Material used for the discovery highlight glow effect.")]
        [SerializeField] private Material glowMaterial;

        [Tooltip("Duration (seconds) of the discovery glow pulse.")]
        [SerializeField] private float glowDuration = 3f;

        [Header("Streaming")]
        [Tooltip("How often (seconds) the placer checks distances for load/unload.")]
        [SerializeField] private float streamInterval = 3f;

        #endregion

        #region Events

        /// <summary>Fired after a landmark <see cref="GameObject"/> is first instantiated.</summary>
        public event Action<LandmarkDefinition, GameObject> OnLandmarkSpawned;

        /// <summary>Fired the first time the player enters a landmark's discovery radius.</summary>
        public event Action<LandmarkDefinition>             OnLandmarkDiscovered;

        /// <summary>Fired when the player requests detailed info about a landmark.</summary>
        public event Action<LandmarkDefinition>             OnLandmarkInfoRequested;

        #endregion

        #region Private State

        // Tracks runtime state for each landmark.
        private class LandmarkInstance
        {
            public LandmarkDefinition definition;
            public GameObject         gameObject;
            public bool               isDiscovered;
            public bool               isLoaded;
        }

        private readonly List<LandmarkInstance> _instances = new List<LandmarkInstance>();
        private Coroutine _streamCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Sort by importance descending so famous landmarks load first.
            landmarks.Sort((a, b) => b.importanceRating.CompareTo(a.importanceRating));

            foreach (var def in landmarks)
            {
                _instances.Add(new LandmarkInstance
                {
                    definition   = def,
                    isDiscovered = false,
                    isLoaded     = false
                });
            }
        }

        private void Start()
        {
            if (visibilityCamera == null) visibilityCamera = Camera.main;
            _streamCoroutine = StartCoroutine(StreamingLoop());
        }

        private void OnDestroy()
        {
            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns all landmark definitions whose
        /// <see cref="LandmarkDefinition.worldPosition"/> lies within
        /// <paramref name="radius"/> of <paramref name="pos"/>.
        /// </summary>
        public List<LandmarkDefinition> GetLandmarksInRadius(Vector3 pos, float radius)
        {
            float sqr    = radius * radius;
            var   result = new List<LandmarkDefinition>();
            foreach (var inst in _instances)
                if ((inst.definition.worldPosition - pos).sqrMagnitude <= sqr)
                    result.Add(inst.definition);
            return result;
        }

        /// <summary>Requests the info popup for a landmark (fires <see cref="OnLandmarkInfoRequested"/>).</summary>
        public void RequestInfo(LandmarkDefinition def)
        {
            OnLandmarkInfoRequested?.Invoke(def);
        }

        #endregion

        #region Streaming

        private IEnumerator StreamingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(streamInterval);
                if (visibilityCamera == null) continue;
                Vector3 camPos = visibilityCamera.transform.position;

                foreach (var inst in _instances)
                {
                    float dist = (inst.definition.worldPosition - camPos).magnitude;
                    if (dist <= inst.definition.visibilityDistance)
                    {
                        if (!inst.isLoaded) SpawnLandmark(inst);
                        if (!inst.isDiscovered) DiscoverLandmark(inst);
                    }
                    else
                    {
                        if (inst.isLoaded) DespawnLandmark(inst);
                    }
                }
            }
        }

        private void SpawnLandmark(LandmarkInstance inst)
        {
            LandmarkDefinition def = inst.definition;

            GameObject go;
            if (def.prefab != null)
            {
                go = Instantiate(def.prefab, def.worldPosition, Quaternion.identity);
            }
            else
            {
                // Procedural fallback: simple billboard / marker.
                go = CreateProceduralMarker(def);
            }

            go.name = $"Landmark_{def.landmarkName}";
            inst.gameObject = go;
            inst.isLoaded   = true;

            OnLandmarkSpawned?.Invoke(def, go);
        }

        private void DespawnLandmark(LandmarkInstance inst)
        {
            if (inst.gameObject != null) Destroy(inst.gameObject);
            inst.gameObject = null;
            inst.isLoaded   = false;
        }

        private void DiscoverLandmark(LandmarkInstance inst)
        {
            inst.isDiscovered = true;
            OnLandmarkDiscovered?.Invoke(inst.definition);

            // Play glow effect if the landmark has a GameObject.
            if (inst.gameObject != null)
                StartCoroutine(GlowEffect(inst.gameObject));

            // Forward to Narration system if a trigger ID is set.
            if (!string.IsNullOrEmpty(inst.definition.narrationTriggerId))
                TriggerNarration(inst.definition.narrationTriggerId);
        }

        private IEnumerator GlowEffect(GameObject target)
        {
            if (target == null || glowMaterial == null) yield break;

            var renderers = target.GetComponentsInChildren<Renderer>();
            // Use MaterialPropertyBlock to avoid mutating shared material assets.
            var block = new MaterialPropertyBlock();

            // Apply glow emission.
            block.SetColor("_EmissionColor", Color.white * 2f);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(block);
            }

            yield return new WaitForSeconds(glowDuration);

            // Clear the property block to restore default rendering.
            var clearBlock = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(clearBlock);
            }
        }

        #endregion

        #region Narration Integration

        private static void TriggerNarration(string triggerId)
        {
#if SWEF_NARRATION_AVAILABLE
            var narrationManager = Narration.NarrationManager.Instance;
            if (narrationManager != null)
                narrationManager.TriggerById(triggerId);
#endif
        }

        #endregion

        #region Procedural Marker

        private static GameObject CreateProceduralMarker(LandmarkDefinition def)
        {
            var root = new GameObject(def.landmarkName);
            root.transform.position = def.worldPosition;

            // Simple vertical pillar to mark the location.
            float importance = def.importanceRating / 10f;
            float height = 10f + importance * 90f;
            float radius = 2f + importance * 8f;

            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(root.transform, false);
            cylinder.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            cylinder.transform.localScale    = new Vector3(radius, height * 0.5f, radius);

            // Color by category.
            var renderer = cylinder.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat   = new Material(Shader.Find("Standard"));
                mat.color = CategoryColor(def.category);
                renderer.sharedMaterial = mat;
            }

            return root;
        }

        private static Color CategoryColor(LandmarkCategory category)
        {
            return category switch
            {
                LandmarkCategory.Natural       => new Color(0.2f, 0.7f, 0.3f),
                LandmarkCategory.Historical    => new Color(0.8f, 0.7f, 0.3f),
                LandmarkCategory.Architectural => new Color(0.3f, 0.5f, 0.9f),
                LandmarkCategory.Religious     => new Color(0.9f, 0.9f, 0.7f),
                LandmarkCategory.Cultural      => new Color(0.9f, 0.4f, 0.6f),
                LandmarkCategory.Engineering   => new Color(0.5f, 0.5f, 0.9f),
                _                              => Color.white
            };
        }

        #endregion
    }
}
