// FormationConfig.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Static configuration constants and preset slot-offset tables for the
    /// Formation &amp; Wingman AI system.
    /// <para>
    /// All slot offsets are expressed in the leader's local space:
    /// +X = right, +Y = up, −Z = behind.
    /// </para>
    /// </summary>
    public static class FormationConfig
    {
        // ── Limits ────────────────────────────────────────────────────────────

        /// <summary>Maximum number of wingmen allowed in a single formation.</summary>
        public const int MaxWingmen = 5;

        /// <summary>Default lateral / longitudinal spacing between slots (metres).</summary>
        public const float DefaultSpacing = 15f;

        /// <summary>
        /// Time in seconds the formation system takes to transition from one
        /// <see cref="FormationType"/> to another.
        /// </summary>
        public const float FormationTransitionTime = 2f;

        // ── Slot-offset tables ────────────────────────────────────────────────

        /// <summary>
        /// Returns an array of local-space slot offsets for the requested
        /// <see cref="FormationType"/> scaled to <paramref name="count"/> wingmen.
        /// <para>
        /// The array length equals <paramref name="count"/> clamped to
        /// [1, <see cref="MaxWingmen"/>].
        /// </para>
        /// </summary>
        /// <param name="type">Desired formation pattern.</param>
        /// <param name="count">Number of wingmen (1–<see cref="MaxWingmen"/>).</param>
        /// <returns>
        /// Array of local-space <see cref="Vector3"/> offsets, one per slot.
        /// </returns>
        public static Vector3[] GetOffsets(FormationType type, int count)
        {
            count = Mathf.Clamp(count, 1, MaxWingmen);
            float s = DefaultSpacing;

            switch (type)
            {
                case FormationType.VShape:        return Truncate(VShapeOffsets(s), count);
                case FormationType.EchelonLeft:   return Truncate(EchelonLeftOffsets(s), count);
                case FormationType.EchelonRight:  return Truncate(EchelonRightOffsets(s), count);
                case FormationType.Diamond:       return Truncate(DiamondOffsets(s), count);
                case FormationType.Trail:         return Truncate(TrailOffsets(s), count);
                case FormationType.LineAbreast:   return Truncate(LineAbreastOffsets(s), count);
                case FormationType.FingerFour:    return Truncate(FingerFourOffsets(s), count);
                default:                          return Truncate(VShapeOffsets(s), count);
            }
        }

        // ── Private offset generators ─────────────────────────────────────────

        private static Vector3[] VShapeOffsets(float s) => new[]
        {
            new Vector3(-s,      0f, -s),
            new Vector3( s,      0f, -s),
            new Vector3(-s * 2f, 0f, -s * 2f),
            new Vector3( s * 2f, 0f, -s * 2f),
            new Vector3( 0f,     0f, -s * 3f),
        };

        private static Vector3[] EchelonLeftOffsets(float s) => new[]
        {
            new Vector3(-s,      0f, -s),
            new Vector3(-s * 2f, 0f, -s * 2f),
            new Vector3(-s * 3f, 0f, -s * 3f),
            new Vector3(-s * 4f, 0f, -s * 4f),
            new Vector3(-s * 5f, 0f, -s * 5f),
        };

        private static Vector3[] EchelonRightOffsets(float s) => new[]
        {
            new Vector3( s,      0f, -s),
            new Vector3( s * 2f, 0f, -s * 2f),
            new Vector3( s * 3f, 0f, -s * 3f),
            new Vector3( s * 4f, 0f, -s * 4f),
            new Vector3( s * 5f, 0f, -s * 5f),
        };

        private static Vector3[] DiamondOffsets(float s) => new[]
        {
            new Vector3(-s,  0f, -s),
            new Vector3( s,  0f, -s),
            new Vector3( 0f, 0f, -s * 2f),
            new Vector3( 0f, s,  -s),
            new Vector3( 0f, -s, -s),
        };

        private static Vector3[] TrailOffsets(float s) => new[]
        {
            new Vector3(0f, 0f, -s),
            new Vector3(0f, 0f, -s * 2f),
            new Vector3(0f, 0f, -s * 3f),
            new Vector3(0f, 0f, -s * 4f),
            new Vector3(0f, 0f, -s * 5f),
        };

        private static Vector3[] LineAbreastOffsets(float s) => new[]
        {
            new Vector3(-s,      0f, 0f),
            new Vector3( s,      0f, 0f),
            new Vector3(-s * 2f, 0f, 0f),
            new Vector3( s * 2f, 0f, 0f),
            new Vector3(-s * 3f, 0f, 0f),
        };

        private static Vector3[] FingerFourOffsets(float s) => new[]
        {
            new Vector3(-s,      0f, -s * 0.5f),
            new Vector3( s,      0f, -s),
            new Vector3(-s * 2f, 0f, -s),
            new Vector3( s * 2f, 0f, -s * 1.5f),
            new Vector3(-s * 3f, 0f, -s * 1.5f),
        };

        // ── Utility ───────────────────────────────────────────────────────────

        private static Vector3[] Truncate(Vector3[] source, int count)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
                result[i] = i < source.Length ? source[i] : source[source.Length - 1];
            return result;
        }
    }
}
