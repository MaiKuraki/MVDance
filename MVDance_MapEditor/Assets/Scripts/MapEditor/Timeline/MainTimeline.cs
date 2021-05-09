using System;
using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class MainTimeline : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] Scrollbar scroll;
        [SerializeField] Text text_head;
        [SerializeField] Text text_tail;

        public Action<float> Action_OnScrollValueChanged;

        void Awake()
        {
            scroll.onValueChanged.AddListener(f => Action_OnScrollValueChanged?.Invoke(f));
        }
        public void UpdateText(string str_head, string str_tail)
        {
            text_head.text = str_head;
            text_tail.text = str_tail;
        }

        public void UpdateScrollSize(float newSize)
        {
            scroll.size = newSize;
        }

        public void UpdateProgress(float newProgress)
        {
            scroll.value = newProgress;
        }
    }
}

