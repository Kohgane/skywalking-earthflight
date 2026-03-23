using System.Collections;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Applies post-processing and visual effects when the main camera
    /// is below the water surface.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Underwater colour grading (blue-green tint, reduced saturation).</item>
    ///   <item>Exponential distance fog simulating water scattering.</item>
    ///   <item>Depth-based darkening via fog density.</item>
    ///   <item>Animated caustic light projection on surfaces below water.</item>
    ///   <item>Bubble particle effects.</item>
    ///   <item>Surface-distortion hint for shaders (looking up through water).</item>
    ///   <item>Smooth enter/exit transition over <see cref="transitionDuration"/> seconds.</item>
    ///   <item>Audio-muffling flags for the Audio system.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Uses Unity's legacy <c>RenderSettings</c> fog for underwater fog.
    /// Replace with a volume override if using URP/HDRP post-processing.</para>
    /// </summary>
    public class UnderwaterEffectController : MonoBehaviour
    {
        #region Constants

        private const float DefaultMaxDepth    = 200f;
        private const float MinFogDensity      = 0f;
        private const float MaxFogDensity      = 0.06f;

        private static readonly int ShaderPropUnderwaterBlend = Shader.PropertyToID("_UnderwaterBlend");
        private static readonly int ShaderPropCausticTex      = Shader.PropertyToID("_CausticTex");
        private static readonly int ShaderPropCausticScale    = Shader.PropertyToID("_CausticScale");
        private static readonly int ShaderPropCausticSpeed    = Shader.PropertyToID("_CausticSpeed");

        #endregion

        #region Inspector

        [Header("Underwater Fog")]
        [Tooltip("Base fog colour when just below the surface.")]
        [SerializeField] private Color shallowFogColor = new Color(0.05f, 0.5f, 0.6f, 1f);

        [Tooltip("Fog colour at maximum configured depth.")]
        [SerializeField] private Color deepFogColor = new Color(0.0f, 0.05f, 0.15f, 1f);

        [Tooltip("Maximum depth at which effects are fully applied (metres).")]
        [SerializeField] private float maxDepth = DefaultMaxDepth;

        [Header("Transition")]
        [Tooltip("Duration in seconds over which effects fade in/out on water entry/exit.")]
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.5f;

        [Header("Caustics")]
        [Tooltip("Animated caustic light texture projected onto underwater surfaces.")]
        [SerializeField] private Texture2D causticTexture;

        [Tooltip("World-space tiling scale of the caustic projection.")]
        [SerializeField] private float causticScale = 5f;

        [Tooltip("Scroll speed of the caustic animation (UV units per second).")]
        [SerializeField] private float causticSpeed = 0.04f;

        [Header("Particles")]
        [Tooltip("Bubble particle system (child of this GameObject).")]
        [SerializeField] private ParticleSystem bubbleParticles;

        [Header("Global Material")]
        [Tooltip("Scene-wide material that receives the underwater blend factor. Optional.")]
        [SerializeField] private Material globalUnderwaterMaterial;

        [Header("References")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Public Properties

        /// <summary><c>true</c> when underwater effects are currently active.</summary>
        public bool IsActive => _blendFactor > 0.001f;

        /// <summary>Whether the Audio system should muffle underwater sounds.</summary>
        public bool AudioMuffleActive => IsActive;

        #endregion

        #region Private State

        private OceanManager _mgr;
        private Camera        _mainCam;
        private float         _blendFactor;      // 0 = above water, 1 = fully underwater
        private Coroutine     _transitionCoroutine;

        // Saved pre-underwater fog state.
        private bool  _savedFogEnabled;
        private Color _savedFogColor;
        private float _savedFogDensity;
        private FogMode _savedFogMode;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mgr     = oceanManager != null ? oceanManager : FindFirstObjectByType<OceanManager>();
            _mainCam = Camera.main;
        }

        private void OnEnable()
        {
            if (_mgr != null)
            {
                _mgr.OnUnderwaterEntered += HandleUnderwaterEntered;
                _mgr.OnUnderwaterExited  += HandleUnderwaterExited;
            }
        }

        private void OnDisable()
        {
            if (_mgr != null)
            {
                _mgr.OnUnderwaterEntered -= HandleUnderwaterEntered;
                _mgr.OnUnderwaterExited  -= HandleUnderwaterExited;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            float depth = ComputeDepth();
            ApplyFog(depth);
            ApplyCaustics(depth);
            ApplyGlobalBlend();
        }

        #endregion

        #region Event Handlers

        private void HandleUnderwaterEntered()
        {
            SaveFogState();
            SetBubbles(true);
            StartTransition(targetBlend: 1f);
        }

        private void HandleUnderwaterExited()
        {
            SetBubbles(false);
            StartTransition(targetBlend: 0f, onComplete: RestoreFogState);
        }

        #endregion

        #region Transition

        private void StartTransition(float targetBlend, System.Action onComplete = null)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionRoutine(targetBlend, onComplete));
        }

        private IEnumerator TransitionRoutine(float target, System.Action onComplete)
        {
            float start    = _blendFactor;
            float elapsed  = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed     += Time.deltaTime;
                _blendFactor = Mathf.Lerp(start, target, elapsed / transitionDuration);
                yield return null;
            }

            _blendFactor = target;
            onComplete?.Invoke();
        }

        #endregion

        #region Effect Application

        private void ApplyFog(float depth)
        {
            float t     = Mathf.Clamp01(depth / maxDepth);
            Color baseColor = Color.Lerp(shallowFogColor, deepFogColor, t);
            Color color = new Color(
                baseColor.r * _blendFactor,
                baseColor.g * _blendFactor,
                baseColor.b * _blendFactor,
                baseColor.a);

            RenderSettings.fogColor   = color;
            RenderSettings.fogDensity = Mathf.Lerp(MinFogDensity, MaxFogDensity, _blendFactor) * (1f + t * 2f);
            RenderSettings.fogMode    = FogMode.Exponential;
            RenderSettings.fog        = true;
        }

        private void ApplyCaustics(float depth)
        {
            if (globalUnderwaterMaterial == null || causticTexture == null) return;

            globalUnderwaterMaterial.SetTexture(ShaderPropCausticTex,   causticTexture);
            globalUnderwaterMaterial.SetFloat(ShaderPropCausticScale,   causticScale);
            globalUnderwaterMaterial.SetFloat(ShaderPropCausticSpeed,   causticSpeed);
        }

        private void ApplyGlobalBlend()
        {
            if (globalUnderwaterMaterial == null) return;
            globalUnderwaterMaterial.SetFloat(ShaderPropUnderwaterBlend, _blendFactor);
        }

        #endregion

        #region Helpers

        private float ComputeDepth()
        {
            if (_mainCam == null || _mgr == null) return 0f;
            float waterY = _mgr.GetWaterHeightAtPosition(_mainCam.transform.position);
            return Mathf.Max(0f, waterY - _mainCam.transform.position.y);
        }

        private void SetBubbles(bool active)
        {
            if (bubbleParticles == null) return;
            if (active) bubbleParticles.Play();
            else        bubbleParticles.Stop();
        }

        private void SaveFogState()
        {
            _savedFogEnabled = RenderSettings.fog;
            _savedFogColor   = RenderSettings.fogColor;
            _savedFogDensity = RenderSettings.fogDensity;
            _savedFogMode    = RenderSettings.fogMode;
        }

        private void RestoreFogState()
        {
            RenderSettings.fog        = _savedFogEnabled;
            RenderSettings.fogColor   = _savedFogColor;
            RenderSettings.fogDensity = _savedFogDensity;
            RenderSettings.fogMode    = _savedFogMode;
        }

        #endregion
    }
}
