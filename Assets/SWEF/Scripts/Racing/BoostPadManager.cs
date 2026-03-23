// BoostPadManager.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Data describing a single world-placed boost pad or aerial boost ring.
    /// </summary>
    [Serializable]
    public class BoostPadData
    {
        /// <summary>Unique identifier assigned at registration time.</summary>
        public string id = Guid.NewGuid().ToString();

        /// <summary>World-space centre position of the pad.</summary>
        [Tooltip("World-space centre position of the pad.")]
        public Vector3 position = Vector3.zero;

        /// <summary>World-space rotation of the pad (defines the boost direction).</summary>
        [Tooltip("World-space rotation of the pad — forward direction is the boost direction.")]
        public Quaternion rotation = Quaternion.identity;

        /// <summary>Size of the trigger volume (half-extents for box pads, radius for rings).</summary>
        [Tooltip("Trigger volume size in metres.")]
        public Vector3 size = new Vector3(4f, 1f, 8f);

        /// <summary>Boost config applied to the player on activation.</summary>
        [Tooltip("Boost config applied when the player passes through this pad.")]
        public BoostConfig boostConfig;

        /// <summary>Seconds before the pad reactivates after being triggered.</summary>
        [Tooltip("Cooldown in seconds before the pad can trigger again.")]
        public float respawnCooldown = 3f;

        /// <summary>Visual style of this pad.</summary>
        [Tooltip("Visual style category (Ground, AerialRing, WallMount, Dive).")]
        public BoostPadStyle style = BoostPadStyle.Ground;

        /// <summary>Whether this pad is currently on cooldown.</summary>
        [NonSerialized] public bool onCooldown;

        /// <summary>Time at which the current cooldown expires.</summary>
        [NonSerialized] public float cooldownEndTime;
    }

    /// <summary>
    /// Phase 62 — Singleton that manages all world-placed boost pads and aerial
    /// boost rings, handles trigger detection, cooldown timers, and forwards
    /// activations to <see cref="BoostController"/>.
    ///
    /// <para>Pads can be registered/unregistered at runtime for procedural
    /// placement or dynamic track loading.</para>
    /// </summary>
    public class BoostPadManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static BoostPadManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Default Pads")]
        [Tooltip("Boost pads registered at startup (populated in the Inspector or via script).")]
        [SerializeField] private List<BoostPadData> startupPads = new List<BoostPadData>();

        [Header("Proximity")]
        [Tooltip("Distance within which a pad is surfaced to the minimap integration.")]
        [SerializeField] private float minimapProximityRange = 300f;

        #endregion

        #region Events

        /// <summary>Fired whenever a player activates a boost pad.</summary>
        public event Action<BoostPadData> OnPadActivated;

        #endregion

        #region Public Properties

        /// <summary>Read-only list of all registered pads.</summary>
        public IReadOnlyList<BoostPadData> Pads => _pads;

        #endregion

        #region Private State

        private readonly List<BoostPadData>            _pads        = new List<BoostPadData>(64);
        private readonly Dictionary<string, GameObject> _padObjects  = new Dictionary<string, GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var pad in startupPads)
                RegisterPad(pad);
        }

        private void Update()
        {
            TickCooldowns();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers a new boost pad into the manager.
        /// A trigger collider proxy is created automatically.
        /// </summary>
        /// <param name="pad">Pad data to register.</param>
        public void RegisterPad(BoostPadData pad)
        {
            if (pad == null || _pads.Contains(pad)) return;
            _pads.Add(pad);
            CreateTriggerProxy(pad);
        }

        /// <summary>
        /// Removes a registered boost pad and destroys its trigger proxy.
        /// </summary>
        /// <param name="padId">The <see cref="BoostPadData.id"/> of the pad to remove.</param>
        public void UnregisterPad(string padId)
        {
            for (int i = _pads.Count - 1; i >= 0; i--)
            {
                if (_pads[i].id == padId)
                {
                    if (_padObjects.TryGetValue(padId, out GameObject proxy))
                    {
                        if (proxy != null) Destroy(proxy);
                        _padObjects.Remove(padId);
                    }
                    _pads.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns all pads within <see cref="minimapProximityRange"/> of the given world position.
        /// </summary>
        /// <param name="position">Query origin in world space.</param>
        /// <returns>List of nearby pad data entries.</returns>
        public List<BoostPadData> GetNearbyPads(Vector3 position)
        {
            var result = new List<BoostPadData>();
            float rangeSq = minimapProximityRange * minimapProximityRange;
            foreach (var pad in _pads)
            {
                if ((pad.position - position).sqrMagnitude <= rangeSq)
                    result.Add(pad);
            }
            return result;
        }

        /// <summary>
        /// Called by the trigger proxy component when the player enters a pad volume.
        /// </summary>
        /// <param name="padId">ID of the activated pad.</param>
        public void NotifyPadTriggered(string padId)
        {
            BoostPadData pad = FindPad(padId);
            if (pad == null || pad.onCooldown) return;

            pad.onCooldown      = true;
            pad.cooldownEndTime = Time.time + pad.respawnCooldown;

            if (BoostController.Instance != null && pad.boostConfig != null)
                BoostController.Instance.ApplyBoost(pad.boostConfig);

            OnPadActivated?.Invoke(pad);
            UpdatePadVisual(padId, dimmed: true);
        }

        #endregion

        #region Private Helpers

        private void TickCooldowns()
        {
            foreach (var pad in _pads)
            {
                if (pad.onCooldown && Time.time >= pad.cooldownEndTime)
                {
                    pad.onCooldown = false;
                    UpdatePadVisual(pad.id, dimmed: false);
                }
            }
        }

        private void CreateTriggerProxy(BoostPadData pad)
        {
            var go = new GameObject($"BoostPad_{pad.id}");
            go.transform.SetPositionAndRotation(pad.position, pad.rotation);

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer < 0)
            {
                Debug.LogWarning("[BoostPadManager] 'Trigger' layer not found. " +
                    "Add a layer named 'Trigger' in Project Settings > Tags and Layers.");
                triggerLayer = 0; // Default layer fallback.
            }
            go.layer = triggerLayer;

            // Choose collider shape based on style.
            if (pad.style == BoostPadStyle.AerialRing)
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius    = Mathf.Max(pad.size.x, pad.size.z) * 0.5f;
            }
            else
            {
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size      = pad.size;
            }

            var listener = go.AddComponent<BoostPadTriggerListener>();
            listener.Initialize(pad.id, this);

            _padObjects[pad.id] = go;
        }

        private void UpdatePadVisual(string padId, bool dimmed)
        {
            // Visual dimming is handled by the VFX bridge or a renderer on the proxy.
            if (!_padObjects.TryGetValue(padId, out GameObject proxy) || proxy == null) return;
            var renderers = proxy.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                Color c = r.material.color;
                c.a             = dimmed ? 0.3f : 1f;
                r.material.color = c;
            }
        }

        private BoostPadData FindPad(string id)
        {
            foreach (var pad in _pads)
                if (pad.id == id) return pad;
            return null;
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var pad in _pads)
            {
                Gizmos.matrix = Matrix4x4.TRS(pad.position, pad.rotation, Vector3.one);
                if (pad.style == BoostPadStyle.AerialRing)
                    Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(pad.size.x, pad.size.z) * 0.5f);
                else
                    Gizmos.DrawWireCube(Vector3.zero, pad.size);

                // Boost direction arrow.
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(Vector3.zero, Vector3.forward * 5f);
                Gizmos.color = Color.yellow;
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif

        #endregion
    }

    /// <summary>
    /// Internal MonoBehaviour placed on boost pad trigger proxy GameObjects.
    /// Forwards <c>OnTriggerEnter</c> events back to <see cref="BoostPadManager"/>.
    /// </summary>
    [AddComponentMenu("")] // Hide from Add Component menu.
    public sealed class BoostPadTriggerListener : MonoBehaviour
    {
        private string           _padId;
        private BoostPadManager  _manager;

        /// <summary>Initialises the listener with the owning pad's ID and manager reference.</summary>
        public void Initialize(string padId, BoostPadManager manager)
        {
            _padId   = padId;
            _manager = manager;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only local player triggers activate pads.
            if (!other.CompareTag("Player")) return;
            _manager?.NotifyPadTriggered(_padId);
        }
    }
}
