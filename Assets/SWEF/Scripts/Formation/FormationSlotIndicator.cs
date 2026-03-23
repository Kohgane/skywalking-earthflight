// FormationSlotIndicator.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// MonoBehaviour that renders a holographic slot marker at each
    /// formation slot position when the formation HUD is active.
    /// <para>
    /// One <see cref="FormationSlotIndicator"/> manages all slot visuals
    /// for the current <see cref="FormationManager"/>; it creates a pool
    /// of <see cref="MeshRenderer"/> quad proxies at runtime.
    /// </para>
    /// </summary>
    public sealed class FormationSlotIndicator : MonoBehaviour
    {
        #region Inspector

        [Header("Visibility")]
        [Tooltip("Master switch.  Disable to hide all slot markers.")]
        [SerializeField] private bool showIndicators = true;

        [Tooltip("Material applied to all slot marker quads " +
                 "(should be a holographic / ghost shader).")]
        [SerializeField] private Material slotMaterial;

        [Header("Colours")]
        [Tooltip("Colour used when a wingman is holding the slot.")]
        [SerializeField] private Color occupiedColor = Color.green;

        [Tooltip("Colour used when the slot has no assigned wingman.")]
        [SerializeField] private Color emptyColor = Color.yellow;

        [Header("Fade")]
        [Tooltip("Distance in metres beyond which slot indicators fade to transparent.")]
        [SerializeField] private float maxVisibleDistance = 200f;

        #endregion

        #region Runtime State

        private GameObject[]      _markerObjects;
        private MeshRenderer[]    _renderers;
        private MaterialPropertyBlock _mpb;

        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mpb = new MaterialPropertyBlock();
            RebuildMarkers();
        }

        private void Update()
        {
            if (!showIndicators)
            {
                SetAllActive(false);
                return;
            }

            FormationManager mgr = FormationManager.Instance;
            if (mgr == null || mgr.CurrentPattern == null)
            {
                SetAllActive(false);
                return;
            }

            int slotCount = mgr.wingmen.Count;
            EnsureCapacity(slotCount);

            Camera cam = Camera.main;

            for (int i = 0; i < _markerObjects.Length; i++)
            {
                if (i >= slotCount)
                {
                    _markerObjects[i].SetActive(false);
                    continue;
                }

                Vector3 slotPos = mgr.CurrentPattern.GetSlotWorldPosition(
                    i, mgr.transform);

                _markerObjects[i].SetActive(true);
                _markerObjects[i].transform.position = slotPos;
                _markerObjects[i].transform.rotation =
                    cam != null ? Quaternion.LookRotation(cam.transform.forward) : Quaternion.identity;

                // Determine occupied / empty colour.
                bool occupied  = mgr.wingmen[i] != null && mgr.wingmen[i].IsInFormation;
                Color baseColor = occupied ? occupiedColor : emptyColor;

                // Distance-based fade.
                float alpha = 1f;
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, slotPos);
                    alpha = Mathf.Clamp01(1f - (dist / Mathf.Max(maxVisibleDistance, 1f)));
                }

                Color final = new Color(baseColor.r, baseColor.g, baseColor.b,
                    baseColor.a * alpha);

                _renderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorProp, final);
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }

        private void OnDestroy()
        {
            DestroyMarkers();
        }

        #endregion

        #region Public API

        /// <summary>Toggles the visibility of all slot indicators.</summary>
        /// <param name="visible"><see langword="true"/> to show, <see langword="false"/> to hide.</param>
        public void SetVisible(bool visible)
        {
            showIndicators = visible;
        }

        #endregion

        #region Private Helpers

        private void RebuildMarkers()
        {
            DestroyMarkers();
            int cap = FormationConfig.MaxWingmen;
            _markerObjects = new GameObject[cap];
            _renderers     = new MeshRenderer[cap];

            for (int i = 0; i < cap; i++)
                CreateMarker(i);
        }

        private void CreateMarker(int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"SlotIndicator_{index}";
            go.transform.SetParent(transform, true);
            go.transform.localScale = Vector3.one * 3f;

            // Remove collider — these are purely visual.
            Destroy(go.GetComponent<Collider>());

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (slotMaterial != null)
                mr.material = slotMaterial;
            else
                mr.material = new Material(Shader.Find("Sprites/Default"));

            _markerObjects[index] = go;
            _renderers[index]     = mr;
            go.SetActive(false);
        }

        private void EnsureCapacity(int required)
        {
            if (_markerObjects != null && _markerObjects.Length >= required) return;
            RebuildMarkers();
        }

        private void DestroyMarkers()
        {
            if (_markerObjects == null) return;
            foreach (var go in _markerObjects)
                if (go != null) Destroy(go);
            _markerObjects = null;
            _renderers     = null;
        }

        private void SetAllActive(bool active)
        {
            if (_markerObjects == null) return;
            foreach (var go in _markerObjects)
                if (go != null) go.SetActive(active);
        }

        #endregion
    }
}
