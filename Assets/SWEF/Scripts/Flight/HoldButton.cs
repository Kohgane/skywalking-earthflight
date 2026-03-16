using UnityEngine;
using UnityEngine.EventSystems;

namespace SWEF.Flight
{
    /// <summary>
    /// Attach to a UI Button GameObject.
    /// Value = valueWhenHeld while pointer is down, 0 otherwise.
    /// Used for roll-left / roll-right buttons.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float valueWhenHeld = 1f;

        public float Value { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Value = valueWhenHeld;
        public void OnPointerUp(PointerEventData eventData)   => Value = 0f;
        public void OnPointerExit(PointerEventData eventData) => Value = 0f;
    }
}
