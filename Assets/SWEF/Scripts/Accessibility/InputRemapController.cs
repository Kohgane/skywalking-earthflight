// InputRemapController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Singleton MonoBehaviour that manages full keyboard, gamepad, and touch remap for
    /// every action in <see cref="RemappableActions.All"/>.
    ///
    /// <para>Settings are persisted to <c>input_remap.json</c> in the persistent data path.</para>
    /// </summary>
    public class InputRemapController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static InputRemapController Instance { get; private set; }

        // ── Paths ─────────────────────────────────────────────────────────────────
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "input_remap.json");

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, InputRemapData> _bindings
            = new Dictionary<string, InputRemapData>(StringComparer.Ordinal);

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after any binding changes; provides the updated action name.</summary>
        public event Action<string> OnBindingChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the current binding for <paramref name="actionName"/>.</summary>
        public InputRemapData GetBinding(string actionName)
        {
            _bindings.TryGetValue(actionName, out var data);
            return data;
        }

        /// <summary>
        /// Updates the primary-key binding for <paramref name="actionName"/>.
        /// Performs conflict detection and rejects the change if the key is already used
        /// (unless <paramref name="force"/> is <c>true</c>).
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on conflict.</returns>
        public bool SetPrimaryKey(string actionName, string key, bool force = false)
        {
            if (!force && IsKeyConflict(key, actionName))
            {
                Debug.LogWarning($"[SWEF] Accessibility: Key '{key}' is already bound to another action.");
                return false;
            }

            EnsureBinding(actionName).primaryKey = key;
            OnBindingChanged?.Invoke(actionName);
            SaveBindings();
            return true;
        }

        /// <summary>Updates the gamepad button binding for <paramref name="actionName"/>.</summary>
        public bool SetGamepadButton(string actionName, string button, bool force = false)
        {
            if (!force && IsGamepadConflict(button, actionName))
            {
                Debug.LogWarning($"[SWEF] Accessibility: Gamepad button '{button}' is already bound.");
                return false;
            }

            EnsureBinding(actionName).gamepadButton = button;
            OnBindingChanged?.Invoke(actionName);
            SaveBindings();
            return true;
        }

        /// <summary>Resets a single action binding to the engine default (clears all overrides).</summary>
        public void ResetBinding(string actionName)
        {
            _bindings.Remove(actionName);
            OnBindingChanged?.Invoke(actionName);
            SaveBindings();
        }

        /// <summary>Resets all bindings to engine defaults.</summary>
        public void ResetAll()
        {
            _bindings.Clear();
            SaveBindings();
            Debug.Log("[SWEF] Accessibility: All input bindings reset to default.");
        }

        /// <summary>Exports the current binding set as a JSON string.</summary>
        public string ExportJson()
        {
            var list = new List<InputRemapData>(_bindings.Values);
            return JsonUtility.ToJson(new SerializableList<InputRemapData> { items = list }, prettyPrint: true);
        }

        /// <summary>Imports bindings from a JSON string produced by <see cref="ExportJson"/>.</summary>
        public void ImportJson(string json)
        {
            try
            {
                var list = JsonUtility.FromJson<SerializableList<InputRemapData>>(json);
                _bindings.Clear();
                foreach (var item in list.items)
                    _bindings[item.actionName] = item;
                SaveBindings();
                Debug.Log($"[SWEF] Accessibility: Imported {list.items.Count} input bindings.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Accessibility: Import failed — {ex.Message}");
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void SaveBindings()
        {
            try
            {
                var list = new List<InputRemapData>(_bindings.Values);
                string json = JsonUtility.ToJson(new SerializableList<InputRemapData> { items = list }, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Accessibility: Failed to save input bindings — {ex.Message}");
            }
        }

        private void LoadBindings()
        {
            _bindings.Clear();
            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                var list = JsonUtility.FromJson<SerializableList<InputRemapData>>(json);
                foreach (var item in list.items)
                    _bindings[item.actionName] = item;
                Debug.Log($"[SWEF] Accessibility: Loaded {_bindings.Count} input bindings.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Accessibility: Failed to load input bindings — {ex.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private InputRemapData EnsureBinding(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out var data))
            {
                data = new InputRemapData { actionName = actionName };
                _bindings[actionName] = data;
            }
            return data;
        }

        private bool IsKeyConflict(string key, string excludeAction)
        {
            if (string.IsNullOrEmpty(key)) return false;
            foreach (var kvp in _bindings)
            {
                if (kvp.Key == excludeAction) continue;
                if (kvp.Value.primaryKey == key || kvp.Value.secondaryKey == key)
                    return true;
            }
            return false;
        }

        private bool IsGamepadConflict(string button, string excludeAction)
        {
            if (string.IsNullOrEmpty(button)) return false;
            foreach (var kvp in _bindings)
            {
                if (kvp.Key == excludeAction) continue;
                if (kvp.Value.gamepadButton == button)
                    return true;
            }
            return false;
        }

        // ── Serialization wrapper ─────────────────────────────────────────────────

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items = new List<T>();
        }
    }
}
