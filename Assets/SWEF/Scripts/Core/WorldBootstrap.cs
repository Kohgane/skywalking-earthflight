using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// On World scene start: sets CesiumGeoreference origin to the player's GPS location
    /// and positions the PlayerRig above the ground.
    /// 
    /// NOTE: Cesium-specific code is wrapped in #if CESIUM_FOR_UNITY to allow compilation
    /// before the Cesium package is installed. Once Cesium for Unity is installed,
    /// the define symbol is automatically available.
    /// </summary>
    public class WorldBootstrap : MonoBehaviour
    {
        [Header("Scene refs")]
        [Tooltip("Assign the GameObject that has CesiumGeoreference component")]
        [SerializeField] private GameObject georeference;
        [SerializeField] private Transform playerRig;

        [Header("Start pose")]
        [SerializeField] private float startLocalHeightMeters = 30f;

        private void Awake()
        {
            if (playerRig == null)
                playerRig = GameObject.Find("PlayerRig")?.transform;
        }

        private void Start()
        {
            if (playerRig == null)
            {
                Debug.LogError("[SWEF] PlayerRig not found in World scene.");
                return;
            }

            if (SWEFSession.HasFix && georeference != null)
            {
                SetGeoreferenceOrigin();
            }
            else if (!SWEFSession.HasFix)
            {
                Debug.LogWarning("[SWEF] No GPS fix available. Using default georeference origin.");
            }

            // Position player rig above the origin
            var p = playerRig.localPosition;
            playerRig.localPosition = new Vector3(p.x, startLocalHeightMeters, p.z);
        }

        private void SetGeoreferenceOrigin()
        {
#if CESIUM_FOR_UNITY
            var geo = georeference.GetComponent<CesiumForUnity.CesiumGeoreference>();
            if (geo != null)
            {
                geo.SetOriginLongitudeLatitudeHeight(SWEFSession.Lon, SWEFSession.Lat, SWEFSession.Alt);
                Debug.Log($"[SWEF] Georeference origin set to ({SWEFSession.Lat}, {SWEFSession.Lon}, {SWEFSession.Alt})");
            }
            else
            {
                Debug.LogError("[SWEF] CesiumGeoreference component not found on assigned GameObject.");
            }
#else
            Debug.LogWarning("[SWEF] Cesium for Unity not installed. Georeference origin not set. " +
                             "Install the Cesium for Unity package to enable 3D Tiles.");
#endif
        }
    }
}
