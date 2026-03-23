// CelebrationVFXController.cs — SWEF Particle Effects & VFX System
using System.Collections;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Plays celebratory visual effects in response to in-game achievements, milestones,
    /// record-breaking moments, and multiplayer events.
    ///
    /// <para>Effects provided:</para>
    /// <list type="bullet">
    /// <item><description>Firework burst on achievement unlock.</description></item>
    /// <item><description>Confetti shower on milestone completion.</description></item>
    /// <item><description>Altitude glow ring on record-breaking.</description></item>
    /// <item><description>Race finish sparkle trail.</description></item>
    /// <item><description>Formation perfect-score radial burst.</description></item>
    /// </list>
    ///
    /// <para>Listens to <c>AchievementManager.OnAchievementUnlocked</c> and
    /// <c>ProgressionManager.OnMilestoneReached</c> when the respective compile
    /// symbols are defined.</para>
    /// </summary>
    public sealed class CelebrationVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Particle Systems")]
        [Tooltip("Firework burst played on achievement unlock.")]
        [SerializeField] private ParticleSystem fireworkSystem;

        [Tooltip("Confetti shower played on milestone completion.")]
        [SerializeField] private ParticleSystem confettiSystem;

        [Tooltip("Glow ring effect played on altitude or score records.")]
        [SerializeField] private ParticleSystem glowRingSystem;

        [Tooltip("Sparkle trail played at the end of a race.")]
        [SerializeField] private ParticleSystem sparkleTrailSystem;

        [Tooltip("Radial burst played on a perfect formation score.")]
        [SerializeField] private ParticleSystem formationBurstSystem;

        [Header("Firework Colours")]
        [Tooltip("Colours cycled through successive firework bursts.")]
        [SerializeField] private Color[] fireworkColors = {
            new Color(1f, 0.85f, 0f),
            new Color(0f, 0.7f, 1f),
            new Color(1f, 0.2f, 0.5f),
            new Color(0.3f, 1f, 0.4f)
        };

        [Header("Timing")]
        [Tooltip("Duration of the confetti shower in seconds.")]
        [SerializeField, Min(0.1f)] private float confettiDuration = 4f;

        [Tooltip("Duration of the sparkle trail in seconds.")]
        [SerializeField, Min(0.1f)] private float sparkleTrailDuration = 3f;

        [Tooltip("Interval between successive firework bursts during a celebration sequence.")]
        [SerializeField, Min(0.1f)] private float fireworkBurstInterval = 0.6f;

        [Tooltip("Number of firework bursts in a full celebration sequence.")]
        [SerializeField, Min(1)] private int fireworkBurstCount = 5;

        // ── Private State ─────────────────────────────────────────────────────────

        private int _fireworkColorIndex;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()  => SubscribeEvents();
        private void OnDisable() => UnsubscribeEvents();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Plays a firework celebration sequence at the given world position.</summary>
        /// <param name="position">World-space position for the fireworks.</param>
        public void PlayFireworks(Vector3 position) => StartCoroutine(FireworkSequence(position));

        /// <summary>Plays a confetti shower effect.</summary>
        public void PlayConfetti()
        {
            if (confettiSystem == null) return;
            confettiSystem.transform.position = transform.position;
            confettiSystem.Play(withChildren: true);
            StartCoroutine(StopAfter(confettiSystem, confettiDuration));
        }

        /// <summary>Plays the altitude glow ring effect at the given position.</summary>
        /// <param name="position">World-space position for the glow ring.</param>
        public void PlayGlowRing(Vector3 position)
        {
            if (glowRingSystem == null) return;
            glowRingSystem.transform.position = position;
            glowRingSystem.Play(withChildren: true);
        }

        /// <summary>Plays the race finish sparkle trail effect.</summary>
        public void PlayRaceFinish()
        {
            if (sparkleTrailSystem == null) return;
            sparkleTrailSystem.Play(withChildren: true);
            StartCoroutine(StopAfter(sparkleTrailSystem, sparkleTrailDuration));
        }

        /// <summary>Plays a perfect formation score radial burst.</summary>
        public void PlayFormationBurst()
        {
            if (formationBurstSystem == null) return;
            formationBurstSystem.Play(withChildren: true);
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator FireworkSequence(Vector3 position)
        {
            for (int i = 0; i < fireworkBurstCount; i++)
            {
                if (fireworkSystem != null)
                {
                    fireworkSystem.transform.position = position + Random.insideUnitSphere * 5f;
                    SetParticleColor(fireworkSystem, NextFireworkColor());
                    fireworkSystem.Play(withChildren: true);
                }

                if (VFXPoolManager.Instance != null)
                    VFXPoolManager.Instance.Spawn(VFXSpawnRequest.At(VFXType.Firework, position));

                yield return new WaitForSeconds(fireworkBurstInterval);
            }
        }

        private static IEnumerator StopAfter(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null) ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
        }

        private Color NextFireworkColor()
        {
            if (fireworkColors == null || fireworkColors.Length == 0) return Color.white;
            Color col = fireworkColors[_fireworkColorIndex % fireworkColors.Length];
            _fireworkColorIndex++;
            return col;
        }

        private static void SetParticleColor(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startColor = color;
        }

        // ── Event Integration ─────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (SWEF.Achievement.AchievementManager.Instance != null)
                SWEF.Achievement.AchievementManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
#endif
#if SWEF_PROGRESSION_AVAILABLE
            if (SWEF.Progression.ProgressionManager.Instance != null)
                SWEF.Progression.ProgressionManager.Instance.OnMilestoneReached += HandleMilestoneReached;
#endif
        }

        private void UnsubscribeEvents()
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (SWEF.Achievement.AchievementManager.Instance != null)
                SWEF.Achievement.AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
#endif
#if SWEF_PROGRESSION_AVAILABLE
            if (SWEF.Progression.ProgressionManager.Instance != null)
                SWEF.Progression.ProgressionManager.Instance.OnMilestoneReached -= HandleMilestoneReached;
#endif
        }

#if SWEF_ACHIEVEMENT_AVAILABLE
        private void HandleAchievementUnlocked(SWEF.Achievement.AchievementData achievement)
        {
            PlayFireworks(transform.position);
        }
#endif

#if SWEF_PROGRESSION_AVAILABLE
        private void HandleMilestoneReached(string milestoneId)
        {
            PlayConfetti();
        }
#endif

#if UNITY_EDITOR
        [ContextMenu("Test Fireworks")]
        private void EditorTestFireworks() => PlayFireworks(transform.position);

        [ContextMenu("Test Confetti")]
        private void EditorTestConfetti() => PlayConfetti();

        [ContextMenu("Test Glow Ring")]
        private void EditorTestGlowRing() => PlayGlowRing(transform.position);

        [ContextMenu("Test Race Finish")]
        private void EditorTestRaceFinish() => PlayRaceFinish();

        [ContextMenu("Test Formation Burst")]
        private void EditorTestFormationBurst() => PlayFormationBurst();
#endif
    }
}
