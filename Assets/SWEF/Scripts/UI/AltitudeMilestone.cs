using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;
using SWEF.Teleport;

namespace SWEF.UI
{
    /// <summary>
    /// Displays altitude milestone toast notifications as the player climbs.
    /// Toasts fade in, hold for <see cref="displayDuration"/> seconds, then fade out.
    /// </summary>
    public class AltitudeMilestone : MonoBehaviour
    {
        // ── Milestone data ────────────────────────────────────────────────────────

        /// <summary>Serializable milestone entry.</summary>
        [System.Serializable]
        public struct Milestone
        {
            public float  altitudeMeters;
            public string message;
            [HideInInspector] public bool triggered;
        }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private AltitudeController altitudeSource;

        [Tooltip("Text element that shows the milestone message.")]
        [SerializeField] private Text milestoneText;

        [Tooltip("CanvasGroup used to fade the toast in/out.")]
        [SerializeField] private CanvasGroup milestoneGroup;

        [Tooltip("Seconds the toast stays fully visible.")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("Seconds taken to fade the toast in.")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Tooltip("Seconds taken to fade the toast out.")]
        [SerializeField] private float fadeOutDuration = 1f;

        [SerializeField] private Milestone[] milestones = new Milestone[]
        {
            new Milestone { altitudeMeters =    1000f, message = "1 km — Above the skyline! 🏙️"     },
            new Milestone { altitudeMeters =   10000f, message = "10 km — Cruising altitude ✈️"       },
            new Milestone { altitudeMeters =   20000f, message = "20 km — Stratosphere! 🌤️"           },
            new Milestone { altitudeMeters =   50000f, message = "50 km — Mesosphere 🌡️"              },
            new Milestone { altitudeMeters =  100000f, message = "100 km — Kármán Line 🚀"            },
            new Milestone { altitudeMeters =  120000f, message = "120 km — Edge of Space! 🌌"         },
        };

        // ── State ─────────────────────────────────────────────────────────────────

        private float   _previousAltitude;
        private Coroutine _toastCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (altitudeSource != null)
                _previousAltitude = altitudeSource.CurrentAltitudeMeters;

            if (milestoneGroup != null)
                milestoneGroup.alpha = 0f;

            // Subscribe to teleport completion to auto-reset
            var teleport = FindFirstObjectByType<TeleportController>();
            if (teleport != null)
                teleport.OnTeleportCompleted += ResetMilestones;
        }

        private void OnDestroy()
        {
            var teleport = FindFirstObjectByType<TeleportController>();
            if (teleport != null)
                teleport.OnTeleportCompleted -= ResetMilestones;
        }

        private void Update()
        {
            if (altitudeSource == null) return;

            float alt = altitudeSource.CurrentAltitudeMeters;

            for (int i = 0; i < milestones.Length; i++)
            {
                if (milestones[i].triggered) continue;

                // Trigger on upward crossing
                if (_previousAltitude < milestones[i].altitudeMeters &&
                    alt >= milestones[i].altitudeMeters)
                {
                    milestones[i].triggered = true;
                    ShowToast(milestones[i].message);
                }
            }

            _previousAltitude = alt;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Re-enables all milestones so they can fire again (e.g. after teleport or restart).</summary>
        public void ResetMilestones()
        {
            for (int i = 0; i < milestones.Length; i++)
                milestones[i].triggered = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ShowToast(string message)
        {
            if (milestoneGroup == null) return;

            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);

            _toastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            if (milestoneText != null)
                milestoneText.text = message;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                milestoneGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            milestoneGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                milestoneGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
                yield return null;
            }
            milestoneGroup.alpha = 0f;
            _toastCoroutine = null;
        }
    }
}
