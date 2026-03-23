using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TimeCapsule
{
    /// <summary>
    /// Places and manages 3D world-space markers at the location of each
    /// <see cref="TimeCapsule"/>. Markers are color-coded by
    /// <see cref="TimeCapsuleStatus"/> and forward click events to the
    /// <see cref="TimeCapsuleUI"/> for detail display.
    /// </summary>
    public class TimeCapsuleMapOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Marker Settings")]
        [Tooltip("Prefab instantiated for each capsule location in the 3D world.")]
        [SerializeField] private GameObject capsuleMarkerPrefab;

        [Tooltip("Color applied to markers for Sealed capsules.")]
        [SerializeField] private Color sealedColor  = new Color(1.0f, 0.84f, 0.0f); // gold

        [Tooltip("Color applied to markers for Opened capsules.")]
        [SerializeField] private Color openedColor  = new Color(0.27f, 0.51f, 0.71f); // blue

        [Tooltip("Color applied to markers for Shared capsules.")]
        [SerializeField] private Color sharedColor  = new Color(0.18f, 0.65f, 0.33f); // green

        [Tooltip("Color applied to markers for Expired capsules.")]
        [SerializeField] private Color expiredColor = new Color(0.5f, 0.5f, 0.5f);   // grey

        [Header("Scale")]
        [Tooltip("World-space scale applied to each instantiated marker.")]
        [SerializeField] private float markerScale = 1f;

        [Header("References")]
        [Tooltip("Optional reference to the TimeCapsuleUI that handles capsule detail display.")]
        [SerializeField] private TimeCapsuleUI capsuleUI;

        // ── Internal state ────────────────────────────────────────────────────────
        /// <summary>Maps capsuleId → instantiated marker GameObject.</summary>
        private readonly Dictionary<string, GameObject> _activeMarkers = new Dictionary<string, GameObject>();
        /// <summary>Caches per-instance materials so ApplyStatusColor avoids repeated allocations.</summary>
        private readonly Dictionary<GameObject, Material> _markerMaterials = new Dictionary<GameObject, Material>();

        private bool _overlayActive = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (TimeCapsuleManager.Instance != null)
            {
                TimeCapsuleManager.Instance.OnCapsuleCreated += OnCapsuleChanged;
                TimeCapsuleManager.Instance.OnCapsuleOpened  += OnCapsuleChanged;
                TimeCapsuleManager.Instance.OnCapsuleShared  += OnCapsuleChanged;
            }
            RefreshMarkers();
        }

        private void OnDisable()
        {
            if (TimeCapsuleManager.Instance != null)
            {
                TimeCapsuleManager.Instance.OnCapsuleCreated -= OnCapsuleChanged;
                TimeCapsuleManager.Instance.OnCapsuleOpened  -= OnCapsuleChanged;
                TimeCapsuleManager.Instance.OnCapsuleShared  -= OnCapsuleChanged;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Synchronises the set of active markers with the capsule list held by
        /// <see cref="TimeCapsuleManager"/>. Creates markers for new capsules,
        /// updates colour for changed ones, and destroys markers for deleted ones.
        /// </summary>
        public void RefreshMarkers()
        {
            if (TimeCapsuleManager.Instance == null) return;

            var currentIds = new HashSet<string>();

            foreach (var capsule in TimeCapsuleManager.Instance.Capsules)
            {
                string id = capsule.capsuleId;
                currentIds.Add(id);

                if (!_activeMarkers.TryGetValue(id, out var marker))
                    marker = CreateMarker(capsule);

                if (marker != null)
                {
                    ApplyStatusColor(marker, capsule.status);
                    marker.SetActive(_overlayActive);
                }
            }

            // Remove markers for capsules that no longer exist.
            var toRemove = new List<string>();
            foreach (var kvp in _activeMarkers)
                if (!currentIds.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);

            foreach (var id in toRemove)
            {
                if (_activeMarkers.TryGetValue(id, out var stale))
                {
                    if (_markerMaterials.TryGetValue(stale, out var mat))
                    {
                        Destroy(mat);
                        _markerMaterials.Remove(stale);
                    }
                    Destroy(stale);
                }
                _activeMarkers.Remove(id);
            }
        }

        /// <summary>
        /// Called when the player interacts with (clicks/taps) a capsule marker in the world.
        /// Forwards the event to <see cref="TimeCapsuleUI.ShowCapsuleDetail"/>.
        /// </summary>
        /// <param name="capsuleId">GUID of the capsule whose marker was clicked.</param>
        public void OnMarkerClicked(string capsuleId)
        {
            if (capsuleUI != null)
                capsuleUI.ShowCapsuleDetail(capsuleId);
        }

        /// <summary>
        /// Enables or disables the entire overlay (all markers in the scene).
        /// </summary>
        /// <param name="active"><c>true</c> to show all markers; <c>false</c> to hide them.</param>
        public void SetOverlayActive(bool active)
        {
            _overlayActive = active;
            foreach (var marker in _activeMarkers.Values)
                if (marker) marker.SetActive(active);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private GameObject CreateMarker(TimeCapsule capsule)
        {
            if (capsuleMarkerPrefab == null)
            {
                Debug.LogWarning("[TimeCapsuleMapOverlay] capsuleMarkerPrefab is not assigned.");
                return null;
            }

            Vector3 worldPos = CapsuleLocationToWorldPosition(capsule.location);
            var marker = Instantiate(capsuleMarkerPrefab, worldPos, Quaternion.identity, transform);
            marker.name = $"CapsuleMarker_{capsule.capsuleId}";
            marker.transform.localScale = Vector3.one * markerScale;

            // Attach a click forwarder component.
            var forwarder = marker.AddComponent<TimeCapsuleMarkerClickForwarder>();
            forwarder.Init(capsule.capsuleId, this);

            _activeMarkers[capsule.capsuleId] = marker;
            return marker;
        }

        private void ApplyStatusColor(GameObject marker, TimeCapsuleStatus status)
        {
            var renderer = marker.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Color color = status switch
            {
                TimeCapsuleStatus.Sealed  => sealedColor,
                TimeCapsuleStatus.Opened  => openedColor,
                TimeCapsuleStatus.Shared  => sharedColor,
                TimeCapsuleStatus.Expired => expiredColor,
                _                         => sealedColor
            };

            // Cache the per-instance material to avoid repeated allocations and memory leaks.
            if (!_markerMaterials.TryGetValue(marker, out Material mat))
            {
                mat = new Material(renderer.sharedMaterial);
                renderer.material = mat;
                _markerMaterials[marker] = mat;
            }
            mat.color = color;
        }

        /// <summary>
        /// Converts a <see cref="CapsuleLocation"/> to a Unity world-space position.
        /// Override or extend this method to integrate with the project's world map.
        /// </summary>
        private static Vector3 CapsuleLocationToWorldPosition(CapsuleLocation location)
        {
            if (location == null) return Vector3.zero;
            // Placeholder: treat lat/lng as X/Z and altitude as Y (metres).
            return new Vector3(location.longitude, location.altitude, location.latitude);
        }

        private void OnCapsuleChanged(TimeCapsule _) => RefreshMarkers();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight component attached to each instantiated capsule marker.
    /// Forwards OnMouseDown events to <see cref="TimeCapsuleMapOverlay.OnMarkerClicked"/>.
    /// </summary>
    [AddComponentMenu("")]   // hide from Add Component menu
    internal class TimeCapsuleMarkerClickForwarder : MonoBehaviour
    {
        private string _capsuleId;
        private TimeCapsuleMapOverlay _overlay;

        /// <summary>Initialises this forwarder with the capsule ID and parent overlay.</summary>
        public void Init(string capsuleId, TimeCapsuleMapOverlay overlay)
        {
            _capsuleId = capsuleId;
            _overlay   = overlay;
        }

        private void OnMouseDown()
        {
            _overlay?.OnMarkerClicked(_capsuleId);
        }
    }
}
