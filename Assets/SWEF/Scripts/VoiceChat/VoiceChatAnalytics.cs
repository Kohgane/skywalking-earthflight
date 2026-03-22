using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Tracks Voice Chat usage analytics and submits events to <see cref="TelemetryDispatcher"/>.
    /// <para>
    /// Tracked metrics:
    /// <list type="bullet">
    ///   <item>Voice chat session duration.</item>
    ///   <item>Channel usage frequency per session.</item>
    ///   <item>Mute/unmute event count.</item>
    ///   <item>Average and peak concurrent speaker counts.</item>
    ///   <item>Codec and quality preference selections.</item>
    ///   <item>Spatial audio enabled/disabled.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class VoiceChatAnalytics : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static VoiceChatAnalytics Instance { get; private set; }
        #endregion

        #region Internal State
        private float _sessionStartTime = 0f;
        private bool  _sessionActive    = false;
        private int   _muteCount        = 0;
        private int   _channelSwitchCount = 0;
        private int   _peakSpeakers     = 0;
        private int   _currentSpeakers  = 0;

        private readonly Dictionary<VoiceChannel, float> _channelTotalTime
            = new Dictionary<VoiceChannel, float>();
        private VoiceChannel _currentChannel     = VoiceChannel.Proximity;
        private float        _channelSwitchTime  = 0f;

        private TelemetryDispatcher _dispatcher;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (VoiceChannel ch in Enum.GetValues(typeof(VoiceChannel)))
                _channelTotalTime[ch] = 0f;
        }

        private void Start()
        {
            _dispatcher = TelemetryDispatcher.Instance;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Records the start of a voice chat session.
        /// </summary>
        public void TrackSessionStart()
        {
            _sessionActive    = true;
            _sessionStartTime = Time.realtimeSinceStartup;
            _muteCount        = 0;
            _channelSwitchCount = 0;
            _peakSpeakers     = 0;
            _currentChannel   = VoiceChannel.Proximity;
            _channelSwitchTime = Time.realtimeSinceStartup;

            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_session_start")
                    .WithCategory("VoiceChat")
                    .Build());
        }

        /// <summary>
        /// Records the end of a voice chat session and submits summary metrics.
        /// </summary>
        public void TrackSessionEnd()
        {
            if (!_sessionActive) return;

            // Accrue time in current channel before ending
            AccrueChannelTime(_currentChannel);

            float duration = Time.realtimeSinceStartup - _sessionStartTime;
            _sessionActive = false;

            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_session_end")
                    .WithCategory("VoiceChat")
                    .WithProperty("duration_seconds", Mathf.RoundToInt(duration))
                    .WithProperty("mute_toggle_count", _muteCount)
                    .WithProperty("channel_switch_count", _channelSwitchCount)
                    .WithProperty("peak_speakers", _peakSpeakers)
                    .WithProperty("proximity_time", Mathf.RoundToInt(_channelTotalTime[VoiceChannel.Proximity]))
                    .WithProperty("team_time",      Mathf.RoundToInt(_channelTotalTime[VoiceChannel.Team]))
                    .WithProperty("global_time",    Mathf.RoundToInt(_channelTotalTime[VoiceChannel.Global]))
                    .WithProperty("private_time",   Mathf.RoundToInt(_channelTotalTime[VoiceChannel.Private]))
                    .WithProperty("atc_time",       Mathf.RoundToInt(_channelTotalTime[VoiceChannel.ATC]))
                    .Build());

            // Reset channel totals for next session
            foreach (VoiceChannel ch in Enum.GetValues(typeof(VoiceChannel)))
                _channelTotalTime[ch] = 0f;
        }

        /// <summary>
        /// Records a channel switch event.
        /// </summary>
        /// <param name="from">Channel being left.</param>
        /// <param name="to">Channel being joined.</param>
        public void TrackChannelSwitch(VoiceChannel from, VoiceChannel to)
        {
            AccrueChannelTime(from);
            _currentChannel    = to;
            _channelSwitchTime = Time.realtimeSinceStartup;
            _channelSwitchCount++;

            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_channel_switch")
                    .WithCategory("VoiceChat")
                    .WithProperty("from", from.ToString())
                    .WithProperty("to",   to.ToString())
                    .Build());
        }

        /// <summary>
        /// Records a mute or unmute toggle event.
        /// </summary>
        /// <param name="muted"><c>true</c> if the microphone was muted; <c>false</c> if unmuted.</param>
        public void TrackMuteToggle(bool muted)
        {
            _muteCount++;

            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_mute_toggle")
                    .WithCategory("VoiceChat")
                    .WithProperty("muted", muted)
                    .Build());
        }

        /// <summary>
        /// Records a voice quality (sample rate) change.
        /// </summary>
        /// <param name="quality">The newly selected quality tier.</param>
        public void TrackQualityChange(VoiceQuality quality)
        {
            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_quality_change")
                    .WithCategory("VoiceChat")
                    .WithProperty("quality", quality.ToString())
                    .Build());
        }

        /// <summary>
        /// Updates the current concurrent speaker count and refreshes the peak count.
        /// </summary>
        /// <param name="activeSpeakerCount">Number of participants currently speaking.</param>
        public void UpdateConcurrentSpeakers(int activeSpeakerCount)
        {
            _currentSpeakers = activeSpeakerCount;
            if (_currentSpeakers > _peakSpeakers)
                _peakSpeakers = _currentSpeakers;
        }

        /// <summary>
        /// Records that the spatial audio feature was toggled.
        /// </summary>
        /// <param name="enabled"><c>true</c> if spatial audio was enabled.</param>
        public void TrackSpatialAudioToggle(bool enabled)
        {
            if (_dispatcher == null) return;
            _dispatcher.EnqueueEvent(
                TelemetryEventBuilder.Create("voice_spatial_audio_toggle")
                    .WithCategory("VoiceChat")
                    .WithProperty("enabled", enabled)
                    .Build());
        }
        #endregion

        #region Private Helpers
        private void AccrueChannelTime(VoiceChannel channel)
        {
            float elapsed = Time.realtimeSinceStartup - _channelSwitchTime;
            _channelTotalTime[channel] += elapsed;
            _channelSwitchTime = Time.realtimeSinceStartup;
        }
        #endregion
    }
}
