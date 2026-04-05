// NPCTrafficHUD.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// In-flight traffic radar overlay and NPC info panel for the player HUD.
// Namespace: SWEF.NPCTraffic

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Displays a traffic radar blip overlay and nearest-NPC
    /// information strip in the player's HUD.
    /// Attach to a Canvas GameObject in the flight scene.
    /// </summary>
    public sealed class NPCTrafficHUD : MonoBehaviour
    {
        #region Inspector

        [Header("Radar")]
        [Tooltip("Root panel shown/hidden when toggling the radar.")]
        [SerializeField] private GameObject _radarPanel;

        [Tooltip("Transform of the radar sweep image (rotates each frame).")]
        [SerializeField] private RectTransform _radarSweep;

        [Tooltip("Prefab used to render a single traffic blip on the radar.")]
        [SerializeField] private RectTransform _blipPrefab;

        [Tooltip("Parent transform that holds instantiated blip objects.")]
        [SerializeField] private RectTransform _blipContainer;

        [Tooltip("Radius of the radar display in UI units.")]
        [SerializeField] private float _radarDisplayRadius = 150f;

        [Tooltip("Real-world range the radar covers in metres.")]
        [SerializeField] private float _radarRangeMetres = 50000f;

        [Header("Info Strip")]
        [Tooltip("Text label showing the nearest NPC callsign.")]
        [SerializeField] private Text _nearestCallsignLabel;

        [Tooltip("Text label showing the nearest NPC distance.")]
        [SerializeField] private Text _nearestDistanceLabel;

        [Tooltip("Text label showing the nearest NPC altitude.")]
        [SerializeField] private Text _nearestAltitudeLabel;

        [Tooltip("Text label showing the nearest NPC behaviour state.")]
        [SerializeField] private Text _nearestStateLabel;

        [Header("Radar Sweep")]
        [Tooltip("Degrees per second for radar sweep rotation.")]
        [SerializeField] private float _sweepDegreesPerSecond = 90f;

        #endregion

        #region Private State

        private readonly List<RectTransform> _blipPool = new List<RectTransform>();
        private Transform                    _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            UpdateRadarSweep();
            UpdateBlips();
            UpdateInfoStrip();
        }

        #endregion

        #region Public API

        /// <summary>Shows or hides the traffic radar panel.</summary>
        /// <param name="visible">Target visibility state.</param>
        public void SetRadarVisible(bool visible)
        {
            if (_radarPanel != null) _radarPanel.SetActive(visible);
        }

        /// <summary>Sets the player transform used as the radar origin.</summary>
        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        #endregion

        #region Private — Radar

        private void UpdateRadarSweep()
        {
            if (_radarSweep == null) return;
            _radarSweep.Rotate(Vector3.forward, -_sweepDegreesPerSecond * Time.deltaTime);
        }

        private void UpdateBlips()
        {
            if (NPCTrafficManager.Instance == null || _blipContainer == null || _blipPrefab == null)
                return;

            IReadOnlyList<NPCAircraftData> npcs = NPCTrafficManager.Instance.ActiveNPCs;

            // Grow pool as needed
            while (_blipPool.Count < npcs.Count)
            {
                RectTransform blip = Instantiate(_blipPrefab, _blipContainer);
                _blipPool.Add(blip);
            }

            // Hide all blips first
            foreach (RectTransform b in _blipPool) b.gameObject.SetActive(false);

            if (_playerTransform == null) return;

            for (int i = 0; i < npcs.Count; i++)
            {
                NPCAircraftData npc = npcs[i];
                Vector3 delta = npc.WorldPosition - _playerTransform.position;
                float   dist  = delta.magnitude;
                if (dist > _radarRangeMetres) continue;

                float ratio = dist / _radarRangeMetres;
                float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

                Vector2 blipPos = new Vector2(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * ratio * _radarDisplayRadius,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * ratio * _radarDisplayRadius);

                RectTransform blipRect = _blipPool[i];
                blipRect.anchoredPosition = blipPos;
                blipRect.gameObject.SetActive(true);

                // Colour by category
                Image img = blipRect.GetComponent<Image>();
                if (img != null) img.color = CategoryToBlipColor(npc.Category);
            }
        }

        #endregion

        #region Private — Info Strip

        private void UpdateInfoStrip()
        {
            if (NPCTrafficManager.Instance == null || _playerTransform == null) return;

            NPCAircraftData nearest = NPCTrafficManager.Instance.GetNearestNPC(
                _playerTransform.position, _radarRangeMetres);

            if (nearest == null)
            {
                if (_nearestCallsignLabel != null) _nearestCallsignLabel.text = "--";
                if (_nearestDistanceLabel != null) _nearestDistanceLabel.text = "--";
                if (_nearestAltitudeLabel != null) _nearestAltitudeLabel.text = "--";
                if (_nearestStateLabel    != null) _nearestStateLabel.text    = "--";
                return;
            }

            float distKm = Vector3.Distance(_playerTransform.position, nearest.WorldPosition) / 1000f;

            if (_nearestCallsignLabel != null) _nearestCallsignLabel.text = nearest.Callsign;
            if (_nearestDistanceLabel != null) _nearestDistanceLabel.text = $"{distKm:F1} km";
            if (_nearestAltitudeLabel != null) _nearestAltitudeLabel.text = $"{nearest.AltitudeMetres:F0} m";
            if (_nearestStateLabel    != null) _nearestStateLabel.text    = nearest.BehaviorState.ToString();
        }

        #endregion

        #region Private — Helpers

        private static Color CategoryToBlipColor(NPCAircraftCategory cat) =>
            cat switch
            {
                NPCAircraftCategory.CommercialAirline => Color.cyan,
                NPCAircraftCategory.PrivateJet        => Color.white,
                NPCAircraftCategory.CargoPlane        => new Color(1f, 0.65f, 0f),
                NPCAircraftCategory.MilitaryAircraft  => Color.red,
                NPCAircraftCategory.Helicopter        => Color.yellow,
                NPCAircraftCategory.TrainingAircraft  => Color.green,
                _                                    => Color.grey
            };

        #endregion
    }
}
