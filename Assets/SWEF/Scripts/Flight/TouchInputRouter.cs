using UnityEngine;

namespace SWEF.Flight
{
    /// <summary>
    /// Routes touch/mouse input to FlightController.
    /// Touch (or mouse drag): yaw/pitch.
    /// Roll: HoldButton left/right (assigned in Inspector).
    /// </summary>
    public class TouchInputRouter : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private HoldButton rollLeft;
        [SerializeField] private HoldButton rollRight;

        [Header("Look Sensitivity")]
        [SerializeField] private float sensitivity = 1.4f;
        [SerializeField] private float maxAxis = 1.0f;

        private Vector2 _lastPos;
        private bool _dragging;

        private void Awake()
        {
            if (flight == null) flight = GetComponent<FlightController>();
        }

        private void Update()
        {
            if (flight == null) return;

            float yaw = 0f;
            float pitch = 0f;

            // Touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    _dragging = true;
                    _lastPos = t.position;
                }
                else if (_dragging && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                {
                    Vector2 delta = t.position - _lastPos;
                    _lastPos = t.position;
                    yaw   = Mathf.Clamp(delta.x / Screen.width  * sensitivity, -maxAxis, maxAxis);
                    pitch = Mathf.Clamp(-delta.y / Screen.height * sensitivity, -maxAxis, maxAxis);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    _dragging = false;
                }
            }
            else
            {
                // Mouse input (editor/PC)
                if (Input.GetMouseButtonDown(0))
                {
                    _dragging = true;
                    _lastPos = Input.mousePosition;
                }
                else if (_dragging && Input.GetMouseButton(0))
                {
                    Vector2 pos = Input.mousePosition;
                    Vector2 delta = pos - _lastPos;
                    _lastPos = pos;
                    yaw   = Mathf.Clamp(delta.x / Screen.width  * sensitivity, -maxAxis, maxAxis);
                    pitch = Mathf.Clamp(-delta.y / Screen.height * sensitivity, -maxAxis, maxAxis);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _dragging = false;
                }
            }

            // Roll from hold buttons
            float roll = 0f;
            if (rollLeft  != null) roll -= rollLeft.Value;
            if (rollRight != null) roll += rollRight.Value;

            flight.Step(yaw, pitch, roll);
        }
    }
}
