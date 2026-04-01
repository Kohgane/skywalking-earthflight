// SurveyPOIManager.cs — SWEF Terrain Scanning & Geological Survey System
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Singleton that owns the discovered POI list.
    /// Deduplicates by proximity, persists to JSON, enforces a max cap with
    /// oldest-first eviction, and fires <see cref="OnPOIDiscovered"/> /
    /// <see cref="OnPOIRemoved"/> events.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    public class SurveyPOIManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SurveyPOIManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private TerrainSurveyConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new unique <see cref="SurveyPOI"/> is discovered.</summary>
        public event Action<SurveyPOI> OnPOIDiscovered;

        /// <summary>Fired when a POI is evicted from the list (oldest-first cap).</summary>
        public event Action<SurveyPOI> OnPOIRemoved;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<SurveyPOI> _pois = new List<SurveyPOI>();
        private string _persistencePath;

        // ── Defaults ──────────────────────────────────────────────────────────────
        private float ProximityThreshold => config != null ? config.proximityThreshold : 500f;
        private int   MaxPOIs            => config != null ? config.maxPOIs            : 500;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _persistencePath = Path.Combine(Application.persistentDataPath, "survey_pois.json");
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Save();
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates a <see cref="SurveySample"/> for POI creation.
        /// If no existing POI lies within <see cref="ProximityThreshold"/> metres,
        /// a new <see cref="SurveyPOI"/> is created and <see cref="OnPOIDiscovered"/> fired.
        /// </summary>
        public SurveyPOI DiscoverPOI(SurveySample sample)
        {
            if (IsDuplicate(sample.position)) return null;

            string locKey = GeologicalClassifier.GetFeatureDisplayName(sample.featureType);
            var poi       = new SurveyPOI(sample.position, sample.featureType, locKey);

            EnforceMaxCap();
            _pois.Add(poi);
            Save();

            OnPOIDiscovered?.Invoke(poi);
            return poi;
        }

        /// <summary>Returns a copy of all discovered POIs.</summary>
        public IReadOnlyList<SurveyPOI> GetAllPOIs() => _pois.AsReadOnly();

        /// <summary>Returns POIs filtered by <paramref name="type"/>.</summary>
        public IEnumerable<SurveyPOI> GetPOIsByFeature(GeologicalFeatureType type)
            => _pois.Where(p => p.featureType == type);

        /// <summary>Marks a POI as acknowledged (isNew → false) and persists.</summary>
        public void AcknowledgePOI(string id)
        {
            var poi = _pois.FirstOrDefault(p => p.id == id);
            if (poi == null) return;
            poi.isNew = false;
            Save();
        }

        /// <summary>Removes all POIs and persists the empty list.</summary>
        public void ClearAll()
        {
            _pois.Clear();
            Save();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private bool IsDuplicate(Vector3 position)
        {
            float threshold = ProximityThreshold;
            foreach (var poi in _pois)
            {
                if (Vector3.Distance(poi.position, position) < threshold)
                    return true;
            }
            return false;
        }

        private void EnforceMaxCap()
        {
            while (_pois.Count >= MaxPOIs)
            {
                // Evict the oldest POI (lowest timestamp)
                SurveyPOI oldest = _pois.OrderBy(p => p.discoveredTimestamp).First();
                _pois.Remove(oldest);
                OnPOIRemoved?.Invoke(oldest);
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        [Serializable]
        private class PoiListWrapper { public List<SurveyPOI> pois = new List<SurveyPOI>(); }

        private void Save()
        {
            try
            {
                var wrapper = new PoiListWrapper { pois = _pois };
                string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
                File.WriteAllText(_persistencePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SurveyPOIManager] Save failed: {ex.Message}");
            }
        }

        private void Load()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json    = File.ReadAllText(_persistencePath);
                var wrapper    = JsonUtility.FromJson<PoiListWrapper>(json);
                if (wrapper?.pois != null)
                {
                    _pois.Clear();
                    _pois.AddRange(wrapper.pois);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SurveyPOIManager] Load failed: {ex.Message}");
            }
        }
    }
}
