using System;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour that simulates passenger reactions — dialogue bubbles,
    /// applause, screams, photography requests, sleep during cruise, and VIP
    /// special requests.
    /// </summary>
    public class PassengerBehaviorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("VIP Settings")]
        [SerializeField] private float vipAltitudeTolerance = 200f; // ±metres

        [Header("Sleep / Cruise")]
        [Tooltip("Minimum continuous smooth-flight seconds before passengers sleep.")]
        [SerializeField] private float sleepOnsetSeconds = 300f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when a passenger reacts.
        /// Parameters: reaction key (localisation), intensity 0–1.
        /// </summary>
        public event Action<string, float> OnPassengerReaction;

        // ── State ─────────────────────────────────────────────────────────────
        private PassengerComfortSystem _comfort;
        private float _smoothFlightTimer;
        private bool  _passengersAsleep;
        private float _lastComfortScore;

        private TransportContract _activeContract;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            _comfort = PassengerComfortSystem.Instance;
            if (_comfort != null)
            {
                _comfort.OnComfortChanged    += HandleComfortChanged;
                _comfort.OnPassengerComplaint += HandleComplaint;
                _comfort.OnComfortCritical    += HandleCritical;
            }

            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  += OnContractAccepted;
                manager.OnMissionCompleted += OnDeliveryComplete;
            }
        }

        private void OnDestroy()
        {
            if (_comfort != null)
            {
                _comfort.OnComfortChanged    -= HandleComfortChanged;
                _comfort.OnPassengerComplaint -= HandleComplaint;
                _comfort.OnComfortCritical    -= HandleCritical;
            }

            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  -= OnContractAccepted;
                manager.OnMissionCompleted -= OnDeliveryComplete;
            }
        }

        private void Update()
        {
            if (_activeContract == null) return;

            UpdateSleepState();
            CheckVIPRequests();
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void HandleComfortChanged(float score)
        {
            float delta = score - _lastComfortScore;
            _lastComfortScore = score;

            if (score >= 90f && delta > 0f)
            {
                // Passengers noticeably comfortable — quiet satisfaction.
                OnPassengerReaction?.Invoke("transport_reaction_satisfied", 0.3f);
            }
        }

        private void HandleComplaint(string key)
        {
            float intensity = key.Contains("distressed") ? 0.8f : 0.5f;
            OnPassengerReaction?.Invoke(key, intensity);
        }

        private void HandleCritical()
        {
            OnPassengerReaction?.Invoke("transport_reaction_panic", 1.0f);
        }

        private void UpdateSleepState()
        {
            if (_comfort == null) return;

            float score = _comfort.ComfortScore;
            if (score >= 70f)
            {
                _smoothFlightTimer += Time.deltaTime;
                if (!_passengersAsleep && _smoothFlightTimer >= sleepOnsetSeconds)
                {
                    _passengersAsleep = true;
                    OnPassengerReaction?.Invoke("transport_reaction_sleeping", 0.5f);
                }
            }
            else
            {
                _smoothFlightTimer = 0f;
                if (_passengersAsleep)
                {
                    _passengersAsleep = false;
                    OnPassengerReaction?.Invoke("transport_reaction_woken", 0.7f);
                }
            }
        }

        private void CheckVIPRequests()
        {
            if (_activeContract == null || _activeContract.passengerProfile.vipLevel < 2) return;

            var altitude = FindObjectOfType<SWEF.Flight.AltitudeController>();
            if (altitude == null) return;

            float preferred = _activeContract.passengerProfile.preferredAltitude;
            float current   = altitude.CurrentAltitudeMeters;

            if (Mathf.Abs(current - preferred) > vipAltitudeTolerance)
                OnPassengerReaction?.Invoke("transport_reaction_vip_altitude_request", 0.6f);
        }

        private void OnContractAccepted(TransportContract contract)
        {
            _activeContract    = contract;
            _smoothFlightTimer = 0f;
            _passengersAsleep  = false;
            _lastComfortScore  = 100f;
        }

        private void OnDeliveryComplete(DeliveryResult result)
        {
            _activeContract = null;

            // Applause on good delivery, grumbles on poor.
            string reactionKey = result.comfortScore >= 70f
                ? "transport_reaction_applause"
                : "transport_reaction_dissatisfied";
            float intensity = result.comfortScore / 100f;
            OnPassengerReaction?.Invoke(reactionKey, intensity);
        }
    }
}
