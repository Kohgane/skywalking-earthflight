using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// MonoBehaviour that exposes clip-level editing operations such as split, trim,
    /// camera-angle assignment, transition assignment, and copy/paste.
    /// Delegates project mutations through <see cref="ReplayEditorManager"/>.
    /// </summary>
    public class ReplayClipEditor : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private ReplayEditorManager editorManager;

        #endregion

        #region State

        private ReplayClip _clipboard;

        #endregion

        #region Properties

        private ReplayEditorManager Editor =>
            editorManager != null ? editorManager : ReplayEditorManager.Instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (editorManager == null)
                editorManager = ReplayEditorManager.Instance;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Splits the clip at <paramref name="timestamp"/>, replacing it with two new clips.
        /// The first clip runs from the original start to <paramref name="timestamp"/>;
        /// the second runs from <paramref name="timestamp"/> to the original end.
        /// </summary>
        /// <param name="clipId">Identifier of the clip to split.</param>
        /// <param name="timestamp">Time within the clip at which to split (seconds).</param>
        /// <returns>
        /// A tuple containing the two resulting clips, or <c>(null, null)</c> on failure.
        /// </returns>
        public (ReplayClip first, ReplayClip second) SplitClip(string clipId, float timestamp)
        {
            var project = Editor?.CurrentProject;
            if (project == null) return (null, null);

            var original = project.clips.Find(c => c.clipId == clipId);
            if (original == null)
            {
                Debug.LogWarning($"[SWEF] ReplayClipEditor: SplitClip — clip '{clipId}' not found.");
                return (null, null);
            }

            if (timestamp <= original.startTime || timestamp >= original.endTime)
            {
                Debug.LogWarning($"[SWEF] ReplayClipEditor: SplitClip — timestamp {timestamp} is outside clip range.");
                return (null, null);
            }

            int index = project.clips.IndexOf(original);

            var first = new ReplayClip
            {
                clipId        = Guid.NewGuid().ToString(),
                startTime     = original.startTime,
                endTime       = timestamp,
                cameraAngle   = original.cameraAngle,
                transitionIn  = original.transitionIn,
                transitionOut = TransitionType.None,
                effects       = new List<string>(original.effects),
                playbackSpeed = original.playbackSpeed,
                label         = original.label + "_A"
            };

            var second = new ReplayClip
            {
                clipId        = Guid.NewGuid().ToString(),
                startTime     = timestamp,
                endTime       = original.endTime,
                cameraAngle   = original.cameraAngle,
                transitionIn  = TransitionType.None,
                transitionOut = original.transitionOut,
                effects       = new List<string>(original.effects),
                playbackSpeed = original.playbackSpeed,
                label         = original.label + "_B"
            };

            project.clips.RemoveAt(index);
            project.clips.Insert(index, second);
            project.clips.Insert(index, first);

            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' split at {timestamp:F3}s.");
            return (first, second);
        }

        /// <summary>
        /// Trims the clip to <paramref name="newStart"/> and <paramref name="newEnd"/>.
        /// </summary>
        /// <param name="clipId">Identifier of the clip to trim.</param>
        /// <param name="newStart">New start time in seconds.</param>
        /// <param name="newEnd">New end time in seconds.</param>
        public void TrimClip(string clipId, float newStart, float newEnd)
        {
            var clip = FindClip(clipId);
            if (clip == null) return;

            if (newStart >= newEnd)
            {
                Debug.LogWarning($"[SWEF] ReplayClipEditor: TrimClip — newStart must be less than newEnd.");
                return;
            }

            clip.startTime = newStart;
            clip.endTime   = newEnd;
            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' trimmed to [{newStart:F3}, {newEnd:F3}].");
        }

        /// <summary>
        /// Assigns the named camera angle to the specified clip.
        /// </summary>
        /// <param name="clipId">Identifier of the clip.</param>
        /// <param name="angle">Camera angle name (e.g. "Follow Cam").</param>
        public void SetCameraAngle(string clipId, string angle)
        {
            var clip = FindClip(clipId);
            if (clip == null) return;

            clip.cameraAngle = angle;
            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' camera angle → '{angle}'.");
        }

        /// <summary>
        /// Sets a transition on the clip's entry or exit point.
        /// </summary>
        /// <param name="clipId">Identifier of the clip.</param>
        /// <param name="transition">Transition type to apply.</param>
        /// <param name="isEntry"><c>true</c> to set the entry (in) transition; <c>false</c> for the exit (out) transition.</param>
        public void AddTransition(string clipId, TransitionType transition, bool isEntry)
        {
            var clip = FindClip(clipId);
            if (clip == null) return;

            if (isEntry) clip.transitionIn  = transition;
            else         clip.transitionOut = transition;

            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' transition {(isEntry ? "in" : "out")} → {transition}.");
        }

        /// <summary>
        /// Sets the playback speed multiplier for the specified clip.
        /// </summary>
        /// <param name="clipId">Identifier of the clip.</param>
        /// <param name="speed">Speed multiplier (e.g. 0.5 for slow-motion, 2 for fast-forward).</param>
        public void SetPlaybackSpeed(string clipId, float speed)
        {
            var clip = FindClip(clipId);
            if (clip == null) return;

            clip.playbackSpeed = Mathf.Max(0.01f, speed);
            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' playback speed → {speed}x.");
        }

        /// <summary>
        /// Copies the clip data to the internal clipboard.
        /// </summary>
        /// <param name="clipId">Identifier of the clip to copy.</param>
        /// <returns>A deep copy of the clip, or <c>null</c> if not found.</returns>
        public ReplayClip CopyClip(string clipId)
        {
            var clip = FindClip(clipId);
            if (clip == null) return null;

            _clipboard = CloneClip(clip);
            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' copied to clipboard.");
            return _clipboard;
        }

        /// <summary>
        /// Pastes the clipboard clip into the project at the given index.
        /// </summary>
        /// <param name="clip">The clip to paste (typically from <see cref="CopyClip"/>).</param>
        /// <param name="insertIndex">Position in the clip list at which to insert.</param>
        public void PasteClip(ReplayClip clip, int insertIndex)
        {
            var project = Editor?.CurrentProject;
            if (project == null || clip == null) return;

            var pasted = CloneClip(clip);
            pasted.clipId = Guid.NewGuid().ToString();

            insertIndex = Mathf.Clamp(insertIndex, 0, project.clips.Count);
            project.clips.Insert(insertIndex, pasted);
            Debug.Log($"[SWEF] ReplayClipEditor: Clip pasted at index {insertIndex} as '{pasted.clipId}'.");
        }

        /// <summary>
        /// Duplicates a clip in-place, inserting the copy immediately after the original.
        /// </summary>
        /// <param name="clipId">Identifier of the clip to duplicate.</param>
        public void DuplicateClip(string clipId)
        {
            var project = Editor?.CurrentProject;
            if (project == null) return;

            var original = project.clips.Find(c => c.clipId == clipId);
            if (original == null)
            {
                Debug.LogWarning($"[SWEF] ReplayClipEditor: DuplicateClip — clip '{clipId}' not found.");
                return;
            }

            int index    = project.clips.IndexOf(original);
            var duplicate = CloneClip(original);
            duplicate.clipId = Guid.NewGuid().ToString();
            duplicate.label  = original.label + "_copy";

            project.clips.Insert(index + 1, duplicate);
            Debug.Log($"[SWEF] ReplayClipEditor: Clip '{clipId}' duplicated as '{duplicate.clipId}'.");
        }

        #endregion

        #region Internals

        private ReplayClip FindClip(string clipId)
        {
            var project = Editor?.CurrentProject;
            if (project == null)
            {
                Debug.LogWarning("[SWEF] ReplayClipEditor: No active project.");
                return null;
            }

            var clip = project.clips.Find(c => c.clipId == clipId);
            if (clip == null)
                Debug.LogWarning($"[SWEF] ReplayClipEditor: Clip '{clipId}' not found.");

            return clip;
        }

        private static ReplayClip CloneClip(ReplayClip source) => new ReplayClip
        {
            clipId        = source.clipId,
            startTime     = source.startTime,
            endTime       = source.endTime,
            cameraAngle   = source.cameraAngle,
            transitionIn  = source.transitionIn,
            transitionOut = source.transitionOut,
            effects       = new List<string>(source.effects),
            playbackSpeed = source.playbackSpeed,
            label         = source.label
        };

        #endregion
    }
}
