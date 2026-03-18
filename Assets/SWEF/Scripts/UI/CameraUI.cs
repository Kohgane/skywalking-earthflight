using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;

namespace SWEF.UI
{
    /// <summary>
    /// Provides UI controls for switching <see cref="CameraController"/> modes.
    /// Wires a cycle button, individual mode buttons, and a text label showing the current mode.
    /// </summary>
    public class CameraUI : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Button cycleCameraButton;
        [SerializeField] private Text cameraModeText;
        [SerializeField] private Button[] modeButtons;

        private void Awake()
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<CameraController>();

            if (cycleCameraButton != null)
                cycleCameraButton.onClick.AddListener(OnCycleClicked);

            if (modeButtons != null)
            {
                for (int i = 0; i < modeButtons.Length; i++)
                {
                    if (modeButtons[i] == null) continue;
                    int modeIndex = i;
                    modeButtons[i].onClick.AddListener(() =>
                        cameraController?.SetCameraMode((CameraController.CameraMode)modeIndex));
                }
            }

            if (cameraController != null)
                cameraController.OnCameraModeChanged += UpdateModeText;
        }

        private void Start()
        {
            if (cameraController != null)
                UpdateModeText(cameraController.CurrentMode);
        }

        private void OnDestroy()
        {
            if (cameraController != null)
                cameraController.OnCameraModeChanged -= UpdateModeText;
        }

        private void OnCycleClicked()
        {
            cameraController?.CycleCamera();
        }

        private void UpdateModeText(CameraController.CameraMode mode)
        {
            if (cameraModeText != null)
                cameraModeText.text = mode.ToString();
        }
    }
}
