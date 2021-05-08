using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MVDance.MapEditor
{
    public class LoopPart : MonoBehaviour
    {
        [SerializeField] TimelineInteractable touch;
        [SerializeField] TimelineInteractable handle_l;
        [SerializeField] TimelineInteractable handle_r;
        private Vector2 lastMousePosition;

        void Awake()
        {
            touch.Action_OnBeginDrag += OnBeginDragTouch;
            touch.Action_OnDragging += OnDragTouch;
            touch.Action_OnEndDrag += OnEndDragTouch;
        }

        public void OnBeginDragTouch(PointerEventData eventData)
        {
            lastMousePosition = eventData.position;
        }
        public void OnEndDragTouch(PointerEventData eventData)
        {
            lastMousePosition = eventData.position;
        }
        public void OnDragTouch(PointerEventData eventData)
        {
            Vector2 currentMousePosition = eventData.position;
            Vector2 diff = currentMousePosition - lastMousePosition;
            print(currentMousePosition);
            Vector3 newPosition = transform.localPosition + new Vector3(diff.x, 0);
            transform.localPosition = newPosition;
            lastMousePosition = currentMousePosition;
        }
    }
}
