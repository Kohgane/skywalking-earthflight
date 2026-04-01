// MusicPlayerBridge.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using UnityEngine;
using SWEF.MusicPlayer;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Bridges the Adaptive Music System with the existing
    /// <see cref="SWEF.MusicPlayer.MusicPlayerManager"/> playlist system.
    ///
    /// <para>Supports three user-selectable modes:
    /// <list type="bullet">
    ///   <item><see cref="MusicMode.AdaptiveOnly"/> — adaptive plays; playlist is paused.</item>
    ///   <item><see cref="MusicMode.PlaylistOnly"/> — playlist plays; adaptive is disabled.</item>
    ///   <item><see cref="MusicMode.Hybrid"/> — adaptive plays during flight; playlist in menus.</item>
    /// </list>
    /// </para>
    ///
    /// <para>All <see cref="MusicPlayerManager"/> calls are null-safe; if Phase 82 is not
    /// present the bridge silently does nothing.</para>
    /// </summary>
    public class MusicPlayerBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [SerializeField] private AdaptiveMusicManager adaptiveManager;
        [SerializeField] private MusicPlayerManager   musicPlayerManager;

        [Header("Volume Balancing")]
        [Tooltip("Volume multiplier applied to MusicPlayerManager when adaptive is active in Hybrid mode.")]
        [SerializeField, Range(0f, 1f)] private float hybridPlaylistVolumeMult = 0.3f;

        [Tooltip("Crossfade duration (seconds) when handing off between systems.")]
        [SerializeField, Min(0.1f)] private float handoffDuration = 2f;

        // ── State ─────────────────────────────────────────────────────────────────
        private MusicMode _appliedMode;
        private bool      _isFlying;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (adaptiveManager    == null) adaptiveManager    = FindFirstObjectByType<AdaptiveMusicManager>();
            if (musicPlayerManager == null) musicPlayerManager = FindFirstObjectByType<MusicPlayerManager>();
        }

        private void OnEnable()
        {
            if (adaptiveManager != null)
                adaptiveManager.OnMoodChanged += HandleMoodChanged;
        }

        private void OnDisable()
        {
            if (adaptiveManager != null)
                adaptiveManager.OnMoodChanged -= HandleMoodChanged;
        }

        private void Start()
        {
            if (adaptiveManager != null)
                ApplyMode(adaptiveManager.Mode);
        }

        private void Update()
        {
            if (adaptiveManager == null) return;

            MusicMode current = adaptiveManager.Mode;
            if (current != _appliedMode)
                ApplyMode(current);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Notifies the bridge that the flight state has changed.</summary>
        public void SetFlyingState(bool flying)
        {
            _isFlying = flying;
            if (_appliedMode == MusicMode.Hybrid)
                ApplyHybrid();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void ApplyMode(MusicMode mode)
        {
            _appliedMode = mode;

            switch (mode)
            {
                case MusicMode.AdaptiveOnly:
                    adaptiveManager?.SetEnabled(true);
                    PausePlaylist();
                    break;

                case MusicMode.PlaylistOnly:
                    adaptiveManager?.SetEnabled(false);
                    ResumePlaylist(1f);
                    break;

                case MusicMode.Hybrid:
                    ApplyHybrid();
                    break;
            }
        }

        private void ApplyHybrid()
        {
            if (_isFlying)
            {
                // Adaptive takes priority during flight; duck the playlist
                adaptiveManager?.SetEnabled(true);
                DuckPlaylist(hybridPlaylistVolumeMult);
            }
            else
            {
                // In menus: playlist at full volume, adaptive fades out
                adaptiveManager?.FadeOut(handoffDuration);
                ResumePlaylist(1f);
            }
        }

        private void PausePlaylist()
        {
#if SWEF_MUSICPLAYER_AVAILABLE
            musicPlayerManager?.Pause();
#else
            if (musicPlayerManager != null)
            {
                // Use reflection to call Pause() if available
                var method = musicPlayerManager.GetType().GetMethod("Pause");
                method?.Invoke(musicPlayerManager, null);
            }
#endif
        }

        private void ResumePlaylist(float volumeMult)
        {
            if (musicPlayerManager == null) return;
            var resumeMethod = musicPlayerManager.GetType().GetMethod("Resume");
            resumeMethod?.Invoke(musicPlayerManager, null);
        }

        private void DuckPlaylist(float volumeMult)
        {
            if (musicPlayerManager == null) return;
            // Try to set volume via SetVolume(float) if available
            var setVol = musicPlayerManager.GetType().GetMethod("SetVolume",
                new[] { typeof(float) });
            setVol?.Invoke(musicPlayerManager, new object[] { volumeMult });
        }

        private void HandleMoodChanged(MusicMood prev, MusicMood next)
        {
            // When entering Danger mood, duck the playlist for urgency
            if (next == MusicMood.Danger)
                DuckPlaylist(0.1f);
            else if (prev == MusicMood.Danger)
                DuckPlaylist(hybridPlaylistVolumeMult);
        }
    }
}
