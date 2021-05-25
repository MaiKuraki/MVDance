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
        public Action<long> OnTimelinePlaying;
        public Action OnTimelinePaused;
        public Action<long> OnTimelineValueChanged;
        public Action OnDragMainTimeline;

        [Header("-- Timeline --")]
        [SerializeField] MainTimeline timeline_main;
        [SerializeField] SubTimeline timeline_sub;

        [Header("-- Timeline Config --")]
        [SerializeField] TimelinePanel timeline_panel;
        [SerializeField] TimelineInteractable timeline_handle;

        [Header("-- Test Val --")]
        [SerializeField] double YourTotalTimeSet_Sec = 35.5;

        InputBinder inputComponent;

        bool bAutoResetMainTimelineOffset = false;
        bool isMouseHoverOnTimeline = false;
        bool canRunTimeline = true;
        bool isPaused = false;
        Action Action_OnTimelineScaled;
        Action Action_OnTimelinePreScale;
        TimelineType eTimelineType = TimelineType.Second;

        int max_visible_grid_amount = 20;
        int min_main_grid_amount = 1;
        int visible_main_grid_amount;

        public void SetMainTimerlineTotalTime(double newTime_Sec)
        {
            YourTotalTimeSet_Sec = newTime_Sec;
        }

        public void SetAutoResetMainTimelineOffset(bool newShouldAutoReset)
        {
            bAutoResetMainTimelineOffset = newShouldAutoReset;
        }

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
                sub_timeline_drag_offset += subTimelineTotalTime * timeline_sub.GetDragVal();
                if (!isPaused) UpdateCanRun(true);
            };

            timeline_sub.Action_OnScrollValueChanged += f =>
            {
                UpdateSubtimelineCursorVisual();

                OnTimelineValueChanged?.Invoke(GetTotalProgressedTime_Ms());
            };
            timeline_main.Action_OnScrollValueChanged += f =>
            {
                int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
                long startValFloor = Mathf.FloorToInt((float)(timeline_main.GetProgress() * subTimelineTotalTime * (totalCount - 1)));
                
                UpdateMainTimelineBarText(totalCount);
                
                UpdateSubtimelineOffset(startValFloor);
                UpdateSubTimelineProgress();

                //  TODO: TRY TO GET IS USER DRAG
                bool isUserDrag = true;
                if(isUserDrag) OnDragMainTimeline?.Invoke();
            };

            Action_OnTimelineScaled += () =>
            {
                int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
                long mainTimelineActualStartVal = (long)(timeline_main.GetActualProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long startValFloor = Mathf.FloorToInt((float)(timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1)));

                timeline_sub.RefreshGrid(visible_main_grid_amount, startValFloor);
                
                RefreshTotalTimeLine(totalPageAmount);            
                UpdateMainTimelineBarText(totalPageAmount);

                UpdateSubtimelineOffset(startValFloor);
                UpdateSubTimelineProgress();
                UpdateSubtimelineCursorVisual();

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
                OnTimelinePlaying?.Invoke(GetTotalProgressedTime_Ms());
            }
            else
            {
                pauseStartTime = DateTime.Now.Ticks;
                OnTimelinePaused?.Invoke();
            }

            timelinePausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }

        void UpdatePauseState()
        {
            if (bFirstTimePlaying)
            {
                TryRunSubTimeline();
                bFirstTimePlaying = false;
                OnTimelinePlaying?.Invoke(0);
                return;
            }

            isPaused = !isPaused;

            if (isPaused)
            {
                pauseStartTime = DateTime.Now.Ticks;
                OnTimelinePaused?.Invoke();
            }
            else
            {
                pauseEndTime = DateTime.Now.Ticks;
                OnTimelinePlaying?.Invoke(GetTotalProgressedTime_Ms());
            }

            timelinePausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }
        double GetDragOffset_Seconds()
        {
            return sub_timeline_drag_offset;
        }
        double GetMainActualTimeOffset()
        {
            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
            long subTimelineStartVal = (long)(timeline_main.GetActualProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
            long mainTimelineStartVal = (long)(timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
            double main_timeline_drag_offset = (mainTimelineStartVal - subTimelineStartVal) / 1000.0;
            return main_timeline_drag_offset;
        }
        long GetPausedTime()
        {
            if (isPaused) return DateTime.Now.Ticks - pauseStartTime + timelinePausedTime;
            else return timelinePausedTime;
        }
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
            int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));

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
            string main_timeline_head_str = (timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1)).ToString("f2");
            string main_timeline_tail_str = (timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) + subTimelineTotalTime).ToString("f2");
            timeline_main.UpdateText(main_timeline_head_str, main_timeline_tail_str);
        }

        long start_time = 0;
        long pauseStartTime = 0;
        long pauseEndTime = 0;
        long timelinePausedTime = 0;
        double sub_timeline_drag_offset = 0;
        bool bFirstTimePlaying = true;

        double subTimelineTotalTime = 0;


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

        private void UpdateSubTimelineProgress()
        {
            int pageIndex = Mathf.FloorToInt((float)((GetTotalProgressedTime_Ms() / 1000.0) / subTimelineTotalTime));
            float subProgressActualVal = (GetTotalProgressedTime_Ms() - (long)(pageIndex * subTimelineTotalTime * 1000.0)) / (float)(subTimelineTotalTime * 1000.0);
            float subProgressVal = subProgressActualVal - (float)(GetMainActualTimeOffset() / subTimelineTotalTime);
            timeline_sub.SetProgress(subProgressVal);

            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
            long mainTimelineStartVal = (long)(timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
            long mainTimelineEndVal = (long)((timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) + subTimelineTotalTime) * 1000.0);
            if (GetTotalProgressedTime_Ms() < mainTimelineStartVal || GetTotalProgressedTime_Ms() > mainTimelineEndVal)
            {
                if (bAutoResetMainTimelineOffset)
                {
                    timeline_main.SyncProgressToActual();
                }
            }
        }

        private void UpdateSubtimelineCursorVisual()
        {
            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
            long mainTimelineStartVal = (long)(timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
            long mainTimelineEndVal = (long)((timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) + subTimelineTotalTime) * 1000.0);
            timeline_sub.UpdateHandleText(eTimelineType, subTimelineTotalTime, (float)(mainTimelineStartVal / 1000.0));
            timeline_sub.SetCursorVisible(mainTimelineStartVal <= GetTotalProgressedTime_Ms() && GetTotalProgressedTime_Ms() <= mainTimelineEndVal);
        }

        IEnumerator Task_UpdateTime()
        {
            //double progress = GetTotalProgressedTime() / YourTotalTimeSet_Sec * 1000.0;
            long yourTimeSetMilliSec = (long)(YourTotalTimeSet_Sec * 1000.0);
            while (GetTotalProgressedTime_Ms() <= yourTimeSetMilliSec)
            {
                
                yield return new WaitUntil(CanRunTimeline);

                int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
                int pageIndex = Mathf.FloorToInt((float)((GetTotalProgressedTime_Ms() / 1000.0) / subTimelineTotalTime));
                float mainProgressVal = totalPageAmount > 1 ? (float)((pageIndex * 2.0 / (totalPageAmount - 1)) / 2) : 0;
                float subProgressActualVal = (GetTotalProgressedTime_Ms() - (long)(pageIndex * subTimelineTotalTime * 1000.0)) / (float)(subTimelineTotalTime * 1000.0);
                float subProgressVal = subProgressActualVal - (float)(GetMainActualTimeOffset() / subTimelineTotalTime);
                timeline_main.SetProgress_Actual(mainProgressVal, subProgressActualVal);

                long subTimelineStartVal = (long)(timeline_main.GetActualProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long subTimelineEndVal = (long)((timeline_main.GetActualProgress() * subTimelineTotalTime * (totalPageAmount - 1) + subTimelineTotalTime) * 1000.0);
                long mainTimelineStartVal = (long)(timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long mainTimelineActualStartVal = (long)(timeline_main.GetActualProgress() * subTimelineTotalTime * (totalPageAmount - 1) * 1000.0);
                long mainTimelineEndVal = (long)((timeline_main.GetProgress() * subTimelineTotalTime * (totalPageAmount - 1) + subTimelineTotalTime) * 1000.0);

                UpdateSubTimelineProgress();
                UpdateSubtimelineCursorVisual();

                OnTimelineValueChanged?.Invoke(GetTotalProgressedTime_Ms());
            }
        }

        long GetTotalProgressedTime_Ms()
        {
            long result = -1;

            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - GetPausedTime() - start_time);
            result = (long)(ts.TotalMilliseconds + GetDragOffset_Seconds() * 1000);

            return result >= 0 ? result : 0;
        }

        void ScaleUpTimeline()
        {
            if (visible_main_grid_amount > min_main_grid_amount) visible_main_grid_amount--;

            subTimelineTotalTime = visible_main_grid_amount;
        }
        void ScaleDownTimeline()
        {
            if (visible_main_grid_amount < max_visible_grid_amount) visible_main_grid_amount++;

            subTimelineTotalTime = visible_main_grid_amount;
        }
        void InitTimeline()
        {
            /** ------ */
            visible_main_grid_amount = max_visible_grid_amount;
            subTimelineTotalTime = visible_main_grid_amount;
   
            long startVal = 0;
            int pageIndex = 0;
            int totalPageAmount = Mathf.CeilToInt((float)(YourTotalTimeSet_Sec / subTimelineTotalTime));
            float mainProgressVal = totalPageAmount > 1 ? (((float)pageIndex / (totalPageAmount - 1))) : 0;
            timeline_main.UpdateScrollSize(1.0f / totalPageAmount);
            timeline_main.SetProgress(mainProgressVal);
            timeline_main.SetProgress_Actual(mainProgressVal, 0);

            timeline_sub.SetProgress(0);
            timeline_sub.RefreshGrid(visible_main_grid_amount, startVal);
            timeline_sub.UpdateHandleText(eTimelineType, subTimelineTotalTime, startVal);

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


