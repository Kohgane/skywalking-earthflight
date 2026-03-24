// WaterInteractionAnalytics.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 74 — Tracks water interaction telemetry and forwards events to
    /// <c>SWEF.Analytics.UserBehaviorTracker</c> (null-safe).
    ///
    /// <para>Subscribes to <see cref="BuoyancyController"/>, <see cref="WaterSurfaceManager"/>,
    /// and <see cref="UnderwaterCameraTransition"/> events and accumulates session metrics
    /// for flush on <see cref="OnDestroy"/>.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class WaterInteractionAnalytics : MonoBehaviour
    {
        #region Private State

        // Session accumulators
        private bool _waterContactTracked;
        private float _skimStartTime;
        private float _totalSkimDuration;
        private float _floatStartTime;
        private float _totalFloatDuration;
        private float _submersionStartTime;
        private float _maxSubmersionDepth;
        private bool _isSubmerged;
        private bool _isFloating;
        private bool _isSkimming;

        private readonly Dictionary<WaterBodyType, float> _bodyTypeTime =
            new Dictionary<WaterBodyType, float>();
        private WaterBodyType _currentBodyType;
        private float _bodyTypeStartTime;
        private bool _overWater;

        private readonly Dictionary<SplashType, int> _splashCounts =
            new Dictionary<SplashType, int>();

        private bool _ditchingStarted;

        // Component references
        private BuoyancyController _buoyancy;
        private UnderwaterCameraTransition _underwaterCam;

        // Null-safe analytics tracker
        private Component _tracker;
        private bool _crossSystemCacheDone;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _buoyancy = GetComponent<BuoyancyController>();
            _underwaterCam = GetComponentInChildren<UnderwaterCameraTransition>()
                             ?? FindObjectOfType<UnderwaterCameraTransition>();

            if (_buoyancy != null)
            {
                _buoyancy.OnWaterContact += OnWaterContact;
                _buoyancy.OnStateChanged += OnStateChanged;
                _buoyancy.OnDitchingComplete += OnDitchingComplete;
            }

            if (_underwaterCam != null)
            {
                _underwaterCam.OnSubmerged   += OnSubmerged;
                _underwaterCam.OnSurfaced    += OnSurfaced;
            }

            if (WaterSurfaceManager.Instance != null)
            {
                WaterSurfaceManager.Instance.OnWaterDetected += OnWaterDetected;
                WaterSurfaceManager.Instance.OnWaterLost     += OnWaterLost;
            }

            foreach (WaterBodyType bt in Enum.GetValues(typeof(WaterBodyType)))
                _bodyTypeTime[bt] = 0f;
            foreach (SplashType st in Enum.GetValues(typeof(SplashType)))
                _splashCounts[st] = 0;

            CacheCrossSystemReferences();
        }

        private void Update()
        {
            if (_overWater)
                _bodyTypeTime[_currentBodyType] += Time.deltaTime;

            if (_isSkimming)
                _totalSkimDuration += Time.deltaTime;

            if (_isFloating)
                _totalFloatDuration += Time.deltaTime;

            if (_isSubmerged && _underwaterCam != null)
                _maxSubmersionDepth = Mathf.Max(_maxSubmersionDepth, _underwaterCam.CurrentDepth);
        }

        private void OnDestroy()
        {
            // Flush session summary
            FlushSessionSummary();

            // Unsubscribe
            if (_buoyancy != null)
            {
                _buoyancy.OnWaterContact     -= OnWaterContact;
                _buoyancy.OnStateChanged     -= OnStateChanged;
                _buoyancy.OnDitchingComplete -= OnDitchingComplete;
            }

            if (_underwaterCam != null)
            {
                _underwaterCam.OnSubmerged -= OnSubmerged;
                _underwaterCam.OnSurfaced  -= OnSurfaced;
            }

            if (WaterSurfaceManager.Instance != null)
            {
                WaterSurfaceManager.Instance.OnWaterDetected -= OnWaterDetected;
                WaterSurfaceManager.Instance.OnWaterLost     -= OnWaterLost;
            }
        }

        #endregion

        #region Event Handlers

        private void OnWaterContact(SplashEvent evt)
        {
            _splashCounts[evt.type]++;

            if (!_waterContactTracked)
            {
                _waterContactTracked = true;
                Track("water_contact", new Dictionary<string, object>
                {
                    { "splash_type",     evt.type.ToString() },
                    { "speed",           evt.velocity.magnitude },
                    { "impact_force",    evt.impactForce },
                    { "position_x",      Mathf.RoundToInt(evt.position.x) },
                    { "position_z",      Mathf.RoundToInt(evt.position.z) },
                });
            }
        }

        private void OnStateChanged(WaterContactState state)
        {
            _isSkimming  = state == WaterContactState.Skimming;
            _isFloating  = state == WaterContactState.Floating;

            if (state == WaterContactState.Ditching && !_ditchingStarted)
                _ditchingStarted = true;

            if (state == WaterContactState.Sinking)
            {
                Track("water_sinking", new Dictionary<string, object>
                {
                    { "submersion_depth", _buoyancy != null ? _buoyancy.State.submersionDepth : 0f },
                    { "time_in_water",    _buoyancy != null ? _buoyancy.State.timeInWater : 0f },
                });
            }
        }

        private void OnDitchingComplete()
        {
            Track("water_ditching", new Dictionary<string, object>
            {
                { "success",         true },
                { "buoyancy_force",  _buoyancy != null ? _buoyancy.State.buoyancyForceMagnitude : 0f },
            });
            _ditchingStarted = false;
        }

        private void OnSubmerged(UnderwaterZone zone)
        {
            _isSubmerged         = true;
            _submersionStartTime = Time.time;
            _maxSubmersionDepth  = 0f;
        }

        private void OnSurfaced()
        {
            if (_isSubmerged)
            {
                float duration = Time.time - _submersionStartTime;
                Track("water_submersion", new Dictionary<string, object>
                {
                    { "max_depth", _maxSubmersionDepth },
                    { "duration",  duration },
                });
            }
            _isSubmerged = false;
        }

        private void OnWaterDetected(WaterBodyType bodyType)
        {
            _overWater            = true;
            _currentBodyType      = bodyType;
            _bodyTypeStartTime    = Time.time;
        }

        private void OnWaterLost()
        {
            _overWater = false;
        }

        #endregion

        #region Session Flush

        private void FlushSessionSummary()
        {
            if (_totalSkimDuration > 0f)
            {
                Track("water_skim_duration", new Dictionary<string, object>
                {
                    { "total_seconds", _totalSkimDuration },
                });
            }

            if (_totalFloatDuration > 0f)
            {
                Track("water_floating_duration", new Dictionary<string, object>
                {
                    { "total_seconds", _totalFloatDuration },
                });
            }

            // Body type distribution
            var bodyDist = new Dictionary<string, object>();
            foreach (var kv in _bodyTypeTime)
                if (kv.Value > 0f) bodyDist[kv.Key.ToString()] = kv.Value;
            if (bodyDist.Count > 0)
                Track("water_body_type_distribution", bodyDist);

            // Splash counts
            var splashData = new Dictionary<string, object>();
            foreach (var kv in _splashCounts)
                if (kv.Value > 0) splashData[kv.Key.ToString()] = kv.Value;
            if (splashData.Count > 0)
                Track("water_splash_count", splashData);
        }

        #endregion

        #region Analytics Bridge

        /// <summary>Notifies the analytics system that an underwater screenshot was taken.</summary>
        public void TrackUnderwaterPhoto()
        {
            if (_underwaterCam != null && _underwaterCam.IsUnderwater)
            {
                Track("water_photo_underwater", new Dictionary<string, object>
                {
                    { "depth", _underwaterCam.CurrentDepth },
                    { "zone",  _underwaterCam.CurrentZone.ToString() },
                });
            }
        }

        private void Track(string eventName, Dictionary<string, object> parameters)
        {
            if (_tracker == null) return;
            try
            {
                var method = _tracker.GetType().GetMethod("Track")
                             ?? _tracker.GetType().GetMethod("LogEvent");
                method?.Invoke(_tracker, new object[] { eventName, parameters });
            }
            catch { }
        }

        private void CacheCrossSystemReferences()
        {
            _crossSystemCacheDone = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var trackerType = assembly.GetType("SWEF.Analytics.UserBehaviorTracker");
                if (trackerType != null)
                {
                    _tracker = FindObjectOfType(trackerType) as Component;
                    if (_tracker != null) break;
                }
            }
        }

        #endregion
    }
}
