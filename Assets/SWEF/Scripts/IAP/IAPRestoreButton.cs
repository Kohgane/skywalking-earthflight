using UnityEngine;
using UnityEngine.UI;

namespace SWEF.IAP
{
    /// <summary>
    /// Attach to a UI Button to give players a "Restore Purchases" action.
    /// The button is only visible on iOS (and in the Unity Editor for testing).
    /// It disables itself while the restore operation is in progress and
    /// re-enables once <see cref="IAPManager.OnRestoreComplete"/> fires.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class IAPRestoreButton : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Optional feedback label")]
        [Tooltip("Optional Text component to display restore status messages.")]
        [SerializeField] private Text statusLabel;

        #endregion

        #region Private Fields

        private Button _button;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();

            // Show only on iOS or in the Editor
            bool shouldShow = Application.platform == RuntimePlatform.IPhonePlayer
#if UNITY_EDITOR
                              || true
#endif
                              ;

            gameObject.SetActive(shouldShow);
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);

            if (IAPManager.Instance != null)
                IAPManager.Instance.OnRestoreComplete += HandleRestoreComplete;
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);

            if (IAPManager.Instance != null)
                IAPManager.Instance.OnRestoreComplete -= HandleRestoreComplete;
        }

        #endregion

        #region Button Callback

        private void OnClick()
        {
            if (IAPManager.Instance == null)
            {
                Debug.LogWarning("[SWEF] IAPRestoreButton: IAPManager.Instance is null.");
                return;
            }

            _button.interactable = false;

            if (statusLabel != null)
                statusLabel.text = "Restoring…";

            IAPManager.Instance.RestorePurchases();
        }

        #endregion

        #region Restore Callback

        private void HandleRestoreComplete(bool success)
        {
            _button.interactable = true;

            if (statusLabel != null)
                statusLabel.text = success ? "Restore complete ✓" : "Restore failed";
        }

        #endregion
    }
}
