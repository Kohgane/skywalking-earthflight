using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Minimap
{
    /// <summary>
    /// Singleton MonoBehaviour that owns the master list of <see cref="MinimapBlip"/> objects.
    /// Every <c>LateUpdate</c> it refreshes <c>distanceFromPlayer</c> and <c>bearingDeg</c> on
    /// every active blip and hides blips that exceed <see cref="maxRenderRange"/>.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class MinimapManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MinimapManager Instance { get; private set; }

        // ── Inspector fields ───────────────────────────────────────────────────────
        [Header("Player Reference")]
        [Tooltip("Transform of the local player. Auto-found from FlightController if left empty.")]
        [SerializeField] private Transform playerTransform;

        [Header("Render Settings")]
        [Tooltip("Blips whose world-space distance from the player exceeds this value are temporarily hidden.")]
        [SerializeField] private float maxRenderRange = 5000f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new blip is added to the registry.</summary>
        public event Action<MinimapBlip> OnBlipRegistered;

        /// <summary>Fired when a blip is removed from the registry.</summary>
        public event Action<MinimapBlip> OnBlipUnregistered;

        /// <summary>Fired after all blips have been updated in <c>LateUpdate</c>.</summary>
        public event Action<MinimapBlip> OnBlipUpdated;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<MinimapBlip>               _blips  = new List<MinimapBlip>();
        private readonly Dictionary<string, MinimapBlip> _byId   = new Dictionary<string, MinimapBlip>();

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SWEF] MinimapManager: initialised.");
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null)
                {
                    playerTransform = fc.transform;
                    Debug.Log("[SWEF] MinimapManager: auto-found FlightController as player transform.");
                }
                else
                {
                    Debug.LogWarning("[SWEF] MinimapManager: FlightController not found; bearing/distance will not be calculated.");
                }
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null) return;

            Vector3 playerPos     = playerTransform.position;
            Vector3 playerForward = playerTransform.forward;

            for (int i = 0; i < _blips.Count; i++)
            {
                MinimapBlip blip = _blips[i];

                // Distance
                float dist = Vector3.Distance(playerPos, blip.worldPosition);
                blip.distanceFromPlayer = dist;

                // Bearing: angle from player forward to blip direction, clockwise 0-360
                Vector3 toBlip     = blip.worldPosition - playerPos;
                toBlip.y           = 0f;
                Vector3 forward2d  = new Vector3(playerForward.x, 0f, playerForward.z);

                if (toBlip.sqrMagnitude > 0.001f && forward2d.sqrMagnitude > 0.001f)
                {
                    float angle = Vector3.SignedAngle(forward2d, toBlip, Vector3.up);
                    blip.bearingDeg = (angle + 360f) % 360f;
                }

                OnBlipUpdated?.Invoke(blip);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a blip to the registry. If a blip with the same <see cref="MinimapBlip.blipId"/>
        /// already exists it is replaced.
        /// </summary>
        public void RegisterBlip(MinimapBlip blip)
        {
            if (blip == null || string.IsNullOrEmpty(blip.blipId))
            {
                Debug.LogWarning("[SWEF] MinimapManager: attempted to register a null or ID-less blip.");
                return;
            }

            if (_byId.ContainsKey(blip.blipId))
            {
                // Replace existing
                int idx = _blips.FindIndex(b => b.blipId == blip.blipId);
                if (idx >= 0) _blips[idx] = blip;
                _byId[blip.blipId] = blip;
            }
            else
            {
                _blips.Add(blip);
                _byId[blip.blipId] = blip;
            }

            OnBlipRegistered?.Invoke(blip);
            Debug.Log($"[SWEF] MinimapManager: registered blip '{blip.blipId}' ({blip.iconType}).");
        }

        /// <summary>
        /// Removes the blip with the given <paramref name="blipId"/> from the registry.
        /// No-op if not found.
        /// </summary>
        public void UnregisterBlip(string blipId)
        {
            if (!_byId.TryGetValue(blipId, out MinimapBlip blip)) return;

            _blips.Remove(blip);
            _byId.Remove(blipId);
            OnBlipUnregistered?.Invoke(blip);
            Debug.Log($"[SWEF] MinimapManager: unregistered blip '{blipId}'.");
        }

        /// <summary>Returns the blip with the given <paramref name="blipId"/>, or <c>null</c>.</summary>
        public MinimapBlip GetBlip(string blipId)
        {
            _byId.TryGetValue(blipId, out MinimapBlip blip);
            return blip;
        }

        /// <summary>Returns all blips whose <see cref="MinimapBlip.isActive"/> flag is <c>true</c>
        /// and whose <see cref="MinimapBlip.distanceFromPlayer"/> is within <see cref="MaxRenderRange"/>.</summary>
        public List<MinimapBlip> GetActiveBlips() =>
            _blips.Where(b => b.isActive && b.distanceFromPlayer <= maxRenderRange).ToList();

        /// <summary>Returns the complete blip list regardless of active state.</summary>
        public List<MinimapBlip> GetAllBlips() => new List<MinimapBlip>(_blips);

        /// <summary>Removes all blips from the registry and fires <see cref="OnBlipUnregistered"/> for each.</summary>
        public void ClearAll()
        {
            var copy = new List<MinimapBlip>(_blips);
            _blips.Clear();
            _byId.Clear();
            foreach (var b in copy)
                OnBlipUnregistered?.Invoke(b);
            Debug.Log("[SWEF] MinimapManager: all blips cleared.");
        }

        /// <summary>The Transform used as the player reference for bearing/distance calculations.</summary>
        public Transform PlayerTransform => playerTransform;

        /// <summary>Sets the player transform at runtime (e.g. after a scene reload).</summary>
        public void SetPlayerTransform(Transform t) => playerTransform = t;

        /// <summary>Maximum world-unit distance at which blips remain active.</summary>
        public float MaxRenderRange
        {
            get => maxRenderRange;
            set => maxRenderRange = value;
        }
    }
}
