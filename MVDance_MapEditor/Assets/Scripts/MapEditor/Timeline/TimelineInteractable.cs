using UnityEngine;
using UnityEngine.EventSystems;

namespace MVDance.MapEditor
{
    public class TimelineInteractable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<PointerEventData> Action_OnBeginDrag;
        public System.Action<PointerEventData> Action_OnEndDrag;
        public System.Action<PointerEventData> Action_OnDragging;
        public System.Action<PointerEventData> Action_OnPointerDown;
        public System.Action<PointerEventData> Action_OnPointerUp;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Action_OnBeginDrag?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Action_OnDragging?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Action_OnEndDrag?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Action_OnPointerDown?.Invoke(eventData);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Action_OnPointerUp?.Invoke(eventData);
        }
    }
}

