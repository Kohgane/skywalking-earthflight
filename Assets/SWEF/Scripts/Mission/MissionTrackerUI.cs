// MissionTrackerUI.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — In-flight HUD panel that tracks mission progress in real time.
    ///
    /// <para>Subscribe to <see cref="MissionManager"/> events in <c>OnEnable</c> and call
    /// <see cref="UpdateTracker"/> each frame (or whenever the manager broadcasts a change)
    /// to keep all displays current.  World-space checkpoint rings are rendered via
    /// <see cref="RenderCheckpointRing"/>.</para>
    /// </summary>
    public class MissionTrackerUI : MonoBehaviour
    {
        #region Inspector

        [Header("Objective Display")]
        [Tooltip("Displays the description of the currently active objective.")]
        /// <summary>Displays the current objective description.</summary>
        public TextMeshProUGUI currentObjectiveText;

        [Tooltip("Fill-bar showing progress toward the current objective.")]
        /// <summary>Fill bar showing current objective progress.</summary>
        public Image objectiveProgressBar;

        [Header("Timer")]
        [Tooltip("Displays elapsed time or remaining time when there is a time limit.")]
        /// <summary>Mission timer / countdown display.</summary>
        public TextMeshProUGUI timerText;

        [Header("Checkpoint")]
        [Tooltip("Displays current / total checkpoints, e.g. 'Checkpoint 3 / 12'.")]
        /// <summary>Displays checkpoint progress as "Checkpoint N/Total".</summary>
        public TextMeshProUGUI checkpointText;

        [Tooltip("RectTransform of the on-screen waypoint marker (projected from 3D position).")]
        /// <summary>On-screen 3D-to-2D projected waypoint dot.</summary>
        public RectTransform waypointMarker;

        [Tooltip("Arrow image clamped to the screen edge when the next checkpoint is off-screen.")]
        /// <summary>Edge-clamped directional arrow used when the checkpoint is off-screen.</summary>
        public Image waypointArrow;

        [Tooltip("Displays the straight-line distance to the next checkpoint.")]
        /// <summary>Distance label, e.g. "2.3 km".</summary>
        public TextMeshProUGUI distanceToWaypointText;

        [Header("Objective Mini-List")]
        [Tooltip("Container for the small per-objective status row list.")]
        /// <summary>Container holding per-objective mini-list rows.</summary>
        public Transform objectiveMiniList;

        [Tooltip("Prefab instantiated for each row in the objective mini-list.")]
        /// <summary>Prefab for each mini-list row.</summary>
        public GameObject objectiveMiniItemPrefab;

        [Header("Colours")]
        [Tooltip("Colour used for active / in-progress elements.")]
        /// <summary>Colour for active objectives and progress elements.</summary>
        public Color activeColor = Color.white;

        [Tooltip("Colour used for completed elements.")]
        /// <summary>Colour for completed objective labels.</summary>
        public Color completedColor = Color.green;

        [Tooltip("Colour used for failed elements.")]
        /// <summary>Colour for failed objective labels.</summary>
        public Color failedColor = Color.red;

        [Header("Camera Reference")]
        [Tooltip("The camera used to project 3D checkpoint positions to screen space.")]
        [SerializeField] private Camera _hudCamera;

        #endregion

        #region Private State

        private List<GameObject> _miniListItems = new List<GameObject>();
        private Coroutine _checkpointFlashCoroutine;
        private Coroutine _objectiveCompleteCoroutine;
        private MissionData _trackedMission;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnCheckpointReached  += HandleCheckpointReached;
                MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
                MissionManager.Instance.OnMissionStatusChanged += HandleMissionStatusChanged;
            }
        }

        private void OnDisable()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnCheckpointReached  -= HandleCheckpointReached;
                MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
                MissionManager.Instance.OnMissionStatusChanged -= HandleMissionStatusChanged;
            }
        }

        private void Update()
        {
            if (MissionManager.Instance == null) return;
            if (MissionManager.Instance.currentStatus != MissionStatus.InProgress) return;
            if (MissionManager.Instance.currentMission == null) return;

            UpdateTracker(MissionManager.Instance.currentMission,
                          MissionManager.Instance.currentCheckpointIndex);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Refreshes all HUD displays to reflect the current mission state.
        /// </summary>
        /// <param name="mission">The currently active mission.</param>
        /// <param name="checkpointIndex">Index of the next checkpoint to reach.</param>
        public void UpdateTracker(MissionData mission, int checkpointIndex)
        {
            if (mission == null) return;

            if (_trackedMission != mission)
            {
                _trackedMission = mission;
                BuildMiniList(mission);
            }

            UpdateActiveObjective(mission);
            UpdateTimer(mission);
            UpdateCheckpointDisplay(mission, checkpointIndex);
            UpdateWaypointIndicator(mission, checkpointIndex);
            RefreshMiniList(mission);
        }

        /// <summary>Plays a brief flash animation indicating the player passed a checkpoint.</summary>
        /// <param name="index">The zero-based index of the checkpoint that was reached.</param>
        public void ShowCheckpointReached(int index)
        {
            if (_checkpointFlashCoroutine != null) StopCoroutine(_checkpointFlashCoroutine);
            _checkpointFlashCoroutine = StartCoroutine(CheckpointFlash(index));
        }

        /// <summary>
        /// Animates an objective row to show a strikethrough and checkmark when completed.
        /// </summary>
        /// <param name="objective">The objective that was completed.</param>
        public void ShowObjectiveComplete(MissionObjective objective)
        {
            if (_objectiveCompleteCoroutine != null) StopCoroutine(_objectiveCompleteCoroutine);
            _objectiveCompleteCoroutine = StartCoroutine(ObjectiveCompleteAnimation(objective));
        }

        /// <summary>
        /// Renders a world-space fly-through ring for the given checkpoint using
        /// Unity's <see cref="GL"/> immediate-mode API (fallback) or a pooled mesh.
        /// Override this method in a subclass to use a particle or mesh-based ring.
        /// </summary>
        /// <param name="cp">Checkpoint whose ring should be drawn.</param>
        public void RenderCheckpointRing(MissionCheckpoint cp)
        {
            if (!cp.showRing) return;
            // Derived classes or a companion Gizmo component handle actual mesh rendering.
            // This base implementation draws a debug circle visible in the editor.
#if UNITY_EDITOR
            DrawDebugCircle(cp.position, cp.radius * cp.ringScale, cp.markerColor);
#endif
        }

        #endregion

        #region Private — Update Helpers

        private void UpdateActiveObjective(MissionData mission)
        {
            MissionObjective active = GetActiveObjective(mission);
            if (currentObjectiveText != null)
                currentObjectiveText.text = active != null ? active.description : string.Empty;

            if (objectiveProgressBar != null)
                objectiveProgressBar.fillAmount = active != null ? active.progress : 0f;
        }

        private void UpdateTimer(MissionData mission)
        {
            if (timerText == null) return;

            float elapsed = MissionManager.Instance.missionElapsedTime;
            if (mission.timeLimit > 0f)
            {
                float remaining = Mathf.Max(0f, mission.timeLimit - elapsed);
                timerText.text = FormatTime(remaining);
                timerText.color = remaining < 30f ? failedColor : activeColor;
            }
            else
            {
                timerText.text = FormatTime(elapsed);
                timerText.color = activeColor;
            }
        }

        private void UpdateCheckpointDisplay(MissionData mission, int checkpointIndex)
        {
            if (checkpointText == null) return;
            int total = mission.checkpoints.Count;
            int reached = Mathf.Min(checkpointIndex, total);
            checkpointText.text = total > 0 ? $"Checkpoint {reached}/{total}" : string.Empty;
        }

        private void UpdateWaypointIndicator(MissionData mission, int checkpointIndex)
        {
            if (checkpointIndex >= mission.checkpoints.Count)
            {
                SetWaypointVisible(false);
                return;
            }

            MissionCheckpoint next = mission.checkpoints[checkpointIndex];
            Camera cam = _hudCamera != null ? _hudCamera : Camera.main;
            if (cam == null) { SetWaypointVisible(false); return; }

            Vector3 screenPos = cam.WorldToScreenPoint(next.position);
            bool inFront = screenPos.z > 0f;
            bool onScreen = inFront
                && screenPos.x >= 0f && screenPos.x <= Screen.width
                && screenPos.y >= 0f && screenPos.y <= Screen.height;

            // Distance label
            if (distanceToWaypointText != null)
            {
                Vector3 playerPos = cam.transform.position;
                float dist = Vector3.Distance(playerPos, next.position);
                distanceToWaypointText.text = dist >= 1000f
                    ? $"{dist / 1000f:0.0} km"
                    : $"{dist:0} m";
            }

            if (onScreen)
            {
                // Project dot onto screen
                if (waypointMarker != null)
                {
                    waypointMarker.gameObject.SetActive(true);
                    waypointMarker.position = new Vector3(screenPos.x, screenPos.y, 0f);
                }
                if (waypointArrow != null) waypointArrow.gameObject.SetActive(false);
            }
            else
            {
                // Clamp arrow to screen edge
                if (waypointMarker != null) waypointMarker.gameObject.SetActive(false);
                if (waypointArrow != null)
                {
                    waypointArrow.gameObject.SetActive(true);
                    Vector3 dir = (inFront ? screenPos : -screenPos) - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    waypointArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                    // Clamp to screen edge
                    dir.Normalize();
                    float halfW = Screen.width  * 0.5f - 40f;
                    float halfH = Screen.height * 0.5f - 40f;
                    float edgeX, edgeY;
                    if (Mathf.Approximately(dir.x, 0f))
                    {
                        // Vertical direction — go straight to top or bottom edge
                        edgeX = 0f;
                        edgeY = dir.y > 0f ? halfH : -halfH;
                    }
                    else
                    {
                        float slope = dir.y / dir.x;
                        edgeX = dir.x > 0f ? halfW : -halfW;
                        edgeY = slope * edgeX;
                        if (Mathf.Abs(edgeY) > halfH)
                        {
                            edgeY = dir.y > 0f ? halfH : -halfH;
                            edgeX = Mathf.Approximately(slope, 0f)
                                ? (dir.x > 0f ? halfW : -halfW)
                                : edgeY / slope;
                        }
                    }
                    waypointArrow.rectTransform.anchoredPosition = new Vector2(edgeX, edgeY);
                }
            }
        }

        private void SetWaypointVisible(bool visible)
        {
            if (waypointMarker != null) waypointMarker.gameObject.SetActive(visible);
            if (waypointArrow  != null) waypointArrow.gameObject.SetActive(visible);
            if (distanceToWaypointText != null) distanceToWaypointText.text = string.Empty;
        }

        #endregion

        #region Private — Mini-List

        private void BuildMiniList(MissionData mission)
        {
            foreach (GameObject item in _miniListItems)
                if (item != null) Destroy(item);
            _miniListItems.Clear();

            if (objectiveMiniList == null || objectiveMiniItemPrefab == null) return;

            foreach (MissionObjective obj in mission.objectives)
            {
                if (obj.isHidden) continue;
                GameObject item = Instantiate(objectiveMiniItemPrefab, objectiveMiniList);
                _miniListItems.Add(item);
            }
        }

        private void RefreshMiniList(MissionData mission)
        {
            int idx = 0;
            foreach (MissionObjective obj in mission.objectives)
            {
                if (obj.isHidden) continue;
                if (idx >= _miniListItems.Count) break;

                GameObject item = _miniListItems[idx++];
                TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label == null) continue;

                label.text = obj.description;
                label.color = obj.status switch
                {
                    ObjectiveStatus.Completed => completedColor,
                    ObjectiveStatus.Failed    => failedColor,
                    _                         => activeColor,
                };
            }
        }

        #endregion

        #region Private — Animations

        private IEnumerator CheckpointFlash(int index)
        {
            if (checkpointText == null) yield break;
            Color original = checkpointText.color;
            checkpointText.color = completedColor;
            yield return new WaitForSeconds(1.5f);
            checkpointText.color = original;
        }

        private IEnumerator ObjectiveCompleteAnimation(MissionObjective objective)
        {
            if (currentObjectiveText == null) yield break;
            Color original = currentObjectiveText.color;
            currentObjectiveText.color = completedColor;
            // Simple strike-through via TMP rich text
            string strikeText = $"<s>{objective.description}</s> ✓";
            currentObjectiveText.text = strikeText;
            yield return new WaitForSeconds(2f);
            currentObjectiveText.color = original;
        }

        #endregion

        #region Private — Event Handlers

        private void HandleCheckpointReached(MissionCheckpoint cp)
        {
            ShowCheckpointReached(cp.checkpointIndex);
        }

        private void HandleObjectiveCompleted(MissionObjective obj)
        {
            ShowObjectiveComplete(obj);
        }

        private void HandleMissionStatusChanged(MissionStatus status)
        {
            bool active = status == MissionStatus.InProgress || status == MissionStatus.Paused;
            gameObject.SetActive(active);
        }

        #endregion

        #region Private — Helpers

        private static MissionObjective GetActiveObjective(MissionData mission)
        {
            foreach (MissionObjective obj in mission.objectives)
                if (obj.status == ObjectiveStatus.Active) return obj;
            return null;
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m:D2}:{s:00.0}";
        }

#if UNITY_EDITOR
        private static void DrawDebugCircle(Vector3 centre, float radius, Color color)
        {
            int segments = 36;
            float step = 360f / segments;
            Vector3 prev = centre + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector3 next = centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Debug.DrawLine(prev, next, color);
                prev = next;
            }
        }
#endif

        #endregion
    }
}
