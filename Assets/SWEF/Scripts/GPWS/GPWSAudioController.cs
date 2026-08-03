using System.Collections.Generic;
using UnityEngine;

#if SWEF_AUDIO_AVAILABLE
using SWEF.Audio;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Manages GPWS aural alerts with a priority queue.
    ///
    /// <para>Plays synthetic voice callouts ("TERRAIN TERRAIN", "PULL UP",
    /// "SINK RATE", altitude advisories, etc.) on a dedicated AudioSource.
    /// Optionally forwards to <c>SWEF.Audio.AudioManager</c>
    /// (<c>#if SWEF_AUDIO_AVAILABLE</c>).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSAudioController : MonoBehaviour
    {
        #region Inspector

        [Header("Audio Clips — Warnings")]
        [SerializeField] private AudioClip clipTerrain;
        [SerializeField] private AudioClip clipPullUp;
        [SerializeField] private AudioClip clipTooLowTerrain;
        [SerializeField] private AudioClip clipTooLowGear;
        [SerializeField] private AudioClip clipTooLowFlaps;
        [SerializeField] private AudioClip clipSinkRate;
        [SerializeField] private AudioClip clipDontSink;
        [SerializeField] private AudioClip clipGlideslope;
        [SerializeField] private AudioClip clipWindshear;

        [Header("Audio Clips — Advisory Callouts")]
        [SerializeField] private AudioClip clipMinimums;
        [SerializeField] private AudioClip clipHundredAbove;
        [SerializeField] private AudioClip clip500;
        [SerializeField] private AudioClip clip400;
        [SerializeField] private AudioClip clip300;
        [SerializeField] private AudioClip clip200;
        [SerializeField] private AudioClip clip100;
        [SerializeField] private AudioClip clip50;
        [SerializeField] private AudioClip clip40;
        [SerializeField] private AudioClip clip30;
        [SerializeField] private AudioClip clip20;
        [SerializeField] private AudioClip clip10;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float minRepeatInterval = 1.5f;

        #endregion

        #region Private State

        private AudioSource _source;
        private GPWSManager _mgr;
        private readonly Queue<AudioClip> _queue = new Queue<AudioClip>();
        private float _lastPlayTime;
        private readonly Dictionary<string, float> _lastPlayedByKey = new Dictionary<string, float>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = volume;
        }

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            if (_mgr == null) return;

            _mgr.OnGPWSAlert += HandleAlert;
            _mgr.OnAltitudeCallout += HandleCallout;
            _mgr.OnWindshearDetected += HandleWindshear;
        }

        private void Update()
        {
            if (_source.isPlaying || _queue.Count == 0) return;
            if (Time.time - _lastPlayTime < minRepeatInterval) return;

            var clip = _queue.Dequeue();
            PlayClip(clip);
        }

        private void OnDestroy()
        {
            if (_mgr != null)
            {
                _mgr.OnGPWSAlert -= HandleAlert;
                _mgr.OnAltitudeCallout -= HandleCallout;
                _mgr.OnWindshearDetected -= HandleWindshear;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleAlert(GPWSAlert alert)
        {
            AudioClip clip = ResolveAlertClip(alert.messageKey);
            if (clip == null) return;

            if (alert.level == GPWSAlertLevel.PullUp)
            {
                _queue.Clear();
                EnqueueClip(clip, alert.messageKey);
            }
            else
            {
                EnqueueClip(clip, alert.messageKey);
            }
        }

        private void HandleCallout(GPWSCalloutType callout)
        {
            AudioClip clip = ResolveCalloutClip(callout);
            if (clip != null)
                EnqueueClip(clip, callout.ToString());
        }

        private void HandleWindshear(float intensityKts)
        {
            if (clipWindshear != null)
            {
                _queue.Clear();
                EnqueueClip(clipWindshear, "windshear");
            }
        }

        #endregion

        #region Clip Resolution

        private AudioClip ResolveAlertClip(string messageKey) => messageKey switch
        {
            "gpws_pull_up"          => clipPullUp,
            "gpws_terrain_pull_up"  => clipPullUp,
            "gpws_terrain"          => clipTerrain,
            "gpws_sink_rate"        => clipSinkRate,
            "gpws_dont_sink"        => clipDontSink,
            "gpws_too_low_terrain"  => clipTooLowTerrain,
            "gpws_too_low_gear"     => clipTooLowGear,
            "gpws_too_low_flaps"    => clipTooLowFlaps,
            "gpws_glideslope"       => clipGlideslope,
            "gpws_windshear"        => clipWindshear,
            _                       => null
        };

        private AudioClip ResolveCalloutClip(GPWSCalloutType callout) => callout switch
        {
            GPWSCalloutType.Minimums     => clipMinimums,
            GPWSCalloutType.HundredAbove => clipHundredAbove,
            GPWSCalloutType.FiveHundred  => clip500,
            GPWSCalloutType.FourHundred  => clip400,
            GPWSCalloutType.ThreeHundred => clip300,
            GPWSCalloutType.TwoHundred   => clip200,
            GPWSCalloutType.OneHundred   => clip100,
            GPWSCalloutType.Fifty        => clip50,
            GPWSCalloutType.Forty        => clip40,
            GPWSCalloutType.Thirty       => clip30,
            GPWSCalloutType.Twenty       => clip20,
            GPWSCalloutType.Ten          => clip10,
            _                            => null
        };

        #endregion

        #region Playback

        private void EnqueueClip(AudioClip clip, string key)
        {
            if (_lastPlayedByKey.TryGetValue(key, out float lastTime))
            {
                if (Time.time - lastTime < minRepeatInterval) return;
            }
            _queue.Enqueue(clip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            _source.clip = clip;
            _source.volume = volume;
            _source.Play();
            _lastPlayTime = Time.time;
        }

        #endregion
    }
}
