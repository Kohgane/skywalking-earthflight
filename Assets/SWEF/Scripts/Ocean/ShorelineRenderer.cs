using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Renders shoreline effects where water meets land.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Animated foam strip along the waterline, controlled by
    ///         <see cref="ShorelineSettings"/>.</item>
    ///   <item>Wave-break foam using a depth/terrain-height comparison approach.</item>
    ///   <item>Wet-sand darkening near the water's edge.</item>
    ///   <item>Optional splash <see cref="ParticleSystem"/> at impact points.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Attach this component to a water-body GameObject.  Wire the
    /// <see cref="shorelineMaterial"/> and optionally the <see cref="beachSplashPrefab"/>
    /// in the Inspector.</para>
    /// </summary>
    public class ShorelineRenderer : MonoBehaviour
    {
        #region Constants

        private static readonly int ShaderPropFoamWidth    = Shader.PropertyToID("_FoamWidth");
        private static readonly int ShaderPropFoamIntensity = Shader.PropertyToID("_FoamIntensity");
        private static readonly int ShaderPropFoamScrollX  = Shader.PropertyToID("_FoamScrollX");
        private static readonly int ShaderPropFoamScrollZ  = Shader.PropertyToID("_FoamScrollZ");
        private static readonly int ShaderPropWaterlineY   = Shader.PropertyToID("_WaterlineY");
        private static readonly int ShaderPropWetRange     = Shader.PropertyToID("_WetRange");
        private static readonly int ShaderPropTime         = Shader.PropertyToID("_WaveTime");

        private const float FullDetailDistance  = 200f;
        private const float NoDetailDistance    = 1000f;

        #endregion

        #region Inspector

        [Header("Settings")]
        [Tooltip("Shoreline visual settings (foam width, intensity, etc.).")]
        [SerializeField] private ShorelineSettings settings = new ShorelineSettings();

        [Header("Rendering")]
        [Tooltip("Material used to render the shoreline foam strip.")]
        [SerializeField] private Material shorelineMaterial;

        [Tooltip("MeshRenderer(s) that should receive wet-sand darkening via property block.")]
        [SerializeField] private Renderer[] terrainRenderers;

        [Header("Particles")]
        [Tooltip("Prefab for the optional beach-splash particle effect spawned at wave impacts.")]
        [SerializeField] private GameObject beachSplashPrefab;

        [Tooltip("Maximum number of simultaneous splash particles.")]
        [SerializeField, Range(0, 20)] private int maxSplashInstances = 5;

        [Header("References")]
        [Tooltip("OceanManager — resolved at runtime if null.")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Private State

        private OceanManager          _mgr;
        private MaterialPropertyBlock _propBlock;
        private GameObject[]          _splashPool;
        private int                   _splashIndex;
        private float                 _lastSplashTime;
        private const float           SplashCooldown = 1.5f;

        // Scrolling foam UV accumulators.
        private float _foamScrollX;
        private float _foamScrollZ;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mgr       = oceanManager != null ? oceanManager : FindFirstObjectByType<OceanManager>();
            _propBlock = new MaterialPropertyBlock();

            if (beachSplashPrefab != null && maxSplashInstances > 0)
                InitSplashPool();
        }

        private void Update()
        {
            if (_mgr == null) return;

            float waveTime = _mgr.WaveTime;
            UpdateFoamScroll(waveTime);
            UpdateShaderProperties(waveTime);
            TryEmitSplash(waveTime);
        }

        #endregion

        #region Foam Animation

        private void UpdateFoamScroll(float waveTime)
        {
            // Scroll foam texture along shore wave direction.
            float sp = settings.shoreWaveSpeed * Time.deltaTime;
            _foamScrollX += sp * 0.5f;
            _foamScrollZ += sp;
        }

        private void UpdateShaderProperties(float waveTime)
        {
            if (shorelineMaterial == null) return;

            // Distance-based quality fade.
            float dist = DistanceToCamera();
            float alpha = 1f - Mathf.InverseLerp(FullDetailDistance, NoDetailDistance, dist);

            _propBlock.SetFloat(ShaderPropFoamWidth,    settings.foamWidth);
            _propBlock.SetFloat(ShaderPropFoamIntensity, settings.foamIntensity * alpha);
            _propBlock.SetFloat(ShaderPropFoamScrollX,  _foamScrollX);
            _propBlock.SetFloat(ShaderPropFoamScrollZ,  _foamScrollZ);
            _propBlock.SetFloat(ShaderPropWaterlineY,   transform.position.y);
            _propBlock.SetFloat(ShaderPropWetRange,     settings.wetSandDarkeningRange);
            _propBlock.SetFloat(ShaderPropTime,         waveTime);

            if (terrainRenderers != null)
            {
                foreach (var r in terrainRenderers)
                {
                    if (r != null)
                        r.SetPropertyBlock(_propBlock);
                }
            }
        }

        #endregion

        #region Beach Splash

        private void InitSplashPool()
        {
            _splashPool = new GameObject[maxSplashInstances];
            for (int i = 0; i < maxSplashInstances; i++)
            {
                _splashPool[i] = Instantiate(beachSplashPrefab, transform);
                _splashPool[i].SetActive(false);
            }
        }

        private void TryEmitSplash(float waveTime)
        {
            if (_splashPool == null || _splashPool.Length == 0) return;
            if (Time.time - _lastSplashTime < SplashCooldown) return;

            // Spawn a splash offset slightly from the waterline.
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r     = Random.Range(2f, settings.foamWidth + 2f);
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            var splash = _splashPool[_splashIndex % _splashPool.Length];
            _splashIndex++;
            splash.transform.position = pos;
            splash.SetActive(true);

            if (splash.TryGetComponent<ParticleSystem>(out var ps))
                ps.Play();

            _lastSplashTime = Time.time;
        }

        #endregion

        #region Helpers

        private float DistanceToCamera()
        {
            Camera cam = Camera.main;
            return cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 0f;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, settings.foamWidth);
            Gizmos.color = new Color(0f, 0.4f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, settings.waveBreakDistance);
        }
#endif
    }
}
