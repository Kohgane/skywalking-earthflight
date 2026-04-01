// MusicPlayerBridge.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System;
using System.Reflection;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Bridges the adaptive music system with the existing MusicPlayer subsystem
    /// without a compile-time dependency (uses reflection).
    ///
    /// Three modes:
    ///   <see cref="BridgeMode.AdaptiveOnly"/>  — only adaptive stems play
    ///   <see cref="BridgeMode.PlaylistOnly"/>  — only the user's MusicPlayer playlist plays
    ///   <see cref="BridgeMode.Hybrid"/>        — both play, adaptive is ducked while playlist is active
    /// </summary>
    public class MusicPlayerBridge : MonoBehaviour
    {
        // ── Types ─────────────────────────────────────────────────────────────

        public enum BridgeMode
        {
            AdaptiveOnly,
            PlaylistOnly,
            Hybrid
        }

        // ── Inspector ──────────────────────────────────────────────────────────

        [SerializeField] private BridgeMode _mode = BridgeMode.Hybrid;

        [Tooltip("Volume multiplier applied to adaptive music when Hybrid mode is active " +
                 "and the user's playlist is playing.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hybridDuckLevel = 0.4f;

        // ── State ─────────────────────────────────────────────────────────────

        private object _musicPlayerManager;   // resolved via reflection
        private bool   _playlistWasActive;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Start()
        {
            ResolveManager();
            ApplyMode(_mode);
        }

        private void Update()
        {
            if (_mode != BridgeMode.Hybrid) return;
            bool playlistActive = IsPlaylistPlaying();
            if (playlistActive != _playlistWasActive)
            {
                _playlistWasActive = playlistActive;
                var stemMixer = FindObjectOfType<StemMixer>();
                if (stemMixer == null) return;
                if (playlistActive) stemMixer.Duck();
                else                stemMixer.Unduck();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Switches the bridge mode at runtime.</summary>
        public void SetMode(BridgeMode mode)
        {
            _mode = mode;
            ApplyMode(mode);
        }

        public BridgeMode CurrentMode => _mode;

        // ── Internals ─────────────────────────────────────────────────────────

        private void ResolveManager()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("SWEF.MusicPlayer.MusicPlayerManager");
                    if (t != null)
                    {
                        var prop = t.GetProperty("Instance",
                            BindingFlags.Public | BindingFlags.Static);
                        _musicPlayerManager = prop?.GetValue(null);
                        break;
                    }
                }
            }
            catch
            {
                _musicPlayerManager = null;
            }
        }

        private void ApplyMode(BridgeMode mode)
        {
            if (_musicPlayerManager == null) return;
            try
            {
                switch (mode)
                {
                    case BridgeMode.PlaylistOnly:
                        InvokeMethod("Resume");
                        AdaptiveMusicManager.Instance?.Pause();
                        break;
                    case BridgeMode.AdaptiveOnly:
                        InvokeMethod("Pause");
                        AdaptiveMusicManager.Instance?.Resume();
                        break;
                    case BridgeMode.Hybrid:
                        InvokeMethod("Resume");
                        AdaptiveMusicManager.Instance?.Resume();
                        break;
                }
            }
            catch { /* reflection failure — graceful degradation */ }
        }

        private bool IsPlaylistPlaying()
        {
            if (_musicPlayerManager == null) return false;
            try
            {
                var t    = _musicPlayerManager.GetType();
                var prop = t.GetProperty("IsPlaying", BindingFlags.Public | BindingFlags.Instance);
                return prop != null && (bool)prop.GetValue(_musicPlayerManager);
            }
            catch { return false; }
        }

        private void InvokeMethod(string methodName)
        {
            if (_musicPlayerManager == null) return;
            var t  = _musicPlayerManager.GetType();
            var m  = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            m?.Invoke(_musicPlayerManager, null);
        }
    }
}
