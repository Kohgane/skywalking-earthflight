// RacingData.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Racing
{
    #region Enumerations

    /// <summary>Classifies the source of a speed boost applied to the player.</summary>
    public enum BoostType
    {
        /// <summary>Triggered by rolling over a world-placed boost pad.</summary>
        BoostPad,
        /// <summary>Granted when the player releases a charged drift.</summary>
        DriftBoost,
        /// <summary>Earned by drafting in another player's slipstream.</summary>
        SlipstreamBoost,
        /// <summary>Manually activated nitro charge.</summary>
        NitroBoost,
        /// <summary>Perfect-timing boost at race start.</summary>
        StartBoost,
        /// <summary>Short boost from a single drift release (1 s charge).</summary>
        MiniTurbo,
        /// <summary>Medium boost from an orange-level drift release (2.5 s charge).</summary>
        SuperMiniTurbo,
        /// <summary>Strongest drift-release boost (6 s UltraPurple charge).</summary>
        UltraMiniTurbo,
        /// <summary>Boost earned by landing an airborne trick.</summary>
        TrickBoost,
        /// <summary>Single-use mushroom-style speed burst.</summary>
        MushroomBoost
    }

    /// <summary>Charge level reached during a drift — determines the boost reward on release.</summary>
    public enum DriftLevel
    {
        /// <summary>No active drift charge.</summary>
        None,
        /// <summary>First charge level — reached after 1 second of drifting.</summary>
        Blue,
        /// <summary>Second charge level — reached after 2.5 seconds.</summary>
        Orange,
        /// <summary>Third charge level — reached after 4 seconds.</summary>
        Purple,
        /// <summary>Maximum charge level — reached after 6 seconds.</summary>
        UltraPurple
    }

    /// <summary>Direction the player locked into at drift initiation.</summary>
    public enum DriftDirection
    {
        /// <summary>No drift is active.</summary>
        None,
        /// <summary>Drifting towards the left.</summary>
        Left,
        /// <summary>Drifting towards the right.</summary>
        Right
    }

    /// <summary>Visual style category for a world-placed boost pad.</summary>
    public enum BoostPadStyle
    {
        /// <summary>Standard flat ground pad.</summary>
        Ground,
        /// <summary>Elevated ring the aircraft flies through.</summary>
        AerialRing,
        /// <summary>Wall-mounted side booster.</summary>
        WallMount,
        /// <summary>Downward-angled boost pad for dive acceleration.</summary>
        Dive
    }

    /// <summary>VFX category tag used by <see cref="BoostVFXBridge"/> to select particle systems.</summary>
    public enum BoostVFXType
    {
        /// <summary>No VFX emitted.</summary>
        None,
        /// <summary>Short forward burst particles.</summary>
        ShortBurst,
        /// <summary>Sustained exhaust flame stream.</summary>
        ExhaustFlame,
        /// <summary>Wind-tunnel slipstream effect.</summary>
        WindTunnel,
        /// <summary>Smoke and flame for a perfect start boost.</summary>
        StartFlame,
        /// <summary>Ribbon trails for aerial tricks.</summary>
        TrickRibbon,
        /// <summary>Mushroom-style shockwave burst.</summary>
        MushroomShockwave
    }

    /// <summary>Grade awarded for the race-start timing input.</summary>
    public enum StartBoostGrade
    {
        /// <summary>Held at exactly the right moment — maximum boost.</summary>
        Perfect,
        /// <summary>Near-perfect timing — standard boost.</summary>
        Good,
        /// <summary>Slightly late — small boost.</summary>
        Ok,
        /// <summary>Too late or no input — no boost.</summary>
        Miss,
        /// <summary>Input too early — 0.5 s engine-stall penalty.</summary>
        Stall
    }

    /// <summary>Aerial trick types that can be performed when airborne.</summary>
    public enum TrickType
    {
        /// <summary>No trick active.</summary>
        None,
        /// <summary>Barrel roll to the left.</summary>
        BarrelRollLeft,
        /// <summary>Barrel roll to the right.</summary>
        BarrelRollRight,
        /// <summary>Forward flip (nose down).</summary>
        FrontFlip,
        /// <summary>Backward flip (nose up).</summary>
        BackFlip,
        /// <summary>Single full horizontal spin (360°).</summary>
        Spin360,
        /// <summary>Double horizontal spin (720°).</summary>
        Spin720
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// ScriptableObject that configures the behaviour of a single boost type.
    /// Create via <em>SWEF &gt; Racing &gt; Boost Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Racing/Boost Config", fileName = "BoostConfig")]
    public class BoostConfig : ScriptableObject
    {
        /// <summary>Boost category this config describes.</summary>
        [Tooltip("Boost category this config describes.")]
        public BoostType boostType = BoostType.BoostPad;

        /// <summary>Multiplier applied to the player's base speed (1.0 = no change).</summary>
        [Tooltip("Speed multiplier applied on top of the player's base speed.")]
        [Range(1f, 5f)]
        public float speedMultiplier = 1.5f;

        /// <summary>How long the boost lasts in seconds.</summary>
        [Tooltip("Duration of the boost in seconds.")]
        [Range(0.1f, 30f)]
        public float durationSeconds = 3f;

        /// <summary>Curve controlling blend-in from 1.0 to the target multiplier.</summary>
        [Tooltip("Ease curve for the initial speed ramp-up (X = normalised time, Y = multiplier weight).")]
        public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>VFX category spawned when this boost activates.</summary>
        [Tooltip("VFX category used by BoostVFXBridge when this boost activates.")]
        public BoostVFXType vfxType = BoostVFXType.ShortBurst;

        /// <summary>Sound effect clip played on boost activation (may be null).</summary>
        [Tooltip("Sound effect clip played on activation. Leave null for AudioManager default.")]
        public AudioClip sfxClip;

        /// <summary>Whether multiple instances of this boost can stack simultaneously.</summary>
        [Tooltip("Allow stacking multiple instances of this boost type.")]
        public bool stackable = false;

        /// <summary>Maximum number of simultaneous stacks (ignored when <see cref="stackable"/> is false).</summary>
        [Tooltip("Maximum simultaneous stacks. Only relevant when Stackable is true.")]
        [Range(1, 10)]
        public int maxStacks = 3;

        /// <summary>Priority used to resolve conflicts in the boost queue (higher wins).</summary>
        [Tooltip("Queue priority — higher value takes precedence over lower when both are active.")]
        [Range(0, 100)]
        public int priority = 10;
    }

    /// <summary>
    /// ScriptableObject that configures the drift charge and boost-reward system.
    /// Create via <em>SWEF &gt; Racing &gt; Drift Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Racing/Drift Config", fileName = "DriftConfig")]
    public class DriftConfig : ScriptableObject
    {
        /// <summary>Multiplier applied to yaw/turn rate while drifting.</summary>
        [Tooltip("Multiplier applied to the player's turn rate during a drift (> 1 = tighter turns).")]
        [Range(0.5f, 3f)]
        public float turnRateMultiplier = 1.4f;

        /// <summary>
        /// Time thresholds in seconds for each drift charge level.
        /// Index 0 = Blue, 1 = Orange, 2 = Purple, 3 = UltraPurple.
        /// </summary>
        [Tooltip("Charge time thresholds (seconds) per drift level: [Blue, Orange, Purple, UltraPurple].")]
        public float[] chargeThresholds = { 1.0f, 2.5f, 4.0f, 6.0f };

        /// <summary>
        /// Boost configs granted on release for each drift level.
        /// Index 0 = Blue reward, 1 = Orange, 2 = Purple, 3 = UltraPurple.
        /// </summary>
        [Tooltip("Boost config granted on drift release per charge level.")]
        public BoostConfig[] boostRewardPerLevel = new BoostConfig[4];

        /// <summary>Spark colours per drift level (Blue, Orange, Purple, UltraPurple).</summary>
        [Tooltip("Spark particle colour per drift level: [Blue, Orange, Purple, UltraPurple].")]
        public Color[] sparkColors =
        {
            new Color(0.3f, 0.6f, 1.0f),
            new Color(1.0f, 0.55f, 0.1f),
            new Color(0.6f, 0.1f, 0.9f),
            new Color(1.0f, 0.2f, 0.8f)
        };

        /// <summary>Percentage reduction in grip/traction applied while drifting (0–1).</summary>
        [Tooltip("Grip reduction applied during drift (0 = no reduction, 1 = full loss).")]
        [Range(0f, 1f)]
        public float gripReductionPercent = 0.35f;

        /// <summary>
        /// Time window in seconds between drift releases that counts as a
        /// "quick successive drift" for the mini-turbo chain bonus.
        /// </summary>
        [Tooltip("Maximum gap between consecutive drifts to qualify for the mini-turbo chain bonus (seconds).")]
        [Range(0.1f, 5f)]
        public float miniTurboChainWindow = 1.5f;
    }

    /// <summary>
    /// ScriptableObject that configures the slipstream / drafting detection zone.
    /// Create via <em>SWEF &gt; Racing &gt; Slipstream Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Racing/Slipstream Config", fileName = "SlipstreamConfig")]
    public class SlipstreamConfig : ScriptableObject
    {
        /// <summary>Maximum distance behind another player to detect a slipstream (metres).</summary>
        [Tooltip("Cone detection range behind other players (metres).")]
        [Range(5f, 200f)]
        public float detectionRange = 50f;

        /// <summary>Half-angle of the detection cone in degrees (default 30°).</summary>
        [Tooltip("Half-angle of the detection cone in degrees.")]
        [Range(5f, 90f)]
        public float coneAngleDegrees = 30f;

        /// <summary>Time in seconds needed inside the slipstream to earn a full charge.</summary>
        [Tooltip("Seconds required to fully charge the slipstream boost.")]
        [Range(0.5f, 10f)]
        public float chargeTime = 3f;

        /// <summary>Boost config granted when the slipstream charge is fully earned.</summary>
        [Tooltip("Boost granted on full slipstream charge.")]
        public BoostConfig boostReward;

        /// <summary>Boost granted when partially charged (below full) and the player exits the zone.</summary>
        [Tooltip("Partial boost granted when the player exits before a full charge is reached.")]
        public BoostConfig partialBoostReward;
    }

    #endregion

    #region Runtime State Structs

    /// <summary>
    /// Snapshot of a single active boost instance managed by <see cref="BoostController"/>.
    /// </summary>
    [Serializable]
    public struct BoostState
    {
        /// <summary>Type of boost.</summary>
        public BoostType type;

        /// <summary>Remaining duration of this boost in seconds.</summary>
        public float remainingDuration;

        /// <summary>Current effective speed multiplier from this boost entry.</summary>
        public float multiplier;

        /// <summary>Current active stack count (only > 1 for stackable boosts).</summary>
        public int stacks;

        /// <summary>The config that created this boost state.</summary>
        public BoostConfig config;

        /// <summary>
        /// Initialises a new <see cref="BoostState"/> from a <see cref="BoostConfig"/>.
        /// </summary>
        public BoostState(BoostConfig cfg)
        {
            config            = cfg;
            type              = cfg.boostType;
            remainingDuration = cfg.durationSeconds;
            multiplier        = cfg.speedMultiplier;
            stacks            = 1;
        }

        /// <summary>Returns <c>true</c> when the boost still has time remaining.</summary>
        public bool IsActive => remainingDuration > 0f;
    }

    /// <summary>
    /// Runtime state of the drift system managed by <see cref="DriftController"/>.
    /// </summary>
    [Serializable]
    public struct DriftState
    {
        /// <summary>Whether a drift is currently in progress.</summary>
        public bool active;

        /// <summary>Direction the drift was locked into.</summary>
        public DriftDirection direction;

        /// <summary>Elapsed time since the drift started (seconds).</summary>
        public float chargeTime;

        /// <summary>Current accumulated drift level.</summary>
        public DriftLevel currentLevel;

        /// <summary>Normalised 0–1 spark intensity for VFX.</summary>
        public float sparkIntensity;

        /// <summary>Resets all fields to their default (non-drifting) values.</summary>
        public void Reset()
        {
            active         = false;
            direction      = DriftDirection.None;
            chargeTime     = 0f;
            currentLevel   = DriftLevel.None;
            sparkIntensity = 0f;
        }
    }

    #endregion
}
