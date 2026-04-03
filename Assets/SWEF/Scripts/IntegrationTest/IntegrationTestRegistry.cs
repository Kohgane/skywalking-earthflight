// IntegrationTestRegistry.cs — SWEF Phase 96: Integration Test & QA Framework
// Static registry that collects all IntegrationTestCase instances.
// Supports manual registration and reflection-based auto-discovery.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Central registry for all <see cref="IntegrationTestCase"/> instances.
    ///
    /// <para>Test cases can be added explicitly via <see cref="Register"/> or
    /// discovered automatically via <see cref="DiscoverAll"/> which uses reflection
    /// to find every non-abstract subclass of <see cref="IntegrationTestCase"/>
    /// in all loaded assemblies.</para>
    /// </summary>
    public static class IntegrationTestRegistry
    {
        private static readonly List<IntegrationTestCase> _cases = new List<IntegrationTestCase>();
        private static bool _discovered;

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>Manually registers a test case instance.</summary>
        /// <param name="testCase">The test case to register. Must not be null.</param>
        public static void Register(IntegrationTestCase testCase)
        {
            if (testCase == null) throw new ArgumentNullException(nameof(testCase));
            if (!_cases.Contains(testCase))
                _cases.Add(testCase);
        }

        /// <summary>Removes all registered test cases and resets discovery state.</summary>
        public static void Clear()
        {
            _cases.Clear();
            _discovered = false;
        }

        // ── Auto-discovery ────────────────────────────────────────────────────

        /// <summary>
        /// Scans all loaded assemblies for non-abstract subclasses of
        /// <see cref="IntegrationTestCase"/> and registers a default-constructed
        /// instance of each.  Safe to call multiple times — discovery only runs once.
        /// </summary>
        public static void DiscoverAll()
        {
            if (_discovered) return;
            _discovered = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Partial failure — use whatever types loaded successfully.
                    types = ex.Types?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(IntegrationTestCase).IsAssignableFrom(type)) continue;

                    try
                    {
                        var instance = (IntegrationTestCase)Activator.CreateInstance(type);
                        Register(instance);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[IntegrationTestRegistry] Could not instantiate {type.FullName}: {ex.Message}");
                    }
                }
            }

            Debug.Log($"[IntegrationTestRegistry] Discovered {_cases.Count} test case(s).");
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>Returns all registered test cases, sorted by priority ascending.</summary>
        public static IReadOnlyList<IntegrationTestCase> GetAll()
            => _cases.OrderBy(c => c.Priority).ToList();

        /// <summary>Returns test cases belonging to the specified module name.</summary>
        /// <param name="moduleName">Case-insensitive module name filter.</param>
        public static IReadOnlyList<IntegrationTestCase> GetByModule(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return GetAll();
            return _cases
                .Where(c => string.Equals(c.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Priority)
                .ToList();
        }

        /// <summary>Returns test cases at or below the specified priority.</summary>
        /// <param name="maxPriority">Only cases with <c>Priority &lt;= maxPriority</c> are returned.</param>
        public static IReadOnlyList<IntegrationTestCase> GetByPriority(int maxPriority)
            => _cases
                .Where(c => c.Priority <= maxPriority)
                .OrderBy(c => c.Priority)
                .ToList();

        /// <summary>Total number of registered test cases.</summary>
        public static int Count => _cases.Count;
    }
}
