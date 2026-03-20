using UnityEngine;
using UnityEngine.UI;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Visual timeline track component rendered inside the Replay Theater UI.
    /// Manages the scrub bar, moving playhead, A-B loop markers, and camera
    /// keyframe markers.  Raises events when the user drags to scrub or selects
    /// a keyframe marker.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TimelineTrack : MonoBehaviour
    {
        #region Inspector

        [Header("Visual Elements")]
        [SerializeField] private RectTransform scrubBarRect;
        [SerializeField] private RectTransform playheadRect;
        [SerializeField] private RectTransform loopMarkerA;
        [SerializeField] private RectTransform loopMarkerB;
        [SerializeField] private Transform     keyframeMarkersParent;
        [SerializeField] private GameObject    keyframeMarkerPrefab;

        [Header("Colors")]
        [SerializeField] private Color scrubBarColor       = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color playheadColor       = Color.white;
        [SerializeField] private Color keyframeMarkerColor = new Color(1f, 0.8f, 0f, 1f);
        [SerializeField] private Color loopRegionColor     = new Color(0.4f, 1f, 0.4f, 0.3f);

        [Header("Zoom")]
        [SerializeField] private float zoomLevel  = 1f;   // 1 = full duration visible
        [SerializeField] private float zoomOffset = 0f;   // left edge offset (0–1 of duration)
        [SerializeField] private float minZoom    = 1f;
        [SerializeField] private float maxZoom    = 10f;

        #endregion

        #region Private State

        private ReplayTimeline _timeline;
        private bool           _dragging;
        private RectTransform  _ownRect;

        #endregion

        #region Events

        /// <summary>Fired while the user is dragging the playhead.  Parameter is seconds.</summary>
        public event System.Action<float> OnScrub;

        /// <summary>Fired when the user clicks a keyframe marker.  Parameter is the keyframe index.</summary>
        public event System.Action<int>   OnKeyframeSelected;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _ownRect = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyColours();
        }

        private void Update()
        {
            if (_timeline == null) return;

            RefreshPlayhead();
        }

        #endregion

        #region Public API

        /// <summary>Links this track to a <see cref="ReplayTimeline"/>.</summary>
        /// <param name="timeline">The timeline to mirror.</param>
        public void Bind(ReplayTimeline timeline)
        {
            _timeline = timeline;
            RefreshPlayhead();
        }

        /// <summary>
        /// Rebuilds keyframe marker GameObjects from the supplied editor's keyframe list.
        /// </summary>
        /// <param name="editor">The camera editor owning the keyframes.</param>
        public void RefreshKeyframeMarkers(CinematicCameraEditor editor)
        {
            if (keyframeMarkersParent == null || keyframeMarkerPrefab == null) return;

            // Destroy old markers
            foreach (Transform child in keyframeMarkersParent)
                Destroy(child.gameObject);

            if (editor == null || _timeline == null) return;

            float duration = _timeline.TotalDuration;
            if (duration <= 0f) return;

            for (int i = 0; i < editor.Keyframes.Count; i++)
            {
                var kf   = editor.Keyframes[i];
                var go   = Instantiate(keyframeMarkerPrefab, keyframeMarkersParent);
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float normalisedPos = TimeToNormalized(kf.time, duration);
                    rect.anchorMin = new Vector2(normalisedPos, 0f);
                    rect.anchorMax = new Vector2(normalisedPos, 1f);
                    rect.anchoredPosition = Vector2.zero;
                }

                var img = go.GetComponent<Image>();
                if (img != null) img.color = keyframeMarkerColor;

                // Store index for click callback
                int capturedIndex = i;
                var btn = go.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnKeyframeSelected?.Invoke(capturedIndex));
            }
        }

        /// <summary>Zooms the timeline view in or out.</summary>
        /// <param name="delta">Positive = zoom in, negative = zoom out.</param>
        public void Zoom(float delta)
        {
            zoomLevel = Mathf.Clamp(zoomLevel + delta, minZoom, maxZoom);
            RefreshPlayhead();
            Debug.Log($"[SWEF] TimelineTrack: Zoom={zoomLevel:F1}x");
        }

        /// <summary>Scrolls the visible zoom window.</summary>
        /// <param name="delta">Normalised offset delta (0–1).</param>
        public void Scroll(float delta)
        {
            float maxOffset = 1f - 1f / zoomLevel;
            zoomOffset = Mathf.Clamp(zoomOffset + delta, 0f, Mathf.Max(0f, maxOffset));
            RefreshPlayhead();
        }

        #endregion

        #region Pointer Events (called by UI EventTrigger)

        /// <summary>Called by an EventTrigger when the pointer presses on the scrub bar.</summary>
        /// <param name="screenPos">Screen-space pointer position.</param>
        public void OnPointerDown(Vector2 screenPos)
        {
            _dragging = true;
            ScrubToScreenPos(screenPos);
        }

        /// <summary>Called by an EventTrigger when the pointer drags on the scrub bar.</summary>
        /// <param name="screenPos">Screen-space pointer position.</param>
        public void OnPointerDrag(Vector2 screenPos)
        {
            if (!_dragging) return;
            ScrubToScreenPos(screenPos);
        }

        /// <summary>Called by an EventTrigger when the pointer is released.</summary>
        public void OnPointerUp()
        {
            _dragging = false;
        }

        #endregion

        #region Internals

        private void ScrubToScreenPos(Vector2 screenPos)
        {
            if (_timeline == null || _ownRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _ownRect, screenPos, null, out Vector2 local);

            float width = _ownRect.rect.width;
            if (width <= 0f) return;

            float normX = Mathf.Clamp01((local.x + width * 0.5f) / width);

            // Map zoomed view back to full timeline
            float mappedT = zoomOffset + normX / zoomLevel;
            float time     = Mathf.Clamp(mappedT * _timeline.TotalDuration, 0f, _timeline.TotalDuration);

            OnScrub?.Invoke(time);
        }

        private void RefreshPlayhead()
        {
            if (playheadRect == null || _timeline == null) return;

            float duration = _timeline.TotalDuration;
            if (duration <= 0f) return;

            float normTime        = TimeToNormalized(_timeline.CurrentTime, duration);
            playheadRect.anchorMin = new Vector2(normTime, 0f);
            playheadRect.anchorMax = new Vector2(normTime, 1f);
            playheadRect.anchoredPosition = Vector2.zero;

            // Update loop markers
            if (loopMarkerA != null)
            {
                float na = TimeToNormalized(_timeline.LoopPointA, duration);
                loopMarkerA.anchorMin = new Vector2(na, 0f);
                loopMarkerA.anchorMax = new Vector2(na, 1f);
                loopMarkerA.anchoredPosition = Vector2.zero;
            }
            if (loopMarkerB != null)
            {
                float nb = TimeToNormalized(_timeline.LoopPointB, duration);
                loopMarkerB.anchorMin = new Vector2(nb, 0f);
                loopMarkerB.anchorMax = new Vector2(nb, 1f);
                loopMarkerB.anchoredPosition = Vector2.zero;
            }
        }

        private float TimeToNormalized(float time, float duration)
        {
            if (duration <= 0f) return 0f;
            float fullNorm = time / duration;
            // Map into zoomed window: normX = (fullNorm - zoomOffset) * zoomLevel
            return Mathf.Clamp01((fullNorm - zoomOffset) * zoomLevel);
        }

        private void ApplyColours()
        {
            if (scrubBarRect != null)
            {
                var img = scrubBarRect.GetComponent<Image>();
                if (img != null) img.color = scrubBarColor;
            }
            if (playheadRect != null)
            {
                var img = playheadRect.GetComponent<Image>();
                if (img != null) img.color = playheadColor;
            }
        }

        #endregion
    }
}
