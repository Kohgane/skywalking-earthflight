using System.Collections.Generic;
using UnityEngine;
using SWEF.Minimap;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Bridges <see cref="HiddenGemManager"/> and <see cref="MinimapManager"/>.
    /// Registers discovered gems as <see cref="MinimapIconType.PointOfInterest"/> blips,
    /// shows pulsing hint blips for undiscovered gems with <c>isHintVisible</c>,
    /// and automatically hides blips for gems beyond <see cref="hideRangeKm"/> km.
    /// </summary>
    public class GemMinimapIntegration : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Tooltip("Gems further than this distance (km) are temporarily hidden on the minimap.")]
        [SerializeField] private float hideRangeKm = 10f;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly HashSet<string> _registeredBlipIds = new HashSet<string>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            var mgr = HiddenGemManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[SWEF] GemMinimapIntegration: HiddenGemManager not found.");
                return;
            }
            mgr.OnGemDiscovered += OnGemDiscovered;
            RegisterAllBlips();
        }

        private void OnDestroy()
        {
            if (HiddenGemManager.Instance != null)
                HiddenGemManager.Instance.OnGemDiscovered -= OnGemDiscovered;
            UnregisterAllBlips();
        }

        private void Update()
        {
            UpdateBlipVisibility();
        }

        // ── Registration ──────────────────────────────────────────────────────────

        private void RegisterAllBlips()
        {
            var mgr = HiddenGemManager.Instance;
            var mm  = MinimapManager.Instance;
            if (mgr == null || mm == null) return;

            foreach (var gem in mgr.GetAllGems())
            {
                bool disc = mgr.IsGemDiscovered(gem.gemId);
                if (disc)
                    RegisterDiscovered(gem);
                else if (gem.isHintVisible)
                    RegisterHint(gem);
            }
        }

        private void RegisterDiscovered(HiddenGemDefinition gem)
        {
            var mm = MinimapManager.Instance;
            if (mm == null) return;
            string id   = BlipId(gem);
            string hint = HintBlipId(gem);

            // Remove hint blip if present
            if (_registeredBlipIds.Contains(hint))
            {
                mm.UnregisterBlip(hint);
                _registeredBlipIds.Remove(hint);
            }

            if (_registeredBlipIds.Contains(id)) return;

            var blip = BuildDiscoveredBlip(gem);
            mm.RegisterBlip(blip);
            _registeredBlipIds.Add(id);
        }

        private void RegisterHint(HiddenGemDefinition gem)
        {
            var mm = MinimapManager.Instance;
            if (mm == null) return;
            string id = HintBlipId(gem);
            if (_registeredBlipIds.Contains(id)) return;

            var blip = new MinimapBlip
            {
                blipId        = id,
                iconType      = MinimapIconType.PointOfInterest,
                worldPosition = HiddenGemManager.GetWorldPosition(gem),
                label         = "???",
                color         = new Color(0.6f, 0.6f, 0.6f, 0.5f),
                isActive      = true,
                isPulsing     = true,
                customIconId  = ""
            };
            blip.metadata["gemId"]  = gem.gemId;
            blip.metadata["isHint"] = "true";
            mm.RegisterBlip(blip);
            _registeredBlipIds.Add(id);
        }

        private static MinimapBlip BuildDiscoveredBlip(HiddenGemDefinition gem)
        {
            ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(gem.rarity), out Color c);
            var blip = new MinimapBlip
            {
                blipId        = BlipId(gem),
                iconType      = MinimapIconType.PointOfInterest,
                worldPosition = HiddenGemManager.GetWorldPosition(gem),
                label         = gem.nameKey,
                color         = c,
                isActive      = true,
                isPulsing     = false,
                customIconId  = gem.iconOverride
            };
            blip.metadata["gemId"]    = gem.gemId;
            blip.metadata["rarity"]   = gem.rarity.ToString();
            blip.metadata["category"] = gem.category.ToString();
            return blip;
        }

        private void UnregisterAllBlips()
        {
            var mm = MinimapManager.Instance;
            if (mm == null) return;
            foreach (string id in _registeredBlipIds)
                mm.UnregisterBlip(id);
            _registeredBlipIds.Clear();
        }

        // ── Distance-based visibility ─────────────────────────────────────────────

        private void UpdateBlipVisibility()
        {
            var mm = MinimapManager.Instance;
            if (mm == null) return;
            float hideRange = hideRangeKm * 1000f;

            foreach (string id in _registeredBlipIds)
            {
                var blip = mm.GetBlip(id);
                if (blip == null) continue;
                bool close = blip.distanceFromPlayer <= hideRange;
                blip.isActive = close;
            }
        }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void OnGemDiscovered(GemDiscoveryEvent evt)
        {
            RegisterDiscovered(evt.gem);
        }

        // ── Id helpers ────────────────────────────────────────────────────────────

        private static string BlipId(HiddenGemDefinition gem)     => "hiddengem_"      + gem.gemId;
        private static string HintBlipId(HiddenGemDefinition gem) => "hiddengem_hint_" + gem.gemId;
    }
}
