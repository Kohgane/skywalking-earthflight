// AircraftShareManager.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Handles exporting and importing aircraft builds as shareable strings.
    ///
    /// <para>
    /// Builds are serialised to JSON via <c>JsonUtility</c>, then encoded as a
    /// Base-64 string for clipboard / social-feed sharing.
    /// </para>
    /// </summary>
    public static class AircraftShareManager
    {
        // ── Export ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises the given build to a Base-64-encoded JSON string that can
        /// be shared with other players.
        /// </summary>
        /// <param name="build">The build to export.  Must not be <c>null</c>.</param>
        /// <returns>Base-64 export string, or <c>null</c> on failure.</returns>
        public static string ExportBuild(AircraftBuildData build)
        {
            if (build == null)
            {
                Debug.LogWarning("[SWEF] Workshop: ExportBuild called with null build.");
                return null;
            }

            try
            {
                string json   = JsonUtility.ToJson(build);
                byte[] bytes  = System.Text.Encoding.UTF8.GetBytes(json);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: ExportBuild failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialises a Base-64 export string back into an
        /// <see cref="AircraftBuildData"/> instance.
        /// </summary>
        /// <param name="shareCode">Base-64 string previously produced by <see cref="ExportBuild"/>.</param>
        /// <returns>
        /// The decoded <see cref="AircraftBuildData"/>, or <c>null</c> if the
        /// input is invalid or fails validation.
        /// </returns>
        public static AircraftBuildData ImportBuild(string shareCode)
        {
            if (string.IsNullOrWhiteSpace(shareCode))
            {
                Debug.LogWarning("[SWEF] Workshop: ImportBuild called with empty shareCode.");
                return null;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(shareCode);
                string json  = System.Text.Encoding.UTF8.GetString(bytes);
                var build    = JsonUtility.FromJson<AircraftBuildData>(json);

                if (!ValidateImportedBuild(build))
                {
                    Debug.LogWarning("[SWEF] Workshop: ImportBuild — validation failed.");
                    return null;
                }

                // Assign a fresh ID so the import doesn't clash with local saves.
                build.buildId = Guid.NewGuid().ToString();
                WorkshopAnalytics.RecordBuildImported(build.buildName);
                return build;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: ImportBuild failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Exports the active build, copies the share code to the system clipboard,
        /// and posts an activity to the social feed.
        /// </summary>
        /// <param name="build">Build to share.  Defaults to the active workshop build if <c>null</c>.</param>
        public static void ShareBuild(AircraftBuildData build = null)
        {
            var target = build ?? WorkshopManager.Instance?.ActiveBuild;
            if (target == null)
            {
                Debug.LogWarning("[SWEF] Workshop: ShareBuild — no build to share.");
                return;
            }

            string code = ExportBuild(target);
            if (string.IsNullOrEmpty(code)) return;

            // Copy to clipboard.
            GUIUtility.systemCopyBuffer = code;

            WorkshopAnalytics.RecordBuildShared(target.buildId);

            // Post to social feed.
#if SWEF_SOCIAL_AVAILABLE
            var feed = SWEF.SocialHub.SocialActivityFeed.Instance;
            feed?.PostActivity("workshop_build_shared",
                $"Shared aircraft build: {target.buildName}",
                target.buildId);
#endif
        }

        /// <summary>
        /// Validates an imported build for basic sanity (non-null, non-empty ID, etc.).
        /// </summary>
        /// <param name="build">Build to validate.</param>
        /// <returns><c>true</c> if the build appears well-formed.</returns>
        public static bool ValidateImportedBuild(AircraftBuildData build)
        {
            if (build == null)                           return false;
            if (string.IsNullOrEmpty(build.buildName))  return false;
            if (build.equippedPartIds == null)           return false;
            if (build.paintScheme == null)               return false;
            if (build.decals == null)                    return false;
            if (build.decals.Count > DecalEditorController.MaxDecals) return false;
            return true;
        }
    }
}
