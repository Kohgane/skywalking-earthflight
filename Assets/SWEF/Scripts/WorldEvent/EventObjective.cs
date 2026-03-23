// EventObjective.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>Defines the type of task the player must perform to advance an objective.</summary>
    public enum ObjectiveType
    {
        /// <summary>Fly to within <see cref="EventObjective.objectiveRadius"/> of a target position.</summary>
        ReachLocation,
        /// <summary>Collect a number of items scattered in the event area.</summary>
        CollectItem,
        /// <summary>Stay alive inside the event area for the required duration.</summary>
        SurviveTime,
        /// <summary>Destroy a number of target objects.</summary>
        DestroyTarget,
        /// <summary>Keep an NPC aircraft safe until it reaches the destination.</summary>
        EscortTarget,
        /// <summary>Fly through a series of ring-shaped gates in order.</summary>
        FlyThroughRings,
        /// <summary>Reach a specific altitude band.</summary>
        ReachAltitude,
        /// <summary>Photograph a target from within the required range.</summary>
        PhotoTarget
    }

    /// <summary>
    /// Defines a single objective within a world event.
    /// Serialised as part of <see cref="ActiveWorldEvent"/>.
    /// </summary>
    [Serializable]
    public sealed class EventObjective
    {
        [Tooltip("Type of task the player must perform.")]
        /// <summary>Type of task the player must perform.</summary>
        public ObjectiveType type;

        [Tooltip("Description shown in the objective tracker UI.")]
        /// <summary>Description shown in the objective tracker UI.</summary>
        public string description;

        [Tooltip("How many times the task must be performed.")]
        /// <summary>How many times the task must be performed.</summary>
        [Min(1)]
        public int requiredCount = 1;

        [Tooltip("How many times the task has been performed so far.")]
        /// <summary>How many times the task has been performed so far.</summary>
        public int currentCount = 0;

        /// <summary>Returns <c>true</c> when <see cref="currentCount"/> meets or exceeds <see cref="requiredCount"/>.</summary>
        public bool isCompleted => currentCount >= requiredCount;

        [Tooltip("World-space target position for location-based objective types.")]
        /// <summary>World-space target position for location-based objective types.</summary>
        public Vector3 targetPosition;

        [Tooltip("Distance in world units within which the objective position is considered reached.")]
        /// <summary>Distance in world units within which the objective position is considered reached.</summary>
        public float objectiveRadius = 50f;

        /// <summary>
        /// Increments <see cref="currentCount"/> by one (up to <see cref="requiredCount"/>).
        /// </summary>
        public void Increment()
        {
            if (currentCount < requiredCount)
                currentCount++;
        }

        /// <summary>
        /// Resets progress back to zero.
        /// </summary>
        public void Reset()
        {
            currentCount = 0;
        }
    }
}
