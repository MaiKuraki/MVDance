using System;
using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class MainTimeline : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] Scrollbar scroll_interactable;
        [SerializeField] Scrollbar scroll_actual;
        [SerializeField] Slider slider_actual;
        [SerializeField] Text text_head;
        [SerializeField] Text text_tail;

        public Action<float> Action_OnScrollValueChanged;

        Transform scroll_preview_tr;

        void Awake()
        {
            scroll_preview_tr = scroll_actual.transform;
            scroll_interactable.onValueChanged.AddListener(f => Action_OnScrollValueChanged?.Invoke(f));
        }
        public void UpdateText(string str_head, string str_tail)
        {
            text_head.text = str_head;
            text_tail.text = str_tail;
        }

        public void UpdateScrollSize(float newSize)
        {
            scroll_interactable.size = newSize;
            scroll_actual.size = newSize;
        }

        public float GetProgress() => scroll_interactable.value;
        public float GetActualProgress() => scroll_actual.value;

        public float GetHandleSize() => scroll_interactable.size;

        public void SetProgress(float newProgress)
        {
            scroll_interactable.value = newProgress;
        }
        public void SetProgress_Actual(float newTotalProgress, float newSubProgress)
        {
            scroll_actual.value = newTotalProgress;
            slider_actual.value = newSubProgress;
        }
        public void SyncProgressToActual()
        {
            scroll_interactable.value = scroll_actual.value;
        }
        public void SetActualProgressVisible(bool newShouldActive)
        {
            scroll_preview_tr.gameObject.SetActive(newShouldActive);
        }
    }
}

