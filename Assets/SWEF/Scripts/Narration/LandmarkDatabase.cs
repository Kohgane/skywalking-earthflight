using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Narration
{
    /// <summary>
    /// ScriptableObject that stores all <see cref="LandmarkData"/> records and the
    /// corresponding <see cref="NarrationScript"/> entries for each landmark.
    /// Provides efficient spatial lookups using a degree-grid bucketing scheme.
    /// Can be extended at runtime via <see cref="LoadFromJson"/> or <see cref="MergeDatabase"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Narration/Landmark Database", fileName = "LandmarkDatabase")]
    public class LandmarkDatabase : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Tooltip("All landmark definitions. Populated from embedded data or JSON at runtime.")]
        [SerializeField] private List<LandmarkData> landmarks = new List<LandmarkData>();

        [Tooltip("All narration scripts. Linked to landmarks via landmarkId.")]
        [SerializeField] private List<NarrationScript> narrationScripts = new List<NarrationScript>();

        // ── Spatial grid ──────────────────────────────────────────────────────────
        // Grid bucket size in decimal degrees (≈ 111 km per degree lat).
        private const float GridDegreeBucket = 1f;

        private Dictionary<Vector2Int, List<LandmarkData>> _spatialGrid;
        private Dictionary<string, LandmarkData>           _byId;
        private Dictionary<string, List<NarrationScript>>  _scriptsByLandmark;
        private bool _initialised;

        // ── Statistics ────────────────────────────────────────────────────────────
        /// <summary>Total number of landmarks in this database.</summary>
        public int TotalLandmarks => landmarks?.Count ?? 0;

        /// <summary>Count of landmarks broken down by category.</summary>
        public Dictionary<LandmarkCategory, int> LandmarksByCategory
        {
            get
            {
                var dict = new Dictionary<LandmarkCategory, int>();
                if (landmarks == null) return dict;
                foreach (var lm in landmarks)
                {
                    dict.TryGetValue(lm.category, out int cnt);
                    dict[lm.category] = cnt + 1;
                }
                return dict;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (landmarks == null)  landmarks        = new List<LandmarkData>();
            if (narrationScripts == null) narrationScripts = new List<NarrationScript>();

            // Seed built-in data if the asset is empty (first run or freshly created).
            if (landmarks.Count == 0)
                PopulateBuiltInData();

            Rebuild();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns landmarks whose GPS position is within <paramref name="radiusKm"/> km.</summary>
        public List<LandmarkData> GetLandmarksNear(double lat, double lon, float radiusKm)
        {
            EnsureInitialised();
            var results = new List<LandmarkData>();
            double radiusDeg = radiusKm / 111.0;

            // Determine the grid cells that overlap the search disc.
            int minLatCell = Mathf.FloorToInt((float)(lat - radiusDeg) / GridDegreeBucket);
            int maxLatCell = Mathf.FloorToInt((float)(lat + radiusDeg) / GridDegreeBucket);
            int minLonCell = Mathf.FloorToInt((float)(lon - radiusDeg) / GridDegreeBucket);
            int maxLonCell = Mathf.FloorToInt((float)(lon + radiusDeg) / GridDegreeBucket);

            double radiusSqKm = (double)radiusKm * radiusKm;

            for (int la = minLatCell; la <= maxLatCell; la++)
            {
                for (int lo = minLonCell; lo <= maxLonCell; lo++)
                {
                    var cell = new Vector2Int(la, lo);
                    if (!_spatialGrid.TryGetValue(cell, out var bucket)) continue;
                    foreach (var lm in bucket)
                    {
                        if (HaversineDistanceSqKm(lat, lon, lm.latitude, lm.longitude) <= radiusSqKm)
                            results.Add(lm);
                    }
                }
            }
            return results;
        }

        /// <summary>Returns a landmark by its unique ID, or null if not found.</summary>
        public LandmarkData GetLandmarkById(string id)
        {
            EnsureInitialised();
            if (string.IsNullOrEmpty(id)) return null;
            _byId.TryGetValue(id, out var lm);
            return lm;
        }

        /// <summary>Returns all landmarks matching the given category.</summary>
        public List<LandmarkData> GetLandmarksByCategory(LandmarkCategory category)
        {
            EnsureInitialised();
            return landmarks.Where(l => l.category == category).ToList();
        }

        /// <summary>Returns all landmarks in the given country (case-insensitive).</summary>
        public List<LandmarkData> GetLandmarksByCountry(string country)
        {
            EnsureInitialised();
            if (string.IsNullOrEmpty(country)) return new List<LandmarkData>();
            return landmarks.Where(l =>
                string.Equals(l.country, country, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>Returns the single closest landmark to the given GPS position.</summary>
        public LandmarkData GetNearestLandmark(double lat, double lon)
        {
            EnsureInitialised();
            LandmarkData nearest = null;
            double minSq = double.MaxValue;
            foreach (var lm in landmarks)
            {
                double sq = HaversineDistanceSqKm(lat, lon, lm.latitude, lm.longitude);
                if (sq < minSq) { minSq = sq; nearest = lm; }
            }
            return nearest;
        }

        /// <summary>
        /// Returns all <see cref="NarrationScript"/> entries for the given landmark ID.
        /// </summary>
        public List<NarrationScript> GetScriptsForLandmark(string landmarkId)
        {
            EnsureInitialised();
            if (string.IsNullOrEmpty(landmarkId)) return new List<NarrationScript>();
            _scriptsByLandmark.TryGetValue(landmarkId, out var scripts);
            return scripts ?? new List<NarrationScript>();
        }

        /// <summary>
        /// Returns the best-matching <see cref="NarrationScript"/> for a landmark in the
        /// requested language, falling back to "en" if the exact language is unavailable.
        /// </summary>
        public NarrationScript GetBestScript(string landmarkId, string languageCode)
        {
            var scripts = GetScriptsForLandmark(landmarkId);
            if (scripts.Count == 0) return null;
            return scripts.FirstOrDefault(s => s.languageCode == languageCode)
                ?? scripts.FirstOrDefault(s => s.languageCode == "en")
                ?? scripts[0];
        }

        /// <summary>Merges all landmarks from <paramref name="json"/> into this database.</summary>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<LandmarkDataWrapper>(json);
                if (wrapper?.landmarks != null)
                    foreach (var lm in wrapper.landmarks)
                        AddLandmark(lm);
                if (wrapper?.scripts != null)
                    foreach (var s in wrapper.scripts)
                        AddScript(s);
                Rebuild();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] LandmarkDatabase.LoadFromJson: {e.Message}");
            }
        }

        /// <summary>Merges all landmarks from another database into this one.</summary>
        public void MergeDatabase(LandmarkDatabase other)
        {
            if (other == null) return;
            foreach (var lm in other.landmarks)   AddLandmark(lm);
            foreach (var s  in other.narrationScripts) AddScript(s);
            Rebuild();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void AddLandmark(LandmarkData lm)
        {
            if (lm == null || string.IsNullOrEmpty(lm.landmarkId)) return;
            if (landmarks.Any(x => x.landmarkId == lm.landmarkId)) return;
            landmarks.Add(lm);
        }

        private void AddScript(NarrationScript s)
        {
            if (s == null || string.IsNullOrEmpty(s.scriptId)) return;
            if (narrationScripts.Any(x => x.scriptId == s.scriptId)) return;
            narrationScripts.Add(s);
        }

        private void EnsureInitialised()
        {
            if (!_initialised) Rebuild();
        }

        private void Rebuild()
        {
            _byId              = new Dictionary<string, LandmarkData>();
            _spatialGrid       = new Dictionary<Vector2Int, List<LandmarkData>>();
            _scriptsByLandmark = new Dictionary<string, List<NarrationScript>>();

            foreach (var lm in landmarks)
            {
                if (lm == null || string.IsNullOrEmpty(lm.landmarkId)) continue;
                _byId[lm.landmarkId] = lm;

                var cell = LatLonToCell(lm.latitude, lm.longitude);
                if (!_spatialGrid.TryGetValue(cell, out var bucket))
                    _spatialGrid[cell] = bucket = new List<LandmarkData>();
                bucket.Add(lm);
            }

            foreach (var s in narrationScripts)
            {
                if (s == null || string.IsNullOrEmpty(s.landmarkId)) continue;
                if (!_scriptsByLandmark.TryGetValue(s.landmarkId, out var list))
                    _scriptsByLandmark[s.landmarkId] = list = new List<NarrationScript>();
                list.Add(s);
            }

            _initialised = true;
            Debug.Log($"[SWEF] LandmarkDatabase rebuilt: {landmarks.Count} landmarks, {narrationScripts.Count} scripts.");
        }

        private static Vector2Int LatLonToCell(double lat, double lon)
        {
            return new Vector2Int(
                Mathf.FloorToInt((float)lat / GridDegreeBucket),
                Mathf.FloorToInt((float)lon / GridDegreeBucket));
        }

        /// <summary>Squared Haversine distance in km² between two GPS points.</summary>
        private static double HaversineDistanceSqKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;
            return d * d;
        }

        // ── JSON serialisation wrapper ─────────────────────────────────────────────
        [Serializable]
        private class LandmarkDataWrapper
        {
            public List<LandmarkData>    landmarks = new List<LandmarkData>();
            public List<NarrationScript> scripts   = new List<NarrationScript>();
        }

        // ── Built-in landmark data (30 real-world landmarks) ──────────────────────

        private void PopulateBuiltInData()
        {
            landmarks.AddRange(new[]
            {
                // ── Europe ────────────────────────────────────────────────────────
                Make("lm_eiffel_tower",   "Eiffel Tower",       48.8584,   2.2945,  35f, LandmarkCategory.Architectural, "France",  "Paris",    "Île-de-France",  1889, "Gustave Eiffel",   true,  2000f, NarrationTriggerType.Proximity, NarrationPriority.High,    "iron_lattice_tower",  new[]{"paris","france","iron","tower"}),
                Make("lm_colosseum",      "Colosseum",          41.8902,  12.4922,  21f, LandmarkCategory.Historical,    "Italy",   "Rome",     "Lazio",          72,   "Vespasian",         true,  1500f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_historical", new[]{"rome","italy","roman","amphitheatre"}),
                Make("lm_sagrada_familia","Sagrada Família",    41.4036,   2.1744,   0f, LandmarkCategory.Architectural, "Spain",   "Barcelona","Catalonia",       1882, "Antoni Gaudí",      true,  1000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_religious",  new[]{"spain","church","gaudi","modernist"}),
                Make("lm_acropolis",      "Acropolis of Athens",37.9715,  23.7257,  156f,LandmarkCategory.Archaeological,"Greece",  "Athens",   "Attica",         -432, "Ictinus",           true,  1200f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_historical", new[]{"greece","athens","ancient","parthenon"}),
                Make("lm_stonehenge",     "Stonehenge",         51.1789,  -1.8262,  102f,LandmarkCategory.Archaeological,"UK",      "Salisbury","Wiltshire",       -3000,"Unknown",          true,  800f,  NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_historical", new[]{"uk","england","neolithic","mystery"}),

                // ── Asia ──────────────────────────────────────────────────────────
                Make("lm_great_wall",     "Great Wall of China",40.4319, 116.5704,  500f,LandmarkCategory.Historical,   "China",   "Beijing",  "Beijing",         -221, "Qin Shi Huang",     true,  3000f, NarrationTriggerType.FlyOver,   NarrationPriority.Critical,"landmark_historical", new[]{"china","wall","dynasty","military"}),
                Make("lm_taj_mahal",      "Taj Mahal",          27.1751,  78.0421,   93f,LandmarkCategory.Architectural,"India",   "Agra",     "Uttar Pradesh",   1632, "Ustad Ahmad Lahauri",true, 1500f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_cultural",   new[]{"india","mughal","mausoleum","marble"}),
                Make("lm_angkor_wat",     "Angkor Wat",         13.4125, 103.8670,    0f,LandmarkCategory.Religious,    "Cambodia","Siem Reap","Siem Reap",        1150, "Suryavarman II",    true,  2000f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_religious",  new[]{"cambodia","khmer","temple","hindu"}),
                Make("lm_mount_fuji",     "Mount Fuji",         35.3606, 138.7274, 3776f,LandmarkCategory.Natural,      "Japan",   "Fujinomiya","Shizuoka",        0,   "Natural",                  true,  5000f, NarrationTriggerType.FlyOver,   NarrationPriority.Normal,  "landmark_natural",    new[]{"japan","volcano","mountain","iconic"}), 
                Make("lm_petra",          "Petra",              30.3285,  35.4444,   750f,LandmarkCategory.Archaeological,"Jordan",  "Ma'an",   "Ma'an",          -312, "Nabataean",         true,  1000f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_historical", new[]{"jordan","nabataean","rock","city"}),
                Make("lm_borobudur",      "Borobudur",          -7.6079, 110.2038,   235f,LandmarkCategory.Religious,   "Indonesia","Magelang","Central Java",    800,  "Gunadharma",        true,  1000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_religious",  new[]{"indonesia","buddhist","java","stupa"}),

                // ── Africa ────────────────────────────────────────────────────────
                Make("lm_great_pyramid",  "Great Pyramid of Giza",29.9792,31.1342,   60f,LandmarkCategory.Archaeological,"Egypt",   "Giza",    "Giza",           -2560,"Hemiunu",           true,  3000f, NarrationTriggerType.FlyOver,   NarrationPriority.Critical,"landmark_historical", new[]{"egypt","pyramid","pharaoh","ancient"}),
                Make("lm_victoria_falls", "Victoria Falls",     -17.9243,  25.8572, 850f,LandmarkCategory.Natural,      "Zimbabwe","Livingstone","Matabeleland",  0,   "Natural",         true,  3000f, NarrationTriggerType.FlyOver,   NarrationPriority.High,    "landmark_natural",    new[]{"africa","waterfall","zambezi","wonder"}),
                Make("lm_kilimanjaro",    "Mount Kilimanjaro",   -3.0674,  37.3556,5895f,LandmarkCategory.Natural,      "Tanzania","Moshi",   "Kilimanjaro",      0,   "Natural",                  true,  8000f, NarrationTriggerType.FlyOver,   NarrationPriority.Normal,  "landmark_natural",    new[]{"africa","mountain","volcano","highest"}), 

                // ── Americas ──────────────────────────────────────────────────────
                Make("lm_grand_canyon",   "Grand Canyon",       36.1069,-112.1126, 2100f,LandmarkCategory.Geological,   "USA",     "Grand Canyon Village","Arizona",0, "Natural",           true,  8000f, NarrationTriggerType.FlyOver,   NarrationPriority.High,    "landmark_natural",    new[]{"usa","canyon","colorado","gorge"}),
                Make("lm_machu_picchu",   "Machu Picchu",      -13.1631, -72.5450, 2430f,LandmarkCategory.Archaeological,"Peru",    "Aguas Calientes","Cusco",    1450, "Pachacuti",         true,  1500f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_historical", new[]{"peru","inca","ruins","andes"}),
                Make("lm_statue_liberty", "Statue of Liberty",  40.6892, -74.0445,  10f, LandmarkCategory.Artistic,     "USA",     "New York","New York",         1886, "Frédéric Bartholdi",true, 1000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_cultural",   new[]{"usa","newyork","icon","freedom"}),
                Make("lm_chichen_itza",   "Chichén Itzá",       20.6843, -88.5678,  26f, LandmarkCategory.Archaeological,"Mexico",  "Yucatán","Yucatán",          600,  "Maya",              true,  1000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_historical", new[]{"mexico","maya","pyramid","calendar"}),
                Make("lm_amazon_river",   "Amazon River Source",-15.5047, -70.0684,4800f,LandmarkCategory.Natural,      "Peru",    "Puno",    "Puno",             0,   "Natural",         false, 5000f, NarrationTriggerType.FlyOver,   NarrationPriority.Low,     "landmark_natural",    new[]{"amazon","peru","river","source"}),
                Make("lm_iguazu_falls",   "Iguaçu Falls",      -25.6953, -54.4367,  200f,LandmarkCategory.Natural,      "Brazil",  "Foz do Iguaçu","Paraná",       0,   "Natural",            true,  3000f, NarrationTriggerType.FlyOver,   NarrationPriority.Normal,  "landmark_natural",    new[]{"brazil","argentina","waterfall","falls"}), 

                // ── Oceania ───────────────────────────────────────────────────────
                Make("lm_sydney_opera",   "Sydney Opera House", -33.8568, 151.2153,   5f, LandmarkCategory.Artistic,    "Australia","Sydney","New South Wales",   1973, "Jørn Utzon",        true,  1500f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_modern",     new[]{"australia","sydney","opera","icon"}),
                Make("lm_uluru",          "Uluru (Ayers Rock)", -25.3444, 131.0369,  863f,LandmarkCategory.Natural,     "Australia","Alice Springs","Northern Territory",0,"Natural",        true,  3000f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_natural",    new[]{"australia","sacred","monolith","indigenous"}),
                Make("lm_great_barrier_reef","Great Barrier Reef",-18.2871,147.6992,  0f, LandmarkCategory.Natural,     "Australia","Cairns","Queensland",          0,   "Natural",            true,  10000f,NarrationTriggerType.FlyOver,   NarrationPriority.Normal,  "landmark_natural",    new[]{"australia","reef","coral","marine"}), 

                // ── Modern icons ──────────────────────────────────────────────────
                Make("lm_burj_khalifa",   "Burj Khalifa",       25.1972,  55.2744,    0f, LandmarkCategory.Modern,      "UAE",     "Dubai","Dubai",              2010, "Adrian Smith",      false, 2000f, NarrationTriggerType.Proximity, NarrationPriority.High,    "landmark_modern",     new[]{"dubai","uae","skyscraper","tallest"}),
                Make("lm_golden_gate",    "Golden Gate Bridge",  37.8199,-122.4783,   67f,LandmarkCategory.Modern,      "USA",     "San Francisco","California",   1937, "Joseph Strauss",    false, 2000f, NarrationTriggerType.FlyThrough,NarrationPriority.Normal,  "landmark_modern",     new[]{"usa","sanfrancisco","bridge","suspension"}),
                Make("lm_ayers_rock",     "Kata Tjuta (Olgas)", -25.3521, 130.7356,  546f,LandmarkCategory.Natural,     "Australia","Alice Springs","Northern Territory",0,"Natural",        true,  4000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_natural",    new[]{"australia","rocks","sacred","domes"}),
                Make("lm_aurora_borealis","Aurora viewing site", 69.6796,  18.9570,    0f, LandmarkCategory.Natural,    "Norway",  "Tromsø","Troms",               0,   "Natural",            false, 20000f,NarrationTriggerType.TimeOfDay, NarrationPriority.Normal,  "landmark_natural",    new[]{"norway","aurora","northern_lights","night"}), 
                Make("lm_venice_canals",  "Venice Grand Canal",  45.4408,  12.3155,    0f, LandmarkCategory.Cultural,   "Italy",   "Venice","Veneto",              697, "Various",                  true,  1500f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_cultural",   new[]{"italy","venice","canal","renaissance"}),
                Make("lm_hagia_sophia",   "Hagia Sophia",        41.0086,  28.9802,   35f, LandmarkCategory.Religious,  "Turkey",  "Istanbul","Istanbul",          537, "Anthemius of Tralles",true,1000f, NarrationTriggerType.Proximity, NarrationPriority.Normal,  "landmark_religious",  new[]{"turkey","istanbul","byzantine","mosque"}),
                Make("lm_mount_everest",  "Mount Everest",       27.9881,  86.9250, 8849f,LandmarkCategory.Natural,     "Nepal",   "Solukhumbu","Sagarmatha",       0,   "Natural",            true,  15000f,NarrationTriggerType.FlyOver,   NarrationPriority.Critical,"landmark_natural",    new[]{"nepal","everest","himalaya","summit"}), 
            });

            // Generate English narration scripts for each landmark.
            foreach (var lm in landmarks)
                narrationScripts.Add(MakeScript(lm));
        }

        // ── Factory helpers ───────────────────────────────────────────────────────

        private static LandmarkData Make(
            string id, string name,
            double lat, double lon, float alt,
            LandmarkCategory cat,
            string country, string city, string region,
            int year, string architect,
            bool unesco,
            float triggerRadius,
            NarrationTriggerType triggerType,
            NarrationPriority priority,
            string iconType,
            string[] tags)
        {
            return new LandmarkData
            {
                landmarkId         = id,
                name               = name,
                localizedNameKey   = id + "_name",
                latitude           = lat,
                longitude          = lon,
                altitude           = alt,
                category           = cat,
                subcategory        = string.Empty,
                country            = country,
                city               = city,
                region             = region,
                yearBuilt          = year,
                architect          = architect,
                unescoWorldHeritage = unesco,
                triggerRadius      = triggerRadius,
                triggerType        = triggerType,
                priority           = priority,
                iconType           = iconType,
                tags               = new List<string>(tags),
                relatedLandmarkIds = new List<string>()
            };
        }

        private static NarrationScript MakeScript(LandmarkData lm)
        {
            string sid = lm.landmarkId + "_en";
            return new NarrationScript
            {
                scriptId     = sid,
                landmarkId   = lm.landmarkId,
                languageCode = "en",
                title        = lm.name,
                subtitle     = $"Discover {lm.name}",
                totalDuration = 30f,
                hasAudio     = false,
                audioClipPath = string.Empty,
                funFacts     = new List<string> { lm.landmarkId + "_fact1" },
                sources      = new List<string>(),
                segments     = new List<NarrationSegment>
                {
                    new NarrationSegment
                    {
                        segmentIndex     = 0,
                        text             = lm.landmarkId + "_seg0",
                        startTime        = 0f,
                        endTime          = 15f,
                        highlightKeywords = new List<string>(),
                        suggestedCameraAngle = Vector3.zero,
                        relatedImagePath  = string.Empty
                    },
                    new NarrationSegment
                    {
                        segmentIndex     = 1,
                        text             = lm.landmarkId + "_seg1",
                        startTime        = 15f,
                        endTime          = 30f,
                        highlightKeywords = new List<string>(),
                        suggestedCameraAngle = Vector3.zero,
                        relatedImagePath  = string.Empty
                    }
                }
            };
        }
    }
}
