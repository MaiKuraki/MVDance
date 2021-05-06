using UnityEngine;
using UnityEngine.EventSystems;

namespace MVDance.MapEditor
{
    public class TimelinePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action Action_OnPointerEnter;
        public System.Action Action_OnPointerExit;
        public void OnPointerEnter(PointerEventData eventData)
        {
            Action_OnPointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Action_OnPointerExit?.Invoke();
        }
    }
}