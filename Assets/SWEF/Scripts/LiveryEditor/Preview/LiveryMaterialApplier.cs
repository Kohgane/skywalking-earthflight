// LiveryMaterialApplier.cs — Phase 115: Advanced Aircraft Livery Editor
// Apply finished livery to aircraft material: base color, metallic, roughness, emission maps.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Applies completed livery textures to an aircraft <see cref="Material"/>.
    /// Sets the base-colour, metallic, roughness (smoothness), and optional emission maps.
    /// </summary>
    public class LiveryMaterialApplier : MonoBehaviour
    {
        // ── Shader property IDs (cached for performance) ──────────────────────────
        private static readonly int PropMainTex     = Shader.PropertyToID("_MainTex");
        private static readonly int PropBaseColor   = Shader.PropertyToID("_BaseColor");
        private static readonly int PropMetallic    = Shader.PropertyToID("_Metallic");
        private static readonly int PropSmoothness  = Shader.PropertyToID("_Smoothness");
        private static readonly int PropEmissionMap = Shader.PropertyToID("_EmissionMap");

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the livery texture is successfully applied to a material.</summary>
        public event Action<Material, Texture2D> OnLiveryApplied;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the given livery texture as the albedo / base-colour map of a material.
        /// </summary>
        /// <param name="material">Target material.</param>
        /// <param name="liveryTexture">Composited livery texture.</param>
        public void Apply(Material material, Texture2D liveryTexture)
        {
            if (material == null || liveryTexture == null) return;

            if (material.HasProperty(PropBaseColor))
                material.SetTexture(PropBaseColor, liveryTexture);
            else if (material.HasProperty(PropMainTex))
                material.SetTexture(PropMainTex, liveryTexture);

            OnLiveryApplied?.Invoke(material, liveryTexture);
        }

        /// <summary>
        /// Sets the metallic intensity on the material.
        /// </summary>
        /// <param name="material">Target material.</param>
        /// <param name="metallic">Metallic value (0 = non-metallic, 1 = fully metallic).</param>
        public void SetMetallic(Material material, float metallic)
        {
            if (material == null) return;
            if (material.HasProperty(PropMetallic))
                material.SetFloat(PropMetallic, Mathf.Clamp01(metallic));
        }

        /// <summary>
        /// Sets the smoothness (roughness inverse) on the material.
        /// </summary>
        /// <param name="material">Target material.</param>
        /// <param name="smoothness">Smoothness value (0 = rough, 1 = perfectly smooth).</param>
        public void SetSmoothness(Material material, float smoothness)
        {
            if (material == null) return;
            if (material.HasProperty(PropSmoothness))
                material.SetFloat(PropSmoothness, Mathf.Clamp01(smoothness));
        }

        /// <summary>
        /// Applies an emission map to the material and enables the emission keyword.
        /// </summary>
        /// <param name="material">Target material.</param>
        /// <param name="emissionMap">Emission texture (typically an illumination overlay).</param>
        public void SetEmissionMap(Material material, Texture2D emissionMap)
        {
            if (material == null) return;
            if (material.HasProperty(PropEmissionMap))
            {
                material.SetTexture(PropEmissionMap, emissionMap);
                material.EnableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// Convenience method that applies all maps in one call.
        /// </summary>
        /// <param name="material">Target material.</param>
        /// <param name="liveryTexture">Base-colour / albedo map.</param>
        /// <param name="metallic">Metallic value (0–1).</param>
        /// <param name="smoothness">Smoothness value (0–1).</param>
        /// <param name="emissionMap">Optional emission map; pass <c>null</c> to skip.</param>
        public void ApplyAll(Material material, Texture2D liveryTexture,
            float metallic = 0.2f, float smoothness = 0.6f, Texture2D emissionMap = null)
        {
            Apply(material, liveryTexture);
            SetMetallic(material, metallic);
            SetSmoothness(material, smoothness);
            if (emissionMap != null) SetEmissionMap(material, emissionMap);
        }
    }
}
