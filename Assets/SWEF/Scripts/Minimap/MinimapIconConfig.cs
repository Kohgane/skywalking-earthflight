using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Minimap
{
    /// <summary>
    /// Serializable entry that maps a <see cref="MinimapIconType"/> to its visual representation.
    /// </summary>
    [Serializable]
    public class IconEntry
    {
        /// <summary>The icon type this entry applies to.</summary>
        public MinimapIconType iconType;

        /// <summary>Sprite rendered on the minimap for this icon type.</summary>
        public Sprite sprite;

        /// <summary>Default tint colour applied to the icon.</summary>
        public Color defaultColor = Color.white;

        /// <summary>Uniform scale multiplier applied to the icon.</summary>
        public float defaultScale = 1f;

        /// <summary>Whether to draw the blip's label text beneath the icon.</summary>
        public bool showLabel = true;
    }

    /// <summary>
    /// ScriptableObject that maps every <see cref="MinimapIconType"/> to its sprite, colour and
    /// scale settings. Assign one instance to <see cref="MinimapRenderer"/> and
    /// <see cref="RadarOverlay"/> via the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "MinimapIconConfig", menuName = "SWEF/Minimap/Icon Config")]
    public class MinimapIconConfig : ScriptableObject
    {
        [Header("Icon Entries")]
        [Tooltip("One entry per MinimapIconType. Missing types fall back to the default entry.")]
        [SerializeField] private List<IconEntry> entries = new List<IconEntry>();

        // ── Private lookup cache ──────────────────────────────────────────────────
        private Dictionary<MinimapIconType, IconEntry> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<MinimapIconType, IconEntry>();
            foreach (var e in entries)
            {
                if (e != null)
                    _lookup[e.iconType] = e;
            }
        }

        /// <summary>
        /// Returns the <see cref="IconEntry"/> for <paramref name="type"/>,
        /// or a fallback default entry if none is configured.
        /// </summary>
        public IconEntry GetEntry(MinimapIconType type)
        {
            if (_lookup == null) BuildLookup();

            if (_lookup.TryGetValue(type, out IconEntry entry))
                return entry;

            // Fallback: return first entry or a safe default
            if (entries != null && entries.Count > 0)
                return entries[0];

            return new IconEntry { iconType = type, defaultColor = Color.white, defaultScale = 1f };
        }

        /// <summary>
        /// Populates <see cref="entries"/> with one default <see cref="IconEntry"/> per
        /// <see cref="MinimapIconType"/>. Useful when creating the asset for the first time.
        /// Call from an Editor script or OnValidate.
        /// </summary>
        public void PopulateDefaults()
        {
            var defaults = new Dictionary<MinimapIconType, Color>
            {
                { MinimapIconType.Player,         Color.cyan                            },
                { MinimapIconType.Waypoint,        Color.white                           },
                { MinimapIconType.WaypointNext,    Color.yellow                          },
                { MinimapIconType.WaypointVisited, new Color(0.5f, 0.5f, 0.5f)          },
                { MinimapIconType.OtherPlayer,     new Color(0.2f, 0.8f, 0.2f)          },
                { MinimapIconType.FormationSlot,   new Color(0.4f, 0.8f, 1.0f)          },
                { MinimapIconType.GhostReplay,     new Color(0.7f, 0.4f, 1.0f)          },
                { MinimapIconType.WorldEvent,      new Color(1.0f, 0.6f, 0.0f)          },
                { MinimapIconType.WeatherZone,     new Color(0.4f, 0.7f, 1.0f)          },
                { MinimapIconType.PointOfInterest, Color.white                           },
                { MinimapIconType.Destination,     new Color(1.0f, 0.3f, 0.3f)          },
                { MinimapIconType.TourPath,        new Color(0.3f, 0.8f, 0.3f)          },
                { MinimapIconType.DangerZone,      Color.red                             },
                { MinimapIconType.LandingZone,     new Color(0.3f, 1.0f, 0.5f)          },
            };

            entries = new List<IconEntry>();
            foreach (var kvp in defaults)
            {
                entries.Add(new IconEntry
                {
                    iconType     = kvp.Key,
                    sprite       = null,
                    defaultColor = kvp.Value,
                    defaultScale = kvp.Key == MinimapIconType.Player ? 1.4f : 1f,
                    showLabel    = kvp.Key != MinimapIconType.TourPath
                                && kvp.Key != MinimapIconType.WaypointVisited
                });
            }
        }
    }
}
