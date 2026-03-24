// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowAudienceSystem.cs
using System;
using UnityEngine;

namespace SWEF.Airshow
{
    /// <summary>
    /// Simulates crowd/audience reactions during an airshow performance.
    /// Drives excitement (0–100), audio reactions, camera shake, and confetti effects.
    /// Subscribes to <see cref="AirshowManager"/> events automatically.
    /// </summary>
    public class AirshowAudienceSystem : MonoBehaviour
    {
        #region Inspector
        [Header("Audio Clips (optional)")]
        [SerializeField] private AudioClip crowdMurmur;
        [SerializeField] private AudioClip crowdCheer;
        [SerializeField] private AudioClip crowdGasp;
        [SerializeField] private AudioClip crowdApplause;
        [SerializeField] private AudioClip standingOvation;

        [Header("Visual")]
        [SerializeField] private ParticleSystem confettiSystem;

        [Header("Excitement Settings")]
        [SerializeField] private float decayPerSecond = 2f;        // excitement lost per second of silence
        [SerializeField] private float maxExcitement  = 100f;
        #endregion

        #region Public State
        /// <summary>Current crowd excitement level, 0–100.</summary>
        public float CurrentExcitement { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when the excitement value changes significantly.</summary>
        public event Action<float> OnExcitementChanged;

        /// <summary>Fired with a label when the crowd reacts (cheer, gasp, applause, ovation).</summary>
        public event Action<string> OnCrowdReaction;
        #endregion

        #region Private
        private AudioSource _audioSource;
        private float _lastExcitement;
        private float _reactionCooldown;
        private const float ReactionCooldownTime = 2f;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnManeuverTriggered += HandleManeuverTriggered;
            AirshowManager.Instance.OnActStarted        += HandleActStarted;
            AirshowManager.Instance.OnAirshowCompleted  += HandleAirshowCompleted;
        }

        private void OnDisable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnManeuverTriggered -= HandleManeuverTriggered;
            AirshowManager.Instance.OnActStarted        -= HandleActStarted;
            AirshowManager.Instance.OnAirshowCompleted  -= HandleAirshowCompleted;
        }

        private void Update()
        {
            DecayExcitement();
            _reactionCooldown -= Time.deltaTime;

            if (Mathf.Abs(CurrentExcitement - _lastExcitement) > 1f)
            {
                OnExcitementChanged?.Invoke(CurrentExcitement);
                _lastExcitement = CurrentExcitement;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Adds excitement from a named event type (e.g., "flyby", "crossover").</summary>
        public void AddExcitement(float amount, string reactionLabel)
        {
            CurrentExcitement = Mathf.Clamp(CurrentExcitement + amount, 0f, maxExcitement);

            if (_reactionCooldown <= 0f)
            {
                TriggerAudioReaction(reactionLabel);
                _reactionCooldown = ReactionCooldownTime;
                OnCrowdReaction?.Invoke(reactionLabel);
            }
        }

        // ── Private event handlers ───────────────────────────────────────────

        private void HandleManeuverTriggered(ManeuverType maneuver, int slot)
        {
            float excitement = ManeuverExcitement(maneuver);
            string label = excitement >= 20f ? "gasp" : "cheer";
            AddExcitement(excitement, label);
        }

        private void HandleActStarted(int actIndex, string actName)
        {
            AddExcitement(5f, "applause");
        }

        private void HandleAirshowCompleted(AirshowResult result)
        {
            PlayClip(crowdApplause);
            OnCrowdReaction?.Invoke("applause");

            if (CurrentExcitement >= 80f)
            {
                PlayClip(standingOvation);
                OnCrowdReaction?.Invoke("ovation");
                TriggerConfetti();
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void DecayExcitement()
        {
            if (CurrentExcitement > 0f)
                CurrentExcitement = Mathf.Max(0f, CurrentExcitement - decayPerSecond * Time.deltaTime);
        }

        private void TriggerAudioReaction(string label)
        {
            switch (label)
            {
                case "cheer":    PlayClip(crowdCheer);    break;
                case "gasp":     PlayClip(crowdGasp);     break;
                case "applause": PlayClip(crowdApplause); break;
                default:         PlayClip(crowdMurmur);   break;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip);
        }

        private void TriggerConfetti()
        {
            if (confettiSystem != null)
                confettiSystem.Play();
        }

        private static float ManeuverExcitement(ManeuverType maneuver)
        {
            return maneuver switch
            {
                ManeuverType.BombBurst        => 25f,
                ManeuverType.CrossOver        => 25f,
                ManeuverType.DeltaPass        => 15f,
                ManeuverType.LoopTheLoop      => 15f,
                ManeuverType.SpiralClimb      => 12f,
                ManeuverType.SynchroMirror    => 20f,
                ManeuverType.DiamondRoll      => 18f,
                ManeuverType.HeartShape       => 8f,
                ManeuverType.FormationBreak   => 10f,
                ManeuverType.InvertedFlight   => 12f,
                ManeuverType.BarrelRoll       => 10f,
                ManeuverType.CubanEight       => 14f,
                ManeuverType.KnifeEdge        => 12f,
                ManeuverType.Hammerhead       => 8f,
                ManeuverType.Immelmann        => 7f,
                ManeuverType.TailSlide        => 10f,
                ManeuverType.SplitS           => 7f,
                _                             => 5f
            };
        }
    }
}
