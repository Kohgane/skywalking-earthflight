using System;
using UnityEngine;

namespace SWEF.Events
{
    /// <summary>
    /// Lifecycle state of a <see cref="WorldEventInstance"/>.
    /// </summary>
    public enum WorldEventState
    {
        /// <summary>Event has been created but has not yet started.</summary>
        Pending,

        /// <summary>Event is running and players can participate.</summary>
        Active,

        /// <summary>Event is about to end — visuals are fading out.</summary>
        Expiring,

        /// <summary>Event has fully ended.</summary>
        Ended
    }

    /// <summary>
    /// Represents a single live instance of a <see cref="WorldEventData"/> at runtime.
    /// This is a plain C# class (not a MonoBehaviour); it is owned and updated by
    /// <see cref="EventScheduler"/>.
    /// </summary>
    [Serializable]
    public class WorldEventInstance
    {
        // ── Fields ────────────────────────────────────────────────────────────────
        /// <summary>The template that describes this event.</summary>
        public WorldEventData eventData;

        /// <summary>Unique identifier for this specific instance.</summary>
        public Guid instanceId;

        /// <summary>World-space position at which this instance was spawned.</summary>
        public Vector3 spawnPosition;

        /// <summary><see cref="Time.time"/> value at which this instance became active.</summary>
        public float startTime;

        /// <summary><see cref="Time.time"/> value at which this instance will end.</summary>
        public float endTime;

        /// <summary>Current lifecycle state.</summary>
        public WorldEventState state = WorldEventState.Pending;

        // ── Computed properties ───────────────────────────────────────────────────
        /// <summary>
        /// Seconds remaining until the event ends. Returns 0 when the event has ended.
        /// </summary>
        public float RemainingTime => state == WorldEventState.Ended
            ? 0f
            : Mathf.Max(0f, endTime - Time.time);

        /// <summary>
        /// Normalised progress of the event in the range [0, 1] where 1 = fully elapsed.
        /// </summary>
        public float Progress01
        {
            get
            {
                float duration = endTime - startTime;
                if (duration <= 0f) return 1f;
                return Mathf.Clamp01((Time.time - startTime) / duration);
            }
        }

        /// <summary>
        /// <c>true</c> when the event is in the <see cref="WorldEventState.Active"/> state.
        /// </summary>
        public bool IsActive => state == WorldEventState.Active;

        // ── Constructor ───────────────────────────────────────────────────────────
        /// <summary>
        /// Creates a new <see cref="WorldEventInstance"/> linked to the given template.
        /// </summary>
        /// <param name="data">Template describing the event.</param>
        /// <param name="position">World-space spawn position.</param>
        /// <param name="durationSeconds">How long (in seconds) the event should run.</param>
        public WorldEventInstance(WorldEventData data, Vector3 position, float durationSeconds)
        {
            eventData     = data;
            instanceId    = Guid.NewGuid();
            spawnPosition = position;
            state         = WorldEventState.Pending;
            startTime     = Time.time;
            endTime       = Time.time + durationSeconds;
        }

        // ── Lifecycle methods ─────────────────────────────────────────────────────
        /// <summary>
        /// Transitions the instance from <see cref="WorldEventState.Pending"/> to
        /// <see cref="WorldEventState.Active"/>.
        /// </summary>
        public void Activate()
        {
            if (state != WorldEventState.Pending) return;
            state     = WorldEventState.Active;
            startTime = Time.time;
            endTime   = startTime + (eventData != null
                ? UnityEngine.Random.Range(eventData.minDurationMinutes, eventData.maxDurationMinutes) * 60f
                : endTime - startTime);
        }

        /// <summary>
        /// Transitions the instance to <see cref="WorldEventState.Expiring"/> so that
        /// visual fade-out can begin.
        /// </summary>
        public void Expire()
        {
            if (state == WorldEventState.Ended) return;
            state = WorldEventState.Expiring;
        }

        /// <summary>
        /// Fully terminates the event instance.
        /// </summary>
        public void End()
        {
            state = WorldEventState.Ended;
        }
    }
}
