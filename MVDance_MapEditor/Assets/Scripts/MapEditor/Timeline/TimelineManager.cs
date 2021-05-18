using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using RyanNielson.InputBinder;

namespace MVDance.MapEditor
{
    public enum TimelineType
    {
        Second,
        Beat,
        Frame
    }

    public class TimelineManager : MonoBehaviour
    {
        [Header("-- Timeline --")]
        [SerializeField] MainTimeline timeline_main;
        [SerializeField] SubTimeline timeline_sub;

        [Header("-- Timeline Config --")]
        [SerializeField] TimelinePanel timeline_panel;
        [SerializeField] TimelineInteractable timeline_handle;

        [Header("-- Test Val --")]
        [SerializeField] double YourTotalTimeSet_Sec = 35.5;

        InputBinder inputComponent;


        bool isMouseHoverOnTimeline = false;
        bool canRunTimeline = true;
        bool isPaused = false;
        Action Action_OnTimelineScaled;
        Action Action_OnTimelinePreScale;
        TimelineType eTimelineType = TimelineType.Second;

        int max_visible_grid_amount = 20;
        int min_main_grid_amount = 1;
        int visible_main_grid_amount;

        private void Awake()
        {
            timeline_panel.Action_OnPointerEnter += () => isMouseHoverOnTimeline = true;
            timeline_panel.Action_OnPointerExit += () => isMouseHoverOnTimeline = false;

            timeline_handle.Action_OnPointerDown += dragOffset =>
            {
                //  hold is drag, hold is pause
                if (!isPaused) UpdateCanRun(false);
            };
            timeline_handle.Action_OnPointerUp += dragOffset =>
            {
                //  hold is drag, hold is pause
                sub_timeline_drag_offset += timelineTotalTime * timeline_sub.GetDragVal();
                if (!isPaused) UpdateCanRun(true);
            };

            timeline_sub.Action_OnScrollValueChanged += f =>
            {
                int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
                long mainTimelineStartVal = (long)(timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
                timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, (float)(mainTimelineStartVal / 1000.0));
            };
            timeline_main.Action_OnScrollValueChanged += f =>
            {
                int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
                long startValFloor = Mathf.FloorToInt((float)(timeline_main.GetProgress() * timelineTotalTime * (totalCount - 1)));
                
                UpdateMainTimelineBarText(totalCount);
                UpdateSubtimelineOffset(startValFloor);
            };

            Action_OnTimelineScaled += () =>
            {
                int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
                long mainTimelineStartVal = (long)(timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long startValFloor = Mathf.FloorToInt((float)(timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1)));

                timeline_sub.RefreshGrid(visible_main_grid_amount, startValFloor);
                
                RefreshTotalTimeLine(totalPageAmount);            
                UpdateMainTimelineBarText(totalPageAmount);
                UpdateSubtimelineOffset(startValFloor);

                timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, (float)(mainTimelineStartVal / 1000.0));

                TryLockGridToCursor();
            };
            Action_OnTimelinePreScale += () =>
            {
                TryLockGridToCursor();
            };

            inputComponent = GetComponent<InputBinder>();
            inputComponent.BindAxis("Mouse ScrollWheel", TryScaleTimeline);
            inputComponent.BindKey(KeyCode.R, InputEvent.Pressed, ResetTimeline);
            inputComponent.BindKey(KeyCode.Space, InputEvent.Pressed, UpdatePauseState);
        }

        private void Start()
        {
            InitTimeline();
        }

        void UpdateCanRun(bool newCanRunTimeline)
        {
            canRunTimeline = newCanRunTimeline;

            if (canRunTimeline)
            {
                pauseEndTime = DateTime.Now.Ticks;
            }
            else
            {
                pauseStartTime = DateTime.Now.Ticks;
            }

            timelinePausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }

        void UpdatePauseState()
        {
            if (bFirstTimePlaying)
            {
                TryRunSubTimeline();
                bFirstTimePlaying = false;
                return;
            }

            isPaused = !isPaused;

            if (isPaused)
            {
                pauseStartTime = DateTime.Now.Ticks;
            }
            else
            {
                pauseEndTime = DateTime.Now.Ticks;
            }

            timelinePausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }
        double GetDragOffset_Seconds()
        {
            return sub_timeline_drag_offset;
        }
        double GetMainActualTimeOffset()
        {
            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
            long subTimelineStartVal = (long)(timeline_main.GetActualProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
            long mainTimelineStartVal = (long)(timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
            double main_timeline_drag_offset = (mainTimelineStartVal - subTimelineStartVal) / 1000.0;
            return main_timeline_drag_offset;
        }
        long GetPausedTime() => timelinePausedTime;
        bool CanRunTimeline() => canRunTimeline && !isPaused;
        bool CanScale() => isMouseHoverOnTimeline;
        bool IsTimelineRunning() => pauseStartTime < pauseEndTime || !bFirstTimePlaying;
        private void TryScaleTimeline(float mouseWheelVal)
        {
            if (CanScale() && (mouseWheelVal > 0.01f || mouseWheelVal < -0.01f))
            {
                // print(isMouseHoverOnTimeline);
                Action_OnTimelinePreScale?.Invoke();
                if (mouseWheelVal > 0.01f)
                {
                    ScaleUpTimeline();
                }
                else if (mouseWheelVal < -0.01f)
                {
                    ScaleDownTimeline();
                }
                Action_OnTimelineScaled?.Invoke();
            }
        }
        private void UpdateSubtimelineOffset(long startVal)
        {
            int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));

            //  there is one more invisible grid out of screen
            double oneGridWidth = timeline_sub.GetGridWidth() / (max_visible_grid_amount + 1);
            //  decrease main progress bar size
            double gridFullWidth = oneGridWidth * visible_main_grid_amount * (totalCount - 1);
            double offsetX = (((int)(timeline_main.GetProgress() * gridFullWidth * 1000) % (oneGridWidth * 1000)) / 1000.0);

            timeline_sub.SetGridXPosition(-(float)offsetX);
            timeline_sub.SetGridValueXPosition(-(float)offsetX);
            timeline_sub.UpdateGridValueTextVal(startVal);
        }
        private void UpdateMainTimelineBarText(int totalPageAmount)
        {
            string main_timeline_head_str = (timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1)).ToString("f2");
            string main_timeline_tail_str = (timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) + timelineTotalTime).ToString("f2");
            timeline_main.UpdateText(main_timeline_head_str, main_timeline_tail_str);
        }

        long start_time = 0;
        long pauseStartTime = 0;
        long pauseEndTime = 0;
        long timelinePausedTime = 0;
        double sub_timeline_drag_offset = 0;
        bool bFirstTimePlaying = true;

        double timelineProgressTime = 0;
        double timelineTotalTime = 0;


        Coroutine task_updateTime = null;

        void ResetTimeline()
        {
            ResetSubTimeline();
        }
        void ResetSubTimeline()
        {
            timelinePausedTime = 0;
            sub_timeline_drag_offset = 0;
            canRunTimeline = true;
            isPaused = false;
            if (task_updateTime != null)
            {
                StopCoroutine(task_updateTime);
                task_updateTime = null;
            }
        }
        void TryRunSubTimeline()
        {
            start_time = DateTime.Now.Ticks;

            if (task_updateTime != null)
            {
                StopCoroutine(task_updateTime);
                task_updateTime = null;
            }
            task_updateTime = StartCoroutine(Task_UpdateTime());
        }
        void TryRunSubTimelineFromStart()
        {
            ResetSubTimeline();
            TryRunSubTimeline();
        }

        IEnumerator Task_UpdateTime()
        {
            //double progress = GetTotalProgressedTime() / YourTotalTimeSet_Sec * 1000.0;
            long yourTimeSetMilliSec = (long)(YourTotalTimeSet_Sec * 1000.0);
            while (GetTotalProgressedTime() <= yourTimeSetMilliSec)
            {
                yield return new WaitUntil(CanRunTimeline);



                int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
                int pageIndex = Mathf.FloorToInt((float)((GetTotalProgressedTime() / 1000.0) / timelineTotalTime));
                float mainProgressVal = totalPageAmount > 1 ? (float)((pageIndex * 2.0 / (totalPageAmount - 1)) / 2) : 0;
                float subProgressVal = (GetTotalProgressedTime() - (long)(pageIndex * timelineTotalTime * 1000.0)) / (float)(timelineTotalTime * 1000.0);
                
                timeline_main.SetProgress_Actual(mainProgressVal, subProgressVal);

                long subTimelineStartVal = (long)(timeline_main.GetActualProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long subTimelineEndVal = (long)((timeline_main.GetActualProgress() * timelineTotalTime * (totalPageAmount - 1) + timelineTotalTime) * 1000.0);
                long mainTimelineStartVal = (long)(timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long mainTimelineEndVal = (long)((timeline_main.GetProgress() * timelineTotalTime * (totalPageAmount - 1) + timelineTotalTime) * 1000.0);

                timeline_sub.SetProgress(subProgressVal);
                timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, (float)(mainTimelineStartVal / 1000.0));

                timeline_main.SyncProgressToActual();

            }
        }

        long GetTotalProgressedTime()
        {
            long result = -1;

            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - GetPausedTime() - start_time);
            result = (long)(ts.TotalMilliseconds + GetDragOffset_Seconds() * 1000);

            return result >= 0 ? result : 0;
        }

        void ScaleUpTimeline()
        {
            if (visible_main_grid_amount > min_main_grid_amount) visible_main_grid_amount--;

            timelineTotalTime = visible_main_grid_amount;
        }
        void ScaleDownTimeline()
        {
            if (visible_main_grid_amount < max_visible_grid_amount) visible_main_grid_amount++;

            timelineTotalTime = visible_main_grid_amount;
        }
        void InitTimeline()
        {
            /** ------ */
            visible_main_grid_amount = max_visible_grid_amount;
            timelineTotalTime = visible_main_grid_amount;
   
            long startVal = 0;
            int pageIndex = 0;
            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / timelineTotalTime));
            float mainProgressVal = totalPageAmount > 1 ? (((float)pageIndex / (totalPageAmount - 1))) : 0;
            timeline_main.UpdateScrollSize(1.0f / totalPageAmount);
            timeline_main.SetProgress(mainProgressVal);
            timeline_main.SetProgress_Actual(mainProgressVal, 0);

            timeline_sub.SetProgress(0);
            timeline_sub.RefreshGrid(visible_main_grid_amount, startVal);
            timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, startVal);

            RefreshTotalTimeLine(totalPageAmount);
            UpdateMainTimelineBarText(totalPageAmount);
            UpdateSubtimelineOffset(startVal);
        }

        void RefreshTotalTimeLine(int totalPageAmount)
        {
            timeline_main.UpdateScrollSize(1.0f / totalPageAmount);
        }

        void TryLockGridToCursor()
        {
            if (IsTimelineRunning())
            {
                Debug.Log("Timeline running, skip lock");
                return;
            }

            // print($"timelineBar: {timeline_bar_rt.rect.width}, Grid: {grid_rt.rect.width}");
        }
    }
}


