using System.Collections;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Manages marine wildlife movement, surfacing, breaching,
    /// and water-surface integration.
    /// </summary>
    public class MarineLifeController : MonoBehaviour
    {
        #region Inspector

        [Header("Movement")]
        [Tooltip("Default swim depth below water surface in metres.")]
        [SerializeField] private float swimDepth = 5f;

        [Tooltip("Dolphin leap height above water in metres.")]
        [SerializeField] private float dolphinLeapHeight = 3f;

        [Tooltip("Whale breach height above water in metres.")]
        [SerializeField] private float whaleBreachHeight  = 8f;

        [Tooltip("Interval in seconds between surfacing events.")]
        [SerializeField] private float surfacingInterval = 30f;

        [Header("Species")]
        [SerializeField] private WildlifeCategory marineCategory = WildlifeCategory.MarineMammal;

        #endregion

        #region Private State

        private WildlifeBehavior _currentBehavior = WildlifeBehavior.Roaming;
        private float _waterHeight;
        private float _surfaceTimer;
        private bool _isSurfacing;
        private Coroutine _surfacingCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _surfaceTimer = surfacingInterval;
            UpdateWaterHeight();
        }

        private void Update()
        {
            UpdateWaterHeight();
            ApplySwimDepth();

            _surfaceTimer -= Time.deltaTime;
            if (_surfaceTimer <= 0f && !_isSurfacing &&
                _currentBehavior != WildlifeBehavior.Fleeing)
            {
                _surfaceTimer = surfacingInterval;
                _surfacingCoroutine = StartCoroutine(SurfacingEvent());
            }
        }

        #endregion

        #region Water Integration

        private void UpdateWaterHeight()
        {
            _waterHeight = 0f;
#if SWEF_WATER_AVAILABLE
            var wsm = SWEF.Water.WaterSurfaceManager.Instance;
            if (wsm != null)
                _waterHeight = wsm.GetWaterHeight(transform.position);
#endif
        }

        private void ApplySwimDepth()
        {
            if (_isSurfacing) return;
            float targetY = _waterHeight - swimDepth;
            Vector3 pos   = transform.position;
            pos.y         = Mathf.Lerp(pos.y, targetY, 5f * Time.deltaTime);
            transform.position = pos;
        }

        #endregion

        #region Surfacing / Breach

        private IEnumerator SurfacingEvent()
        {
            _isSurfacing = true;
            float leapHeight = marineCategory == WildlifeCategory.Fish
                ? 0.5f
                : dolphinLeapHeight;

            // Rise to surface
            float t = 0f;
            Vector3 startPos = transform.position;
            Vector3 surfacePos = new Vector3(startPos.x, _waterHeight + leapHeight * 0.1f, startPos.z);
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                transform.position = Vector3.Lerp(startPos, surfacePos, t);
                yield return null;
            }

            // Brief surface
            TriggerSplash(SplashSize.Medium);
            yield return new WaitForSeconds(1.5f);

            // Return to swim depth
            t = 0f;
            startPos = transform.position;
            Vector3 divePos = new Vector3(startPos.x, _waterHeight - swimDepth, startPos.z);
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.position = Vector3.Lerp(startPos, divePos, t);
                yield return null;
            }

            _isSurfacing = false;
        }

        private IEnumerator BreachCoroutine()
        {
            _isSurfacing = true;
            Vector3 startPos = transform.position;
            Vector3 apex     = new Vector3(startPos.x, _waterHeight + whaleBreachHeight, startPos.z);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                float parabola  = 4f * t * (1f - t); // parabolic arc
                transform.position = Vector3.Lerp(startPos, apex, t) +
                                     Vector3.up * parabola * whaleBreachHeight * 0.2f;
                yield return null;
            }

            TriggerSplash(SplashSize.Large);
            yield return new WaitForSeconds(0.5f);

            t = 0f;
            startPos = transform.position;
            Vector3 endPos = new Vector3(startPos.x, _waterHeight - swimDepth, startPos.z);
            while (t < 1f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            _isSurfacing = false;
        }

        private void TriggerSplash(SplashSize size)
        {
#if SWEF_WATER_AVAILABLE
            var splash = SWEF.Water.SplashEffectController.Instance;
            splash?.SpawnSplash(transform.position, size == SplashSize.Large ? 3f : 1f);
#endif
        }

        private enum SplashSize { Medium, Large }

        #endregion

        #region Public API

        /// <summary>Forces a whale breach animation.</summary>
        public void TriggerBreach()
        {
            if (!_isSurfacing)
                _surfacingCoroutine = StartCoroutine(BreachCoroutine());
        }

        /// <summary>Adjusts the default swim depth.</summary>
        public void SetSwimDepth(float depth)
        {
            swimDepth = Mathf.Max(0f, depth);
        }

        /// <summary>Called by AnimalGroupController when behavior changes.</summary>
        public void OnBehaviorChanged(WildlifeBehavior behavior)
        {
            _currentBehavior = behavior;
            if (behavior == WildlifeBehavior.Fleeing)
            {
                if (_surfacingCoroutine != null) StopCoroutine(_surfacingCoroutine);
                _isSurfacing  = false;
                swimDepth     = 20f;   // dive deep
                _surfaceTimer = surfacingInterval * 2f;
            }
            else if (behavior == WildlifeBehavior.Roaming)
            {
                swimDepth = 5f;
            }
        }

        #endregion
    }
}
