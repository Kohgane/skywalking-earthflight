using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Floating tooltip panel that anchors to a target HUD element and
    /// displays the current tutorial step instruction.
    /// Includes a directional arrow image and "Tap to continue" / "Do it now!" prompts.
    /// </summary>
    public class TutorialTooltip : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private Text instructionText;
        [SerializeField] private Text promptText;

        [Header("Arrow")]
        [SerializeField] private RectTransform arrowRect;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup   canvasGroup;

        [Header("Animation")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float fadeSpeed = 5f;

        // Arrow offsets relative to the spotlight target's edge (pixels)
        private const float ArrowOffset = 24f;

        private bool          _visible;
        private RectTransform _anchorTarget;
        private TooltipAnchor _currentAnchor;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Displays the tooltip with the given message, optionally anchored to a HUD element.
        /// </summary>
        /// <param name="message">Instruction text to show.</param>
        /// <param name="anchor">Target RectTransform to position the tooltip near. Pass <c>null</c> for centered.</param>
        /// <param name="direction">Which edge of the target the tooltip should appear on.</param>
        /// <param name="requiresAction">
        /// When <c>true</c> the prompt reads "Do it now!"; otherwise "Tap to continue".
        /// </param>
        public void Show(string message, RectTransform anchor, TooltipAnchor direction, bool requiresAction = false)
        {
            _anchorTarget  = anchor;
            _currentAnchor = direction;
            _visible       = true;

            gameObject.SetActive(true);

            if (instructionText != null)
                instructionText.text = message;

            if (promptText != null)
                promptText.text = requiresAction ? "Do it now!" : "Tap to continue";

            if (anchor != null)
                SnapToAnchor(anchor, direction);
            else
                CenterOnScreen();

            UpdateArrowRotation(direction);
        }

        /// <summary>Fades out and hides the tooltip.</summary>
        public void Hide()
        {
            _visible      = false;
            _anchorTarget = null;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateFade();
            UpdatePosition();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void UpdateFade()
        {
            if (canvasGroup == null) return;
            float targetAlpha = _visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            if (!_visible && canvasGroup.alpha <= 0f)
                gameObject.SetActive(false);
        }

        private void UpdatePosition()
        {
            if (!_visible || panelRect == null) return;

            Vector3 targetPos = _anchorTarget != null
                ? CalcPosition(_anchorTarget, _currentAnchor)
                : (Vector3)(Vector2.zero);

            panelRect.position = Vector3.Lerp(panelRect.position, targetPos, moveSpeed * Time.deltaTime);
        }

        private void SnapToAnchor(RectTransform anchor, TooltipAnchor direction)
        {
            if (panelRect == null) return;
            panelRect.position = CalcPosition(anchor, direction);
        }

        private void CenterOnScreen()
        {
            if (panelRect == null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            panelRect.anchoredPosition = Vector2.zero;
        }

        private Vector3 CalcPosition(RectTransform anchor, TooltipAnchor direction)
        {
            if (anchor == null || panelRect == null) return Vector3.zero;

            Vector3 anchorPos = anchor.position;
            float   halfW     = (panelRect.rect.width  * panelRect.lossyScale.x) * 0.5f;
            float   halfH     = (panelRect.rect.height * panelRect.lossyScale.y) * 0.5f;
            float   targetH   = (anchor.rect.height    * anchor.lossyScale.y)    * 0.5f;
            float   targetW   = (anchor.rect.width     * anchor.lossyScale.x)    * 0.5f;

            switch (direction)
            {
                case TooltipAnchor.Top:
                    return anchorPos + Vector3.down    * (targetH + halfH + ArrowOffset);
                case TooltipAnchor.Bottom:
                    return anchorPos + Vector3.up      * (targetH + halfH + ArrowOffset);
                case TooltipAnchor.Left:
                    return anchorPos + Vector3.right   * (targetW + halfW + ArrowOffset);
                case TooltipAnchor.Right:
                    return anchorPos + Vector3.left    * (targetW + halfW + ArrowOffset);
                default:
                    return anchorPos;
            }
        }

        private void UpdateArrowRotation(TooltipAnchor direction)
        {
            if (arrowRect == null) return;

            // The arrow image is assumed to point upward by default.
            float zAngle;
            switch (direction)
            {
                case TooltipAnchor.Top:    zAngle = 180f; break; // points down toward target
                case TooltipAnchor.Bottom: zAngle =   0f; break; // points up toward target
                case TooltipAnchor.Left:   zAngle =  90f; break; // points right toward target
                case TooltipAnchor.Right:  zAngle = 270f; break; // points left toward target
                default:
                    arrowRect.gameObject.SetActive(false);
                    return;
            }

            arrowRect.gameObject.SetActive(true);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, zAngle);
        }
    }
}
