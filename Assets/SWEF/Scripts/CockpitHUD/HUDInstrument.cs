// HUDInstrument.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — Abstract base class for every cockpit HUD instrument.
    ///
    /// <para>Each instrument automatically registers itself with
    /// <see cref="HUDDashboard"/> on <c>OnEnable</c> and unregisters on
    /// <c>OnDisable</c>.  Subclasses must implement
    /// <see cref="UpdateInstrument(FlightData)"/> which is called by the dashboard
    /// every frame.</para>
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class HUDInstrument : MonoBehaviour
    {
        #region Inspector

        [Header("Instrument Identity")]
        [Tooltip("Human-readable name shown in editor and debug overlays.")]
        [SerializeField] private string instrumentName = "HUD Instrument";

        [Tooltip("Whether this instrument is currently visible.")]
        [SerializeField] private bool isVisible = true;

        [Tooltip("Minimum HUD mode in which this instrument is displayed.")]
        [SerializeField] private HUDMode minimumMode = HUDMode.Minimal;

        [Header("Fade")]
        [Tooltip("Speed at which the instrument fades in or out (alpha/second).")]
        [SerializeField] private float fadeSpeed = 5f;

        #endregion

        #region Public Properties

        /// <summary>Human-readable name of this instrument.</summary>
        public string InstrumentName => instrumentName;

        /// <summary>Whether this instrument is currently visible.</summary>
        public bool IsVisible => isVisible;

        /// <summary>Minimum HUD mode required to display this instrument.</summary>
        public HUDMode MinimumMode => minimumMode;

        /// <summary>The instrument's <see cref="CanvasGroup"/> for alpha-based fading.</summary>
        public CanvasGroup CanvasGroupComponent { get; private set; }

        /// <summary>The instrument's <see cref="RectTransform"/>.</summary>
        public RectTransform InstrumentRect { get; private set; }

        #endregion

        #region Private State

        private float _targetAlpha;

        #endregion

        #region Unity Lifecycle

        /// <summary>Caches component references.</summary>
        protected virtual void Awake()
        {
            CanvasGroupComponent = GetComponent<CanvasGroup>();
            InstrumentRect       = GetComponent<RectTransform>();
        }

        /// <summary>Registers with <see cref="HUDDashboard"/> and sets initial visibility.</summary>
        protected virtual void OnEnable()
        {
            HUDDashboard.Instance?.RegisterInstrument(this);
            _targetAlpha = isVisible ? 1f : 0f;
            if (CanvasGroupComponent != null)
                CanvasGroupComponent.alpha = _targetAlpha;
        }

        /// <summary>Unregisters from <see cref="HUDDashboard"/>.</summary>
        protected virtual void OnDisable()
        {
            HUDDashboard.Instance?.UnregisterInstrument(this);
        }

        /// <summary>Smoothly interpolates alpha toward the target.</summary>
        protected virtual void Update()
        {
            if (CanvasGroupComponent == null) return;
            CanvasGroupComponent.alpha = Mathf.MoveTowards(
                CanvasGroupComponent.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        }

        #endregion

        #region Abstract / Virtual API

        /// <summary>
        /// Called by <see cref="HUDDashboard"/> every frame with the current
        /// <see cref="FlightData"/> snapshot.  Subclasses update their UI elements here.
        /// </summary>
        /// <param name="data">Latest flight data from <see cref="FlightDataProvider"/>.</param>
        public abstract void UpdateInstrument(FlightData data);

        /// <summary>Fades this instrument in and marks it visible.</summary>
        public virtual void Show()
        {
            isVisible    = true;
            _targetAlpha = 1f;
            if (CanvasGroupComponent != null)
            {
                CanvasGroupComponent.interactable   = true;
                CanvasGroupComponent.blocksRaycasts = true;
            }
        }

        /// <summary>Fades this instrument out and marks it hidden.</summary>
        public virtual void Hide()
        {
            isVisible    = false;
            _targetAlpha = 0f;
            if (CanvasGroupComponent != null)
            {
                CanvasGroupComponent.interactable   = false;
                CanvasGroupComponent.blocksRaycasts = false;
            }
        }

        #endregion
    }
}
