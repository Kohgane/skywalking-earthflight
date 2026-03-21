using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Attached to the player's aircraft GameObject. Reads the active
    /// <see cref="AircraftLoadout"/> from <see cref="AircraftCustomizationManager"/>
    /// and applies the correct material, trail colours, decal, particle, and aura
    /// effects to the relevant renderers and attach points.
    /// </summary>
    public class AircraftVisualController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Part Renderers")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer wingsRenderer;
        [SerializeField] private Renderer engineRenderer;
        [SerializeField] private Renderer cockpitRenderer;

        [Header("Effect Attach Points")]
        [SerializeField] private Transform trailAttachPoint;
        [SerializeField] private Transform particleAttachPoint;
        [SerializeField] private Transform auraAttachPoint;
        [SerializeField] private Transform decalAttachPoint;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private AircraftCustomizationManager _customManager;
        private AircraftSkinRegistry _registry;

        private TrailRenderer _activeTrail;
        private GameObject _activeParticle;
        private GameObject _activeDecal;
        private GameObject _activeAura;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _customManager = AircraftCustomizationManager.Instance;
            _registry      = AircraftSkinRegistry.Instance;

            if (_customManager == null)
            {
                Debug.LogWarning("[AircraftVisualController] AircraftCustomizationManager not found.");
                return;
            }

            _customManager.OnLoadoutChanged += ApplyLoadout;

            // Apply the current loadout immediately.
            if (_customManager.ActiveLoadout != null)
                ApplyLoadout(_customManager.ActiveLoadout);
        }

        private void OnDestroy()
        {
            if (_customManager != null)
                _customManager.OnLoadoutChanged -= ApplyLoadout;

            ClearAll();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Iterates all eight part slots and applies the skin defined in
        /// <paramref name="loadout"/>.
        /// </summary>
        public void ApplyLoadout(AircraftLoadout loadout)
        {
            if (loadout == null) return;

            foreach (AircraftPartType part in Enum.GetValues(typeof(AircraftPartType)))
                ApplyPart(part, loadout.GetSkinForPart(part));
        }

        /// <summary>
        /// Looks up the <see cref="AircraftSkinDefinition"/> for <paramref name="skinId"/>
        /// and dispatches to the relevant apply method.
        /// </summary>
        public void ApplyPart(AircraftPartType part, string skinId)
        {
            if (string.IsNullOrEmpty(skinId))
            {
                ClearSlot(part);
                return;
            }

            if (_registry == null) return;

            var skin = _registry.GetSkin(skinId);
            if (skin == null)
            {
                Debug.LogWarning($"[AircraftVisualController] Skin '{skinId}' not found in registry.");
                return;
            }

            switch (part)
            {
                case AircraftPartType.Body:    ApplyMaterial(bodyRenderer,    skin.materialId); break;
                case AircraftPartType.Wings:   ApplyMaterial(wingsRenderer,   skin.materialId); break;
                case AircraftPartType.Engine:  ApplyMaterial(engineRenderer,  skin.materialId); break;
                case AircraftPartType.Cockpit: ApplyMaterial(cockpitRenderer, skin.materialId); break;
                case AircraftPartType.Trail:   ApplyTrail(skinId);   break;
                case AircraftPartType.Decal:   ApplyDecal(skinId);   break;
                case AircraftPartType.Particle: ApplyParticle(skinId); break;
                case AircraftPartType.Aura:    ApplyAura(skinId);    break;
            }
        }

        /// <summary>
        /// Loads the material named <paramref name="materialId"/> from Resources
        /// and applies it to <paramref name="target"/>.
        /// </summary>
        public void ApplyMaterial(Renderer target, string materialId)
        {
            if (target == null || string.IsNullOrEmpty(materialId)) return;

            var mat = Resources.Load<Material>(materialId);
            if (mat == null)
            {
                Debug.LogWarning($"[AircraftVisualController] Material '{materialId}' not found in Resources.");
                return;
            }
            target.material = mat;
        }

        /// <summary>
        /// Configures the aircraft's contrail colours from the given trail skin.
        /// </summary>
        public void ApplyTrail(string skinId)
        {
            if (_registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null) return;

            if (_activeTrail == null && trailAttachPoint != null)
                _activeTrail = trailAttachPoint.gameObject.GetComponentInChildren<TrailRenderer>();

            if (_activeTrail == null) return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(skin.trailColorPrimary,   0f),
                        new GradientColorKey(skin.trailColorSecondary, 1f) },
                new[] { new GradientAlphaKey(skin.trailColorPrimary.a,   0f),
                        new GradientAlphaKey(skin.trailColorSecondary.a, 1f) }
            );
            _activeTrail.colorGradient = gradient;
        }

        /// <summary>
        /// Instantiates the particle-system prefab for the given skin at the
        /// <see cref="particleAttachPoint"/>, replacing any previous particle.
        /// </summary>
        public void ApplyParticle(string skinId)
        {
            if (_registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null || string.IsNullOrEmpty(skin.particleSystemId)) return;

            if (_activeParticle != null) Destroy(_activeParticle);

            var prefab = Resources.Load<GameObject>(skin.particleSystemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[AircraftVisualController] Particle prefab '{skin.particleSystemId}' not found.");
                return;
            }

            Transform parent = particleAttachPoint != null ? particleAttachPoint : transform;
            _activeParticle = Instantiate(prefab, parent.position, parent.rotation, parent);
        }

        /// <summary>
        /// Positions / replaces the decal sprite at the <see cref="decalAttachPoint"/>.
        /// </summary>
        public void ApplyDecal(string skinId)
        {
            if (_registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null || string.IsNullOrEmpty(skin.materialId)) return;

            if (_activeDecal != null) Destroy(_activeDecal);

            var prefab = Resources.Load<GameObject>(skin.materialId);
            if (prefab == null) return;

            Transform parent = decalAttachPoint != null ? decalAttachPoint : transform;
            _activeDecal = Instantiate(prefab, parent.position, parent.rotation, parent);
        }

        /// <summary>
        /// Instantiates the aura particle effect at the <see cref="auraAttachPoint"/>.
        /// </summary>
        public void ApplyAura(string skinId)
        {
            if (_registry == null) return;
            var skin = _registry.GetSkin(skinId);
            if (skin == null || string.IsNullOrEmpty(skin.particleSystemId)) return;

            if (_activeAura != null) Destroy(_activeAura);

            var prefab = Resources.Load<GameObject>(skin.particleSystemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[AircraftVisualController] Aura prefab '{skin.particleSystemId}' not found.");
                return;
            }

            Transform parent = auraAttachPoint != null ? auraAttachPoint : transform;
            _activeAura = Instantiate(prefab, parent.position, parent.rotation, parent);
        }

        /// <summary>
        /// Removes the visual element for a single part slot.
        /// </summary>
        public void ClearSlot(AircraftPartType part)
        {
            switch (part)
            {
                case AircraftPartType.Trail:
                    if (_activeTrail != null)
                    {
                        _activeTrail.Clear();
                        _activeTrail.enabled = false;
                    }
                    break;
                case AircraftPartType.Particle:
                    if (_activeParticle != null) Destroy(_activeParticle);
                    _activeParticle = null;
                    break;
                case AircraftPartType.Decal:
                    if (_activeDecal != null) Destroy(_activeDecal);
                    _activeDecal = null;
                    break;
                case AircraftPartType.Aura:
                    if (_activeAura != null) Destroy(_activeAura);
                    _activeAura = null;
                    break;
            }
        }

        /// <summary>
        /// Removes all customization visuals from the aircraft.
        /// </summary>
        public void ClearAll()
        {
            if (_activeTrail != null) { _activeTrail.Clear(); _activeTrail.enabled = false; }
            if (_activeParticle != null) Destroy(_activeParticle);
            if (_activeDecal    != null) Destroy(_activeDecal);
            if (_activeAura     != null) Destroy(_activeAura);

            _activeParticle = null;
            _activeDecal    = null;
            _activeAura     = null;
        }
    }
}
