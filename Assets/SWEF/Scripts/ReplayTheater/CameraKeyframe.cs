using System;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    // ── Camera mode enum ──────────────────────────────────────────────────────────

    /// <summary>
    /// Camera behaviour modes available in the Replay Theater.
    /// </summary>
    public enum CameraMode
    {
        /// <summary>Fully free camera with no subject tracking.</summary>
        Free,
        /// <summary>Camera follows and looks at the subject.</summary>
        Follow,
        /// <summary>Camera orbits around the subject at a fixed distance.</summary>
        Orbit,
        /// <summary>Camera stays on a fixed track, pointing at the subject.</summary>
        Track,
        /// <summary>Camera moves along a dolly rail path.</summary>
        Dolly
    }

    // ── CameraKeyframe ────────────────────────────────────────────────────────────

    /// <summary>
    /// A single keyframe in the cinematic camera editor timeline.
    /// Stores position, orientation, lens, and depth-of-field settings at a
    /// specific replay time. Serializable for save/load.
    /// </summary>
    [Serializable]
    public class CameraKeyframe
    {
        #region Fields

        [Header("Timing")]
        /// <summary>Replay time (seconds) at which this keyframe is placed.</summary>
        [SerializeField] public float time;

        [Header("Transform")]
        /// <summary>World-space camera position at this keyframe.</summary>
        [SerializeField] public Vector3 position;

        /// <summary>World-space camera rotation at this keyframe.</summary>
        [SerializeField] public Quaternion rotation = Quaternion.identity;

        [Header("Lens")]
        /// <summary>Camera field of view in degrees.</summary>
        [SerializeField] public float fov = 60f;

        [Header("Depth of Field")]
        /// <summary>Distance to the in-focus plane (metres).</summary>
        [SerializeField] public float dofFocusDistance = 10f;

        /// <summary>Lens aperture f-stop value; lower = shallower depth of field.</summary>
        [SerializeField] public float dofAperture = 5.6f;

        [Header("Camera Mode")]
        /// <summary>Camera behaviour mode at this keyframe.</summary>
        [SerializeField] public CameraMode mode = CameraMode.Free;

        [Header("Easing")]
        /// <summary>Easing curve applied when transitioning out of this keyframe.</summary>
        [SerializeField] public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Constructors

        /// <summary>Creates a default keyframe at time zero.</summary>
        public CameraKeyframe() { }

        /// <summary>Creates a keyframe with the given time, transform, and mode.</summary>
        /// <param name="time">Replay time in seconds.</param>
        /// <param name="position">World-space camera position.</param>
        /// <param name="rotation">World-space camera rotation.</param>
        /// <param name="mode">Camera behaviour mode.</param>
        public CameraKeyframe(float time, Vector3 position, Quaternion rotation, CameraMode mode = CameraMode.Free)
        {
            this.time     = time;
            this.position = position;
            this.rotation = rotation;
            this.mode     = mode;
        }

        #endregion

        #region Interpolation

        /// <summary>
        /// Linearly interpolates between two <see cref="CameraKeyframe"/> instances.
        /// The easing curve of <paramref name="from"/> is applied to <paramref name="t"/>.
        /// </summary>
        /// <param name="from">Start keyframe.</param>
        /// <param name="to">End keyframe.</param>
        /// <param name="t">Normalised interpolation factor (0–1).</param>
        /// <returns>Interpolated <see cref="CameraKeyframe"/>.</returns>
        public static CameraKeyframe Lerp(CameraKeyframe from, CameraKeyframe to, float t)
        {
            if (from == null) return to;
            if (to   == null) return from;

            float easedT = from.easingCurve != null ? from.easingCurve.Evaluate(t) : t;

            return new CameraKeyframe
            {
                time             = Mathf.Lerp(from.time, to.time, t),
                position         = Vector3.Lerp(from.position, to.position, easedT),
                rotation         = Quaternion.Slerp(from.rotation, to.rotation, easedT),
                fov              = Mathf.Lerp(from.fov, to.fov, easedT),
                dofFocusDistance = Mathf.Lerp(from.dofFocusDistance, to.dofFocusDistance, easedT),
                dofAperture      = Mathf.Lerp(from.dofAperture, to.dofAperture, easedT),
                mode             = from.mode,
                easingCurve      = from.easingCurve,
            };
        }

        #endregion
    }
}
