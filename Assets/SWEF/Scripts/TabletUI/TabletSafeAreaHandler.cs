// TabletSafeAreaHandler.cs — SWEF Tablet UI Optimization (Phase 97)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that applies Unity's <c>Screen.safeArea</c> insets to
    /// every UI root <see cref="Canvas"/> in the scene, accommodating notches, rounded
    /// corners, and home indicators on iPad and Android tablets.
    ///
    /// <para>Attach to a persistent GameObject.  It auto-discovers all root canvases
    /// on start and re-applies insets when the orientation or window size changes.</para>
    ///
    /// <para>Platform-specific detection uses <c>#if UNITY_IOS || UNITY_ANDROID</c>;
    /// a no-op identity rect is used on PC so existing UI is never disturbed.</para>
    /// </summary>
    public class TabletSafeAreaHandler : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletSafeAreaHandler Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Behaviour")]
        [Tooltip("When true the safe area is applied to every root canvas automatically.")]
        [SerializeField] private bool autoApply = true;

        [Tooltip("When true the handler creates a child RectTransform per canvas that " +
                 "serves as the new layout root, leaving the canvas RectTransform intact.")]
        [SerializeField] private bool useChildPanel = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Rect   _lastSafeArea  = Rect.zero;
        private int    _lastWidth;
        private int    _lastHeight;
        private ScreenOrientation _lastOrientation;

        private readonly List<RectTransform> _managedPanels = new List<RectTransform>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoApply) ApplySafeArea();
        }

        private void Update()
        {
            if (!autoApply) return;
            if (Screen.safeArea     != _lastSafeArea   ||
                Screen.width        != _lastWidth      ||
                Screen.height       != _lastHeight     ||
                Screen.orientation  != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately (re-)applies the current <c>Screen.safeArea</c> to all
        /// discovered root canvases.  Safe to call manually after scene loads.
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea = GetPlatformSafeArea();

            _lastSafeArea   = safeArea;
            _lastWidth      = Screen.width;
            _lastHeight     = Screen.height;
            _lastOrientation = Screen.orientation;

            _managedPanels.Clear();

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;
                // Only process root canvases (no parent canvas)
                if (canvas.transform.parent != null &&
                    canvas.transform.parent.GetComponentInParent<Canvas>() != null)
                    continue;

                ApplySafeAreaToCanvas(canvas, safeArea);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private static Rect GetPlatformSafeArea()
        {
#if UNITY_IOS || UNITY_ANDROID
            return Screen.safeArea;
#else
            // On PC the full screen is always the safe area
            return new Rect(0, 0, Screen.width, Screen.height);
#endif
        }

        private void ApplySafeAreaToCanvas(Canvas canvas, Rect safeArea)
        {
            RectTransform targetRT = useChildPanel
                ? GetOrCreateSafeAreaPanel(canvas)
                : canvas.GetComponent<RectTransform>();

            if (targetRT == null) return;

            // Convert safe area to anchor min/max in [0,1] space
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0f || screenH <= 0f) return;

            anchorMin.x /= screenW;
            anchorMin.y /= screenH;
            anchorMax.x /= screenW;
            anchorMax.y /= screenH;

            targetRT.anchorMin = anchorMin;
            targetRT.anchorMax = anchorMax;
            targetRT.offsetMin = Vector2.zero;
            targetRT.offsetMax = Vector2.zero;

            _managedPanels.Add(targetRT);
        }

        private static RectTransform GetOrCreateSafeAreaPanel(Canvas canvas)
        {
            const string panelName = "SafeAreaPanel";
            Transform existing = canvas.transform.Find(panelName);
            if (existing != null) return existing.GetComponent<RectTransform>();

            GameObject panel = new GameObject(panelName, typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Move all existing children into the safe area panel
            List<Transform> children = new List<Transform>();
            foreach (Transform child in canvas.transform)
            {
                if (child != panel.transform) children.Add(child);
            }
            foreach (Transform child in children)
            {
                child.SetParent(panel.transform, true);
            }

            return rt;
        }
    }
}
