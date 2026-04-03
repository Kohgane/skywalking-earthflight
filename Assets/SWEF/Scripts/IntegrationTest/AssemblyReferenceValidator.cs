// AssemblyReferenceValidator.cs — SWEF Phase 96: Integration Test & QA Framework
// Addresses the known CI build errors: ReplayTheater ↔ Replay assembly references
// and UnityEngine.UI availability. Validates all .asmdef files in the project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Validates all Unity Assembly Definition Files (.asmdef) in the project:
    /// <list type="bullet">
    ///   <item>Checks that every reference name corresponds to another .asmdef in the project.</item>
    ///   <item>Specifically validates the ReplayTheater ↔ Replay reference chain.</item>
    ///   <item>Checks that <c>UnityEngine.UI</c> is available in the current runtime.</item>
    ///   <item>Detects simple circular dependencies (A → B → A).</item>
    /// </list>
    ///
    /// <para>This validator is Editor-only in its file-scanning mode but the type-check
    /// portion runs on all platforms.</para>
    /// </summary>
    public class AssemblyReferenceValidator : IntegrationTestCase
    {
        /// <inheritdoc/>
        public override string TestName => "AssemblyReferenceValidation";

        /// <inheritdoc/>
        public override string ModuleName => "BuildPipeline";

        /// <inheritdoc/>
        public override int Priority => 1; // Critical infrastructure — run first.

        private readonly List<string> _errors   = new List<string>();
        private readonly List<string> _warnings  = new List<string>();

        /// <inheritdoc/>
        public override IntegrationTestResult Setup() => null;

        /// <inheritdoc/>
        public override IntegrationTestResult Execute()
        {
            _errors.Clear();
            _warnings.Clear();

            // 1. Validate UnityEngine.UI availability (runtime check, all platforms).
            ValidateUnityEngineUI();

            // 2. Validate .asmdef files (editor / file-system check).
#if UNITY_EDITOR
            ValidateAsmdefFiles();
#else
            _warnings.Add("Asmdef file scan skipped on non-Editor build.");
#endif

            if (_errors.Count == 0)
            {
                string msg = _warnings.Count > 0
                    ? $"OK (warnings: {string.Join("; ", _warnings)})"
                    : "All assembly references validated.";
                return Pass(msg);
            }

            return Fail($"Errors: {string.Join("; ", _errors)}");
        }

        /// <inheritdoc/>
        public override void Teardown() { }

        // ── UnityEngine.UI check ──────────────────────────────────────────────

        private void ValidateUnityEngineUI()
        {
            // Probe for core UI types that ReplayTheaterUI and others depend on.
            string[] requiredUITypes =
            {
                "UnityEngine.UI.Button",
                "UnityEngine.UI.Text",
                "UnityEngine.UI.Slider",
                "UnityEngine.UI.RawImage",
                "UnityEngine.UI.Dropdown",
            };

            foreach (string typeName in requiredUITypes)
            {
                Type t = Type.GetType(typeName + ", UnityEngine.UI");
                if (t == null)
                    t = FindType(typeName);

                if (t == null)
                    _errors.Add($"UnityEngine.UI type not found: {typeName}");
            }

            if (_errors.Count == 0)
                Debug.Log("[AssemblyReferenceValidator] UnityEngine.UI types: all present.");
        }

        // ── .asmdef file scan (Editor only) ──────────────────────────────────

#if UNITY_EDITOR
        private void ValidateAsmdefFiles()
        {
            string assetsPath = Application.dataPath; // <Project>/Assets
            string[] asmdefPaths = Directory.GetFiles(assetsPath, "*.asmdef", SearchOption.AllDirectories);

            if (asmdefPaths.Length == 0)
            {
                _warnings.Add("No .asmdef files found in Assets/.");
                return;
            }

            // Build a set of all known assembly names.
            var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var asmdefMap  = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase); // name → references[]

            foreach (string path in asmdefPaths)
            {
                string json = File.ReadAllText(path);
                string name = ExtractJsonString(json, "name");
                if (string.IsNullOrEmpty(name)) continue;

                knownNames.Add(name);
                string[] refs = ExtractJsonStringArray(json, "references");
                asmdefMap[name] = refs;
            }

            // Check references.
            foreach (var kvp in asmdefMap)
            {
                string asmName = kvp.Key;
                foreach (string refName in kvp.Value)
                {
                    // Strip GUID-style references (e.g. "GUID:abc123")
                    if (refName.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!knownNames.Contains(refName))
                        _errors.Add($"{asmName} → '{refName}' (not found)");
                }
            }

            // Check specifically known critical paths.
            CheckCriticalReference(asmdefMap, "SWEF.ReplayTheater", "SWEF.Replay");
            CheckCriticalReference(asmdefMap, "SWEF.ReplayTheater", "SWEF.Cinema");

            // Detect simple circular dependencies (A → B → A).
            DetectCircularDependencies(asmdefMap);

            Debug.Log($"[AssemblyReferenceValidator] Scanned {asmdefPaths.Length} .asmdef file(s). " +
                      $"Known assemblies: {knownNames.Count}. Errors: {_errors.Count}.");
        }

        private void CheckCriticalReference(Dictionary<string, string[]> map, string owner, string dependency)
        {
            if (!map.TryGetValue(owner, out var refs))
            {
                _warnings.Add($"{owner} .asmdef not found — skipping critical reference check.");
                return;
            }

            bool found = refs.Any(r => string.Equals(r, dependency, StringComparison.OrdinalIgnoreCase));
            if (!found)
                _errors.Add($"Critical reference missing: {owner} must reference {dependency}.");
        }

        private void DetectCircularDependencies(Dictionary<string, string[]> map)
        {
            foreach (var kvp in map)
            {
                string a = kvp.Key;
                foreach (string b in kvp.Value)
                {
                    if (!map.TryGetValue(b, out var bRefs)) continue;
                    if (bRefs.Any(r => string.Equals(r, a, StringComparison.OrdinalIgnoreCase)))
                        _errors.Add($"Circular dependency: {a} ↔ {b}");
                }
            }
        }

        // Minimal JSON field extractors — avoids adding a JSON library dependency.
        private static string ExtractJsonString(string json, string key)
        {
            string search = $"\"{key}\"";
            int idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return null;

            idx = json.IndexOf('"', idx + search.Length);
            if (idx < 0) return null;
            int end = json.IndexOf('"', idx + 1);
            if (end < 0) return null;

            return json.Substring(idx + 1, end - idx - 1);
        }

        private static string[] ExtractJsonStringArray(string json, string key)
        {
            string search = $"\"{key}\"";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return Array.Empty<string>();

            int arrStart = json.IndexOf('[', start + search.Length);
            int arrEnd   = json.IndexOf(']', arrStart > 0 ? arrStart : start);
            if (arrStart < 0 || arrEnd < 0) return Array.Empty<string>();

            string block = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
            var parts = block.Split(',');
            var result = new List<string>();

            foreach (string part in parts)
            {
                string trimmed = part.Trim().Trim('"');
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result.ToArray();
        }
#endif

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Type FindType(string fullTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullTypeName);
                    if (t != null) return t;
                }
                catch { /* ignore */ }
            }

            return null;
        }
    }
}
