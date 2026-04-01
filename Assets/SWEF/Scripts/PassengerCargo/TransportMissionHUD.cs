using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour HUD for active transport missions.
    ///
    /// Displays:
    ///   • Contract summary card (origin → destination, payload icon, timer)
    ///   • Comfort meter (colour-coded bar with state label)
    ///   • Cargo integrity bar (cargo missions only)
    ///   • Passenger count / weight indicator
    ///   • Delivery timer with phase colours
    ///   • Passenger reaction toast notifications
    /// </summary>
    public class TransportMissionHUD : MonoBehaviour
    {
        // ── Inspector bindings (assign in Unity Editor) ───────────────────────
        [Header("Root Panel")]
        [SerializeField] private GameObject hudPanel;

        [Header("Contract Card")]
        [SerializeField] private Text   lblOrigin;
        [SerializeField] private Text   lblDestination;
        [SerializeField] private Text   lblMissionType;
        [SerializeField] private Text   lblPassengerCount;
        [SerializeField] private Text   lblCargoWeight;

        [Header("Comfort Meter")]
        [SerializeField] private Slider comfortBar;
        [SerializeField] private Text   lblComfortLevel;
        [SerializeField] private Image  comfortBarFill;
        [SerializeField] private Color  colorExcellent  = new Color(0.20f, 0.85f, 0.40f);
        [SerializeField] private Color  colorGood       = new Color(0.40f, 0.80f, 0.20f);
        [SerializeField] private Color  colorFair       = new Color(1.00f, 0.85f, 0.10f);
        [SerializeField] private Color  colorPoor       = new Color(1.00f, 0.50f, 0.10f);
        [SerializeField] private Color  colorCritical   = new Color(0.90f, 0.10f, 0.10f);

        [Header("Cargo Integrity")]
        [SerializeField] private GameObject cargoSection;
        [SerializeField] private Slider     cargoIntegrityBar;
        [SerializeField] private Text       lblCargoStatus;

        [Header("Delivery Timer")]
        [SerializeField] private Text  lblTimer;
        [SerializeField] private Color colorGreen    = new Color(0.20f, 0.85f, 0.40f);
        [SerializeField] private Color colorYellow   = new Color(1.00f, 0.85f, 0.10f);
        [SerializeField] private Color colorRed      = new Color(0.90f, 0.10f, 0.10f);
        [SerializeField] private Color colorOvertime = new Color(0.60f, 0.00f, 0.80f);

        [Header("Toast Notifications")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text       lblToast;
        [SerializeField] private float      toastDuration = 3f;

        // ── State ─────────────────────────────────────────────────────────────
        private float _toastTimer;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            SetHUDVisible(false);

            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  += ShowForContract;
                manager.OnMissionCompleted += OnComplete;
                manager.OnMissionFailed    += HideHUD;
                manager.OnMissionAbandoned += HideHUD;
            }

            if (PassengerComfortSystem.Instance != null)
                PassengerComfortSystem.Instance.OnComfortChanged += RefreshComfort;

            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.OnCargoDamaged += RefreshCargoIntegrity;

            if (DeliveryTimerController.Instance != null)
            {
                DeliveryTimerController.Instance.OnTimerPhaseChanged += RefreshTimerColor;
            }

            var behavior = FindObjectOfType<PassengerBehaviorController>();
            if (behavior != null)
                behavior.OnPassengerReaction += ShowToast;
        }

        private void OnDestroy()
        {
            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  -= ShowForContract;
                manager.OnMissionCompleted -= OnComplete;
                manager.OnMissionFailed    -= HideHUD;
                manager.OnMissionAbandoned -= HideHUD;
            }

            if (PassengerComfortSystem.Instance != null)
                PassengerComfortSystem.Instance.OnComfortChanged -= RefreshComfort;

            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.OnCargoDamaged -= RefreshCargoIntegrity;
        }

        private void Update()
        {
            if (hudPanel == null || !hudPanel.activeSelf) return;

            RefreshTimer();
            RefreshToast();
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void ShowForContract(TransportContract contract)
        {
            SetHUDVisible(true);

            if (lblOrigin)      lblOrigin.text      = contract.origin;
            if (lblDestination) lblDestination.text  = contract.destination;
            if (lblMissionType) lblMissionType.text  = contract.missionType.ToString();

            bool isPax   = contract.missionType == MissionType.PassengerStandard
                        || contract.missionType == MissionType.PassengerVIP
                        || contract.missionType == MissionType.PassengerCharter;

            if (lblPassengerCount)
                lblPassengerCount.text = isPax
                    ? contract.passengerProfile.passengerCount.ToString()
                    : "—";

            if (lblCargoWeight)
                lblCargoWeight.text = !isPax
                    ? $"{contract.cargoManifest.weight:F0} kg"
                    : "—";

            if (cargoSection)
                cargoSection.SetActive(!isPax);
        }

        private void RefreshComfort(float score)
        {
            if (comfortBar != null) comfortBar.value = score / 100f;

            ComfortLevel level = PassengerComfortSystem.ScoreToLevel(score);
            if (lblComfortLevel) lblComfortLevel.text = level.ToString();

            if (comfortBarFill != null)
                comfortBarFill.color = LevelToColor(level);
        }

        private void RefreshCargoIntegrity(float damagePercent)
        {
            if (cargoIntegrityBar != null)
                cargoIntegrityBar.value = 1f - (damagePercent / 100f);

            if (lblCargoStatus)
                lblCargoStatus.text = damagePercent < 50f ? "OK" : "⚠ Damaged";
        }

        private void RefreshTimer()
        {
            if (lblTimer == null || DeliveryTimerController.Instance == null) return;

            float t = DeliveryTimerController.Instance.TimeRemainingSeconds;
            if (t >= 0f)
            {
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                lblTimer.text = $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                lblTimer.text = $"OT {Mathf.Abs(t):F0}s";
            }
        }

        private void RefreshTimerColor(TimerPhase phase)
        {
            if (lblTimer == null) return;
            lblTimer.color = phase switch
            {
                TimerPhase.Green    => colorGreen,
                TimerPhase.Yellow   => colorYellow,
                TimerPhase.Red      => colorRed,
                TimerPhase.Overtime => colorOvertime,
                _                   => Color.white
            };
        }

        private void ShowToast(string reactionKey, float intensity)
        {
            if (toastPanel == null) return;
            if (lblToast != null) lblToast.text = reactionKey;
            toastPanel.SetActive(true);
            _toastTimer = toastDuration;
        }

        private void RefreshToast()
        {
            if (toastPanel == null || !toastPanel.activeSelf) return;
            _toastTimer -= Time.deltaTime;
            if (_toastTimer <= 0f) toastPanel.SetActive(false);
        }

        private void OnComplete(DeliveryResult _) => HideHUD();
        private void HideHUD() => SetHUDVisible(false);
        private void SetHUDVisible(bool v) { if (hudPanel != null) hudPanel.SetActive(v); }

        private Color LevelToColor(ComfortLevel level) => level switch
        {
            ComfortLevel.Excellent => colorExcellent,
            ComfortLevel.Good      => colorGood,
            ComfortLevel.Fair      => colorFair,
            ComfortLevel.Poor      => colorPoor,
            _                      => colorCritical
        };
    }
}
