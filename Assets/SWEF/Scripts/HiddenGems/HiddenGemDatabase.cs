using System.Collections.Generic;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Static database of 50+ real-world hidden-gem locations with accurate GPS coordinates.
    /// Each continent has at least 7 entries (Antarctica has 3).
    /// </summary>
    public static class HiddenGemDatabase
    {
        // ── Helper ────────────────────────────────────────────────────────────────

        private static HiddenGemDefinition Make(
            string id, GemCategory cat, GemRarity rarity, GemContinent cont,
            string country, double lat, double lon, float altHint,
            bool hint = true, string unlock = "")
        {
            return new HiddenGemDefinition
            {
                gemId                 = id,
                nameKey               = id + "_name",
                descriptionKey        = id + "_desc",
                factKey               = id + "_fact",
                category              = cat,
                rarity                = rarity,
                continent             = cont,
                country               = country,
                latitude              = lat,
                longitude             = lon,
                altitudeHint          = altHint,
                discoveryRadiusMeters = HiddenGemDefinition.DefaultRadius(rarity),
                xpReward              = HiddenGemDefinition.DefaultXP(rarity),
                iconOverride          = "",
                isHintVisible         = hint,
                unlockRequirement     = unlock
            };
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the complete list of all hidden gem definitions.</summary>
        public static List<HiddenGemDefinition> GetAllGems()
        {
            var gems = new List<HiddenGemDefinition>();
            gems.AddRange(GetAsiaGems());
            gems.AddRange(GetEuropeGems());
            gems.AddRange(GetNorthAmericaGems());
            gems.AddRange(GetSouthAmericaGems());
            gems.AddRange(GetAfricaGems());
            gems.AddRange(GetOceaniaGems());
            gems.AddRange(GetAntarcticaGems());
            return gems;
        }

        // ── Asia (10) ─────────────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetAsiaGems()
        {
            // Hang Sơn Đoòng — world's largest cave, Vietnam
            yield return Make("gem_son_doong_cave", GemCategory.UndergroundCave, GemRarity.Legendary,
                GemContinent.Asia, "Vietnam", 17.4521, 106.2873, 150f);

            // Son Doong exit light beam — specific cave opening, Vietnam
            yield return Make("gem_son_doong_light_beam", GemCategory.UndergroundCave, GemRarity.Legendary,
                GemContinent.Asia, "Vietnam", 17.4633, 106.2895, 80f, true, "discover_1_asia");

            // Zhangye Danxia Rainbow Mountains, China
            yield return Make("gem_zhangye_danxia", GemCategory.GeologicalFormation, GemRarity.Epic,
                GemContinent.Asia, "China", 38.9384, 100.0988, 2000f);

            // Socotra Island Dragon Blood Trees, Yemen
            yield return Make("gem_socotra_dragon_trees", GemCategory.NaturalWonder, GemRarity.Legendary,
                GemContinent.Asia, "Yemen", 12.4634, 53.8237, 400f);

            // Aogashima volcanic island, Japan
            yield return Make("gem_aogashima_volcano", GemCategory.VolcanicFormation, GemRarity.Epic,
                GemContinent.Asia, "Japan", 32.4574, 139.7691, 600f);

            // Cat Ba Island hidden lagoons, Vietnam
            yield return Make("gem_cat_ba_lagoons", GemCategory.SecretBeach, GemRarity.Uncommon,
                GemContinent.Asia, "Vietnam", 20.7841, 107.0848, 300f);

            // Jiuzhaigou Valley turquoise lakes, China
            yield return Make("gem_jiuzhaigou_valley", GemCategory.NaturalWonder, GemRarity.Rare,
                GemContinent.Asia, "China", 33.2600, 103.9170, 2500f);

            // Bagan Temples at sunrise, Myanmar
            yield return Make("gem_bagan_temples", GemCategory.ForgottenTemple, GemRarity.Rare,
                GemContinent.Asia, "Myanmar", 21.1717, 94.8585, 500f);

            // Preah Vihear cliff temple, Cambodia
            yield return Make("gem_preah_vihear", GemCategory.ForgottenTemple, GemRarity.Rare,
                GemContinent.Asia, "Cambodia", 14.3924, 104.6804, 700f);

            // Sigiriya Lion Rock fortress, Sri Lanka
            yield return Make("gem_sigiriya_lion_rock", GemCategory.AncientRuin, GemRarity.Epic,
                GemContinent.Asia, "Sri Lanka", 7.9570, 80.7603, 400f);
        }

        // ── Europe (10) ───────────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetEuropeGems()
        {
            // Trolltunga tongue-shaped rock, Norway
            yield return Make("gem_trolltunga", GemCategory.GeologicalFormation, GemRarity.Rare,
                GemContinent.Europe, "Norway", 60.1240, 6.7390, 1100f);

            // Plitvice Lakes underground falls, Croatia
            yield return Make("gem_plitvice_underground", GemCategory.HiddenWaterfall, GemRarity.Uncommon,
                GemContinent.Europe, "Croatia", 44.8804, 15.6166, 400f);

            // Meteora monasteries on rock pillars, Greece
            yield return Make("gem_meteora_monasteries", GemCategory.SacredSite, GemRarity.Rare,
                GemContinent.Europe, "Greece", 39.7217, 21.6306, 700f);

            // Drangarnir sea-stack arch, Faroe Islands, Denmark
            yield return Make("gem_drangarnir_arch", GemCategory.NaturalArch, GemRarity.Epic,
                GemContinent.Europe, "Denmark (Faroe Islands)", 62.0933, -7.1676, 250f);

            // Lauterbrunnen Valley 72 waterfalls, Switzerland
            yield return Make("gem_lauterbrunnen_valley", GemCategory.HiddenWaterfall, GemRarity.Uncommon,
                GemContinent.Europe, "Switzerland", 46.5938, 7.9089, 1200f);

            // Skógafoss hidden cave behind waterfall, Iceland
            yield return Make("gem_skogafoss_cave", GemCategory.HiddenWaterfall, GemRarity.Rare,
                GemContinent.Europe, "Iceland", 63.5322, -19.5118, 200f);

            // Caminito del Rey gorge, Spain
            yield return Make("gem_caminito_del_rey", GemCategory.GeologicalFormation, GemRarity.Uncommon,
                GemContinent.Europe, "Spain", 36.9351, -4.8055, 600f);

            // Turda Salt Mine underground cathedral, Romania
            yield return Make("gem_turda_salt_mine", GemCategory.UndergroundCave, GemRarity.Epic,
                GemContinent.Europe, "Romania", 46.5768, 23.7842, 50f);

            // Externsteine sandstone pillars, Germany
            yield return Make("gem_externsteine", GemCategory.GeologicalFormation, GemRarity.Uncommon,
                GemContinent.Europe, "Germany", 51.8671, 8.9189, 300f);

            // Blue Grotto, Capri, Italy
            yield return Make("gem_blue_grotto_capri", GemCategory.UndergroundCave, GemRarity.Rare,
                GemContinent.Europe, "Italy", 40.5612, 14.2167, 100f);
        }

        // ── North America (8) ─────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetNorthAmericaGems()
        {
            // Antelope Canyon light beams, Arizona, USA
            yield return Make("gem_antelope_canyon", GemCategory.UndergroundCave, GemRarity.Rare,
                GemContinent.NorthAmerica, "USA", 36.8619, -111.3743, 100f);

            // Fly Geyser, Nevada, USA
            yield return Make("gem_fly_geyser", GemCategory.VolcanicFormation, GemRarity.Epic,
                GemContinent.NorthAmerica, "USA", 40.8596, -119.3317, 400f);

            // Cenote Ik Kil, Mexico
            yield return Make("gem_cenote_ik_kil", GemCategory.UndergroundCave, GemRarity.Rare,
                GemContinent.NorthAmerica, "Mexico", 20.6761, -88.5734, 50f);

            // Hamilton Pool Preserve, Texas, USA
            yield return Make("gem_hamilton_pool", GemCategory.HiddenWaterfall, GemRarity.Uncommon,
                GemContinent.NorthAmerica, "USA", 30.3418, -98.1265, 200f);

            // Marble Canyon, British Columbia, Canada
            yield return Make("gem_marble_canyon_bc", GemCategory.GeologicalFormation, GemRarity.Rare,
                GemContinent.NorthAmerica, "Canada", 50.7879, -121.1024, 1000f);

            // Bioluminescent Bay (Mosquito Bay), Puerto Rico
            yield return Make("gem_mosquito_bay", GemCategory.NaturalWonder, GemRarity.Epic,
                GemContinent.NorthAmerica, "Puerto Rico", 18.0921, -65.4448, 50f);

            // Na Pali Coast sea caves, Hawaii, USA
            yield return Make("gem_na_pali_caves", GemCategory.UndergroundCave, GemRarity.Legendary,
                GemContinent.NorthAmerica, "USA", 22.1941, -159.6438, 100f);

            // Havasu Falls, Grand Canyon, USA
            yield return Make("gem_havasu_falls", GemCategory.HiddenWaterfall, GemRarity.Rare,
                GemContinent.NorthAmerica, "USA", 36.2553, -112.6979, 600f);
        }

        // ── South America (8) ─────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetSouthAmericaGems()
        {
            // Mount Roraima tepui summit, Venezuela
            yield return Make("gem_mount_roraima", GemCategory.NaturalWonder, GemRarity.Legendary,
                GemContinent.SouthAmerica, "Venezuela", 5.1438, -60.7625, 2900f);

            // Marble Caves, Chile
            yield return Make("gem_marble_caves_chile", GemCategory.NaturalArch, GemRarity.Epic,
                GemContinent.SouthAmerica, "Chile", -46.6370, -72.6868, 200f);

            // Rainbow Mountain Vinicunca, Peru
            yield return Make("gem_vinicunca_rainbow", GemCategory.GeologicalFormation, GemRarity.Rare,
                GemContinent.SouthAmerica, "Peru", -13.8047, -71.3342, 5200f);

            // Lençóis Maranhenses lagoons, Brazil
            yield return Make("gem_lencois_maranhenses", GemCategory.NaturalWonder, GemRarity.Epic,
                GemContinent.SouthAmerica, "Brazil", -2.4885, -43.1234, 50f);

            // Perito Moreno ice tunnels, Argentina
            yield return Make("gem_perito_moreno_tunnels", GemCategory.IceFormation, GemRarity.Rare,
                GemContinent.SouthAmerica, "Argentina", -50.4957, -73.0455, 300f);

            // Ciudad Perdida, Colombia
            yield return Make("gem_ciudad_perdida", GemCategory.AncientRuin, GemRarity.Legendary,
                GemContinent.SouthAmerica, "Colombia", 11.0384, -73.9249, 1000f);

            // Salar de Uyuni mirror effect, Bolivia
            yield return Make("gem_salar_de_uyuni", GemCategory.NaturalWonder, GemRarity.Rare,
                GemContinent.SouthAmerica, "Bolivia", -20.1338, -67.4891, 3700f);

            // Chapada Diamantina blue cave, Brazil
            yield return Make("gem_chapada_blue_cave", GemCategory.UndergroundCave, GemRarity.Uncommon,
                GemContinent.SouthAmerica, "Brazil", -12.4556, -41.4450, 400f);
        }

        // ── Africa (8) ────────────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetAfricaGems()
        {
            // Deadvlei clay pan, Namibia
            yield return Make("gem_deadvlei", GemCategory.NaturalWonder, GemRarity.Epic,
                GemContinent.Africa, "Namibia", -24.7574, 15.2929, 800f);

            // Tsingy de Bemaraha stone forest, Madagascar
            yield return Make("gem_tsingy_bemaraha", GemCategory.GeologicalFormation, GemRarity.Legendary,
                GemContinent.Africa, "Madagascar", -19.0340, 44.7960, 300f);

            // Lake Natron, Tanzania
            yield return Make("gem_lake_natron", GemCategory.NaturalWonder, GemRarity.Rare,
                GemContinent.Africa, "Tanzania", -2.4089, 36.0521, 700f);

            // Danakil Depression sulfur springs, Ethiopia
            yield return Make("gem_danakil_depression", GemCategory.VolcanicFormation, GemRarity.Epic,
                GemContinent.Africa, "Ethiopia", 14.2417, 40.3000, 100f);

            // Blyde River Canyon potholes, South Africa
            yield return Make("gem_blyde_river_potholes", GemCategory.GeologicalFormation, GemRarity.Uncommon,
                GemContinent.Africa, "South Africa", -24.6561, 30.8033, 1000f);

            // Ennedi Plateau arches, Chad
            yield return Make("gem_ennedi_arches", GemCategory.NaturalArch, GemRarity.Legendary,
                GemContinent.Africa, "Chad", 16.8491, 21.9070, 800f, true, "discover_3_africa");

            // Skeleton Coast shipwrecks, Namibia
            yield return Make("gem_skeleton_coast", GemCategory.AbandonedStructure, GemRarity.Rare,
                GemContinent.Africa, "Namibia", -19.9890, 12.7840, 100f);

            // Chocolate Hills, Bohol — additional African entry replaced with
            // Cave of Swimmers, Egypt
            yield return Make("gem_cave_of_swimmers", GemCategory.AncientRuin, GemRarity.Rare,
                GemContinent.Africa, "Egypt", 22.9650, 25.3820, 200f);
        }

        // ── Oceania (8) ───────────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetOceaniaGems()
        {
            // Waitomo Glowworm Caves, New Zealand
            yield return Make("gem_waitomo_glowworm", GemCategory.UndergroundCave, GemRarity.Rare,
                GemContinent.Oceania, "New Zealand", -38.2614, 175.1047, 100f);

            // Bungle Bungle Range beehive domes, Australia
            yield return Make("gem_bungle_bungle", GemCategory.GeologicalFormation, GemRarity.Epic,
                GemContinent.Oceania, "Australia", -17.4994, 128.4105, 500f);

            // Milford Sound underwater observatory, New Zealand
            yield return Make("gem_milford_sound_obs", GemCategory.NaturalWonder, GemRarity.Uncommon,
                GemContinent.Oceania, "New Zealand", -44.6413, 167.8974, 200f);

            // Horizontal Falls, Western Australia
            yield return Make("gem_horizontal_falls", GemCategory.NaturalWonder, GemRarity.Epic,
                GemContinent.Oceania, "Australia", -16.3747, 124.0862, 50f);

            // Lord Howe Island Ball's Pyramid sea stack, Australia
            yield return Make("gem_balls_pyramid", GemCategory.GeologicalFormation, GemRarity.Legendary,
                GemContinent.Oceania, "Australia", -31.7558, 159.2476, 400f);

            // Blue Holes of Palau
            yield return Make("gem_palau_blue_holes", GemCategory.UndergroundCave, GemRarity.Rare,
                GemContinent.Oceania, "Palau", 7.3667, 134.4794, 50f);

            // Champagne Pool geothermal, New Zealand
            yield return Make("gem_champagne_pool", GemCategory.VolcanicFormation, GemRarity.Uncommon,
                GemContinent.Oceania, "New Zealand", -38.3390, 176.3635, 200f);

            // Wave Rock, Western Australia
            yield return Make("gem_wave_rock", GemCategory.GeologicalFormation, GemRarity.Uncommon,
                GemContinent.Oceania, "Australia", -32.4350, 118.8991, 200f);
        }

        // ── Antarctica (3) ────────────────────────────────────────────────────────

        private static IEnumerable<HiddenGemDefinition> GetAntarcticaGems()
        {
            // Blood Falls, Taylor Glacier
            yield return Make("gem_blood_falls", GemCategory.NaturalWonder, GemRarity.Legendary,
                GemContinent.Antarctica, "Antarctica", -77.7167, 162.2667, 300f);

            // Deception Island volcanic caldera
            yield return Make("gem_deception_island", GemCategory.VolcanicFormation, GemRarity.Epic,
                GemContinent.Antarctica, "Antarctica", -62.9731, -60.6459, 400f);

            // Ice tunnels of Ross Island
            yield return Make("gem_ross_island_tunnels", GemCategory.IceFormation, GemRarity.Rare,
                GemContinent.Antarctica, "Antarctica", -77.5287, 167.1503, 200f);
        }
    }
}
