#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.Editor
{
    /// <summary>
    /// Editor utility that validates the World scene setup.
    /// Run via menu <b>SWEF → Validate World Scene</b>.
    /// Checks for all components required by SWEF and logs pass/fail results with the
    /// <c>[SWEF Validator]</c> prefix.
    /// </summary>
    public static class SWEFSceneValidator
    {
        // Component type names to check (string-based so we don't need hard assembly refs).
        private static readonly string[] k_RequiredTypes =
        {
            "CesiumGeoreference",
            "Cesium3DTileset",
            "FlightController",
            "TouchInputRouter",
            "AltitudeController",
            "HudBinder",
            "AtmosphereController",
            "SaveManager",
        };

        // ── Menu item ────────────────────────────────────────────────────────

        /// <summary>Validates all required components in the currently open World scene.</summary>
        [MenuItem("SWEF/Validate World Scene")]
        public static void ValidateWorldScene()
        {
            Debug.Log("[SWEF Validator] ─── Starting World Scene validation ───");

            var missing   = new List<string>();
            var found     = new List<string>();

            foreach (string typeName in k_RequiredTypes)
            {
                bool present = FindTypeInScene(typeName);
                if (present)
                {
                    Debug.Log($"[SWEF Validator] ✓ Found: {typeName}");
                    found.Add(typeName);
                }
                else
                {
                    Debug.LogWarning($"[SWEF Validator] ✗ Missing: {typeName}");
                    missing.Add(typeName);
                }
            }

            if (missing.Count == 0)
            {
                Debug.Log($"[SWEF Validator] ─── PASS — all {found.Count} required components present. ───");
            }
            else
            {
                Debug.LogError($"[SWEF Validator] ─── FAIL — {missing.Count} component(s) missing: " +
                               $"{string.Join(", ", missing)} ───");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Searches all active scenes (including DontDestroyOnLoad objects) for the
        /// first <see cref="Component"/> whose type name matches <paramref name="typeName"/>.
        /// </summary>
        private static bool FindTypeInScene(string typeName)
        {
            // Search all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (FindInHierarchy(root, typeName))
                        return true;
                }
            }

            // Also check DontDestroyOnLoad objects via FindObjectsByType (slow path, editor only)
            foreach (var obj in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (obj.GetType().Name == typeName)
                    return true;
            }

            return false;
        }

        private static bool FindInHierarchy(GameObject go, string typeName)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name == typeName)
                    return true;
            }

            foreach (Transform child in go.transform)
            {
                if (FindInHierarchy(child.gameObject, typeName))
                    return true;
            }

            return false;
        }
    }
}
#endif
