using System;
using UnityEngine;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class SubTimeline : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] Scrollbar scroll;
        [SerializeField] TimelineInteractable handle;
        [SerializeField] Text handle_text;
        [SerializeField] Text handle_text_copy;

        [Header("--- Grid Config ---")]
        [SerializeField] SubTimelineGrid sub_timeline_grid;

        public Action<float> Action_OnScrollValueChanged;


        float dragStartVal = 0;
        float dragEndVal = 0;
        bool isDragging = false;
        Transform handle_root_tr;
        public float GetProgress() => scroll.value;
        public float GetDragVal() => dragEndVal - dragStartVal;
        public bool IsDragging() => isDragging;
        public void SetGridXPosition(float newPosX)
        {
            sub_timeline_grid.SetGridXPosition(newPosX);
        }
        public void SetGridValueXPosition(float newOffsetPosition)
        {
            sub_timeline_grid.SetGridValueXPosition(newOffsetPosition);
        }
        public void UpdateGridValueTextVal(long startVal)
        {
            sub_timeline_grid.UpdateGridValueTextVal(startVal);
        }
        public double GetGridOriginWidth() => sub_timeline_grid.GetGridOriginWidth();
        public double GetGridWidth() => sub_timeline_grid.GetGridWidth();
        public void SetProgress(float newProgress)
        {
            scroll.value = newProgress;
        }
        public void UpdateHandleText(TimelineType timelineType, double inTimelineTotalTime, double startVal)
        {
            long handleTime = (long)(GetProgress() * inTimelineTotalTime * 1000) + (long)(1000 * startVal);
            string displayStr = "NULL";
            if (timelineType == TimelineType.Second)
            {
                displayStr = string.Format("{0:00.00}", (handleTime / 1000.0f).ToString("f2"));
            }
            handle_text.text = displayStr;
            handle_text_copy.text = displayStr;
        }
        public void RefreshGrid(int newVisibleGridAmount, long newStartVal)
        {
            sub_timeline_grid.RefreshGrid(newVisibleGridAmount, newStartVal);
        }
        public void SetCursorVisible(bool bNewShouldVidible)
        {
            handle_root_tr.gameObject.SetActive(bNewShouldVidible);
        }
        private void Awake()
        {
            handle_root_tr = handle_text.transform;

            scroll.onValueChanged.AddListener(f => Action_OnScrollValueChanged?.Invoke(f));

            handle.Action_OnDragging += d =>
            {
                //  do not remove.
                scroll.OnDrag(d);
            };
            handle.Action_OnPointerDown += d =>
            {
                //  hold is drag, hold is pause
                dragStartVal = scroll.value;
                isDragging = true;
            };
            handle.Action_OnPointerUp += d =>
            {
                //  hold is drag, hold is pause
                dragEndVal = scroll.value;
                isDragging = false;
            };
        }
    }
}

