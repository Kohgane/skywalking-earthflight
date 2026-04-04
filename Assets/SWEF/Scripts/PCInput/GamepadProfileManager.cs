// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/GamepadProfileManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// Manages gamepad connection detection and profile switching for Xbox,
    /// PlayStation, and generic controllers.
    /// Supports custom profile creation and persistence via PlayerPrefs.
    /// </summary>
    /// <remarks>
    /// Default axis/button layout:
    /// <list type="bullet">
    ///   <item>Left stick — pitch / yaw</item>
    ///   <item>Right stick — camera look</item>
    ///   <item>RT — throttle up / LT — brake/throttle down</item>
    ///   <item>LB/RB — roll</item>
    ///   <item>D-pad — quick actions (map, HUD toggle, screenshot, etc.)</item>
    /// </list>
    /// </remarks>
    [DisallowMultipleComponent]
    public class GamepadProfileManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared gamepad profile manager instance.</summary>
        public static GamepadProfileManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildDefaultProfiles();
            LoadCustomProfiles();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Inspector
        [Header("Auto-detect Settings")]
        [Tooltip("Poll interval (seconds) for gamepad connection changes.")]
        [SerializeField, Range(0.5f, 5f)] private float pollInterval = 1f;

        [Header("Sensitivity")]
        [Tooltip("Left-stick axis sensitivity multiplier.")]
        [SerializeField, Range(0.1f, 3f)] private float axisScaleLeft = 1f;

        [Tooltip("Right-stick (camera) axis sensitivity multiplier.")]
        [SerializeField, Range(0.1f, 3f)] private float axisScaleRight = 1f;
        #endregion

        #region Events
        /// <summary>Fired when a gamepad is connected. Argument is the device name.</summary>
        public event Action<string> OnGamepadConnected;

        /// <summary>Fired when a gamepad is disconnected. Argument is the device name.</summary>
        public event Action<string> OnGamepadDisconnected;

        /// <summary>Fired when the active profile changes. Argument is the new profile.</summary>
        public event Action<GamepadProfile> OnProfileChanged;
        #endregion

        #region Public State
        /// <summary>Currently active gamepad profile.</summary>
        public GamepadProfile ActiveProfile { get; private set; }

        /// <summary>Whether any gamepad is currently connected.</summary>
        public bool IsGamepadConnected { get; private set; }

        /// <summary>Read-only list of all available profiles (built-in + custom).</summary>
        public IReadOnlyList<GamepadProfile> Profiles => _profiles;
        #endregion

        #region Private State
        private const string CustomProfilesKey = "SWEF_GamepadProfiles";
        private readonly List<GamepadProfile> _profiles = new List<GamepadProfile>();
        private readonly List<string> _previousJoysticks = new List<string>();
        private float _pollTimer;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            RefreshConnectedJoysticks(firstRun: true);
        }

        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= pollInterval)
            {
                _pollTimer = 0f;
                RefreshConnectedJoysticks(firstRun: false);
            }

            if (ActiveProfile != null && IsGamepadConnected)
                ReadGamepadInput();
        }
        #endregion

        #region Profile Management
        private void BuildDefaultProfiles()
        {
            _profiles.Clear();
            _profiles.Add(GamepadProfile.CreateXboxDefault());
            _profiles.Add(GamepadProfile.CreatePlayStationDefault());
            _profiles.Add(GamepadProfile.CreateGenericDefault());
            ActiveProfile = _profiles[0];
        }

        private void LoadCustomProfiles()
        {
            string json = PlayerPrefs.GetString(CustomProfilesKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<ProfileListWrapper>(json);
                if (wrapper?.profiles == null) return;
                foreach (var p in wrapper.profiles)
                    if (p != null) _profiles.Add(p);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GamepadProfileManager] Failed to load custom profiles: {e.Message}");
            }
        }

        private void SaveCustomProfiles()
        {
            var wrapper = new ProfileListWrapper();
            foreach (var p in _profiles)
                if (p.GamepadType == GamepadType.Custom) wrapper.profiles.Add(p);

            PlayerPrefs.SetString(CustomProfilesKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        /// <summary>Switch to a profile by index in <see cref="Profiles"/>.</summary>
        /// <param name="index">Zero-based index into <see cref="Profiles"/>.</param>
        public void SetProfile(int index)
        {
            if (index < 0 || index >= _profiles.Count)
            {
                Debug.LogWarning($"[GamepadProfileManager] Profile index {index} out of range.");
                return;
            }
            ActiveProfile = _profiles[index];
            OnProfileChanged?.Invoke(ActiveProfile);
        }

        /// <summary>Create a custom profile by cloning an existing one and save it.</summary>
        /// <param name="sourceIndex">Index of the profile to clone.</param>
        /// <param name="newName">Name for the new custom profile.</param>
        /// <returns>The newly created <see cref="GamepadProfile"/>.</returns>
        public GamepadProfile CreateCustomProfile(int sourceIndex, string newName)
        {
            if (sourceIndex < 0 || sourceIndex >= _profiles.Count)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            var clone = _profiles[sourceIndex].Clone();
            if (!string.IsNullOrEmpty(newName))
                clone.ProfileName = newName;

            _profiles.Add(clone);
            SaveCustomProfiles();
            return clone;
        }

        /// <summary>Delete a custom profile. Built-in profiles cannot be deleted.</summary>
        /// <param name="index">Index of the profile to delete.</param>
        public void DeleteCustomProfile(int index)
        {
            if (index < 0 || index >= _profiles.Count) return;
            if (_profiles[index].GamepadType != GamepadType.Custom)
            {
                Debug.LogWarning("[GamepadProfileManager] Cannot delete a built-in profile.");
                return;
            }
            if (ActiveProfile == _profiles[index])
                ActiveProfile = _profiles[0];
            _profiles.RemoveAt(index);
            SaveCustomProfiles();
        }
        #endregion

        #region Device Detection
        private void RefreshConnectedJoysticks(bool firstRun)
        {
            var current = new List<string>(Input.GetJoystickNames());
            bool anyConnected = false;
            foreach (var name in current)
                if (!string.IsNullOrEmpty(name)) { anyConnected = true; break; }

            if (!firstRun)
            {
                // Detect new connections
                foreach (var name in current)
                {
                    if (!string.IsNullOrEmpty(name) && !_previousJoysticks.Contains(name))
                    {
                        OnGamepadConnected?.Invoke(name);
                        AutoSelectProfile(name);
                    }
                }
                // Detect disconnections
                foreach (var name in _previousJoysticks)
                {
                    if (!string.IsNullOrEmpty(name) && !current.Contains(name))
                        OnGamepadDisconnected?.Invoke(name);
                }
            }
            else
            {
                foreach (var name in current)
                    if (!string.IsNullOrEmpty(name)) AutoSelectProfile(name);
            }

            IsGamepadConnected = anyConnected;
            _previousJoysticks.Clear();
            _previousJoysticks.AddRange(current);
        }

        private void AutoSelectProfile(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return;
            string lower = deviceName.ToLowerInvariant();

            GamepadType detected = GamepadType.Generic;
            if (lower.Contains("xbox") || lower.Contains("xinput"))
                detected = GamepadType.Xbox;
            else if (lower.Contains("playstation") || lower.Contains("dualshock") || lower.Contains("dualsense") || lower.Contains("ps4") || lower.Contains("ps5"))
                detected = GamepadType.PlayStation;

            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].GamepadType == detected)
                {
                    ActiveProfile = _profiles[i];
                    OnProfileChanged?.Invoke(ActiveProfile);
                    return;
                }
            }
        }
        #endregion

        #region Input Reading
        /// <summary>
        /// Current pitch input from gamepad [-1, 1]. Positive = pitch down.
        /// </summary>
        public float PitchInput { get; private set; }

        /// <summary>Current yaw input from gamepad [-1, 1].</summary>
        public float YawInput { get; private set; }

        /// <summary>Current roll input from gamepad [-1, 1].</summary>
        public float RollInput { get; private set; }

        /// <summary>Current throttle (RT) value [0, 1].</summary>
        public float ThrottleInput { get; private set; }

        /// <summary>Current brake (LT) value [0, 1].</summary>
        public float BrakeInput { get; private set; }

        /// <summary>Camera horizontal look from right stick [-1, 1].</summary>
        public float CameraHorizontal { get; private set; }

        /// <summary>Camera vertical look from right stick [-1, 1].</summary>
        public float CameraVertical { get; private set; }

        private void ReadGamepadInput()
        {
            if (ActiveProfile == null) return;

            PitchInput       = ReadAxis("Pitch")           * axisScaleLeft;
            YawInput         = ReadAxis("Yaw")             * axisScaleLeft;
            RollInput        = ReadButton("RollRight") - ReadButton("RollLeft");
            ThrottleInput    = ReadAxis("ThrottleUp");
            BrakeInput       = ReadAxis("ThrottleDown");
            CameraHorizontal = ReadAxis("CameraHorizontal") * axisScaleRight;
            CameraVertical   = ReadAxis("CameraVertical")   * axisScaleRight;
        }

        private float ReadAxis(string axisName)
        {
            if (ActiveProfile == null) return 0f;
            foreach (var m in ActiveProfile.AxisMappings)
            {
                if (m.axisName != axisName) continue;
                float raw = 0f;
                try { raw = Input.GetAxis(m.unityAxisName); } catch { return 0f; }
                if (Mathf.Abs(raw) < m.deadZone) return 0f;
                return m.inverted ? -raw : raw;
            }
            return 0f;
        }

        private float ReadButton(string actionName)
        {
            if (ActiveProfile == null) return 0f;
            foreach (var m in ActiveProfile.ButtonMappings)
            {
                if (m.actionName != actionName) continue;
                try { return Input.GetButton(m.unityButtonName) ? 1f : 0f; } catch { return 0f; }
            }
            return 0f;
        }
        #endregion

        #region Serialisation Helper
        [Serializable]
        private class ProfileListWrapper
        {
            public List<GamepadProfile> profiles = new List<GamepadProfile>();
        }
        #endregion
    }
}
