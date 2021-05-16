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
        [SerializeField] double YourTotalTimeSet = 35.5;

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
                dragOffset_Sec += timelineTotalTime * timeline_sub.GetDragVal();
                if (!isPaused) UpdateCanRun(true);
                print(dragOffset_Sec);
            };

            timeline_sub.Action_OnScrollValueChanged += f =>
            {
                int index = Mathf.FloorToInt((float)(totalProgressedTimeStamp / timelineTotalTime));
                long startVal = index * visible_main_grid_amount;
                timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, startVal);
            };
            timeline_main.Action_OnScrollValueChanged += f =>
            {
                int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet / timelineTotalTime));
                long startVal = Mathf.FloorToInt((float)(timeline_main.GetProgress() * timelineTotalTime * (totalCount - 1)));
                
                UpdateMainTimelineBarText(totalCount);
                UpdateSubtimelineOffset(startVal);
            };

            Action_OnTimelineScaled += () =>
            {
                int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet / timelineTotalTime));
                long startVal = Mathf.FloorToInt((float)(timeline_main.GetProgress() * timelineTotalTime * (totalCount - 1)));

                timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, startVal);
                timeline_sub.RefreshGrid(visible_main_grid_amount, startVal);
                
                RefreshTotalTimeLine(totalCount);            
                UpdateMainTimelineBarText(totalCount);
                UpdateSubtimelineOffset(startVal);
                
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
        double GetDragOffset_Seconds() => dragOffset_Sec;
        long GetPausedTime() => timelinePausedTime;
        bool CanRunTimeline() => canRunTimeline && !isPaused;
        bool CanScale() => isMouseHoverOnTimeline;
        bool IsTimelineRunning() => pauseStartTime < pauseEndTime || !bFirstTimePlaying;
        long GetNowHandleTime() => (long)((timeline_sub.GetProgress() * timelineTotalTime + totalProgressedTimeStamp) * 1000);
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
            int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet / timelineTotalTime));

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

        long grid_start_time = 0;
        long pauseStartTime = 0;
        long pauseEndTime = 0;
        long timelinePausedTime = 0;
        double dragOffset_Sec = 0;
        bool bFirstTimePlaying = true;

        double timelineProgressTime = 0;
        double timelineTotalTime = 0;
        double totalProgressedTimeStamp = 0;


        Coroutine task_updateTime = null;

        void ResetTimeline()
        {
            ResetMainTimeline();
            ResetSubTimeline();
        }
        void ResetMainTimeline()
        {
            totalProgressedTimeStamp = 0;
        }
        void ResetSubTimeline()
        {
            timelinePausedTime = 0;
            dragOffset_Sec = 0;
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
            grid_start_time = DateTime.Now.Ticks;

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
            double progress = 0;
            while (progress <= 1)
            {
                if (visible_main_grid_amount <= 0)
                {
                    Debug.LogError("invalid visible grid");
                    yield break;
                }
                TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - GetPausedTime() - grid_start_time);
                progress = (ts.TotalSeconds + GetDragOffset_Seconds()) / visible_main_grid_amount;

                timelineProgressTime = visible_main_grid_amount * progress;

                timeline_sub.UpdateProgress((float)progress);

                if (timelineProgressTime + totalProgressedTimeStamp > YourTotalTimeSet)
                {
                    yield break;
                }
                yield return new WaitUntil(CanRunTimeline);
            }
            timeline_sub.UpdateProgress(Mathf.Clamp((float)progress, 0, 1));

            /** after end of progress */

            totalProgressedTimeStamp += timelineProgressTime;
            // print($"totalProgressTime:{totalProgressTime}");
            if (totalProgressedTimeStamp < YourTotalTimeSet)
            {
                int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet / timelineTotalTime));
                int index = Mathf.FloorToInt((float)(totalProgressedTimeStamp / timelineTotalTime));
                float mainProgressVal = totalCount > 1 ? (float)((index * 2.0 / (totalCount - 1)) / 2) : 0;
                timeline_main.UpdateProgress(mainProgressVal);

                long startVal = index * visible_main_grid_amount;
                timeline_sub.RefreshGrid(visible_main_grid_amount, startVal);

                string main_timeline_head_str = string.Format("{0:00.00}", totalProgressedTimeStamp.ToString("f2"));
                string main_timeline_tail_str = string.Format("{0:00.00}", (totalProgressedTimeStamp + timelineTotalTime).ToString("f2"));
                timeline_main.UpdateText(main_timeline_head_str, main_timeline_tail_str);

                timeline_sub.UpdateProgress(0);
                TryRunSubTimelineFromStart();
            }
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
            timeline_sub.UpdateProgress(0);

            int totalCount = Mathf.CeilToInt((float)(YourTotalTimeSet / timelineTotalTime));
            long startVal = Mathf.FloorToInt((float)(timeline_main.GetProgress() * timelineTotalTime * (totalCount - 1)));

            int index = Mathf.FloorToInt((float)(totalProgressedTimeStamp / timelineTotalTime));
            float mainProgressVal = totalCount > 1 ? (((float)index / (totalCount - 1))) : 0;
            timeline_main.UpdateScrollSize(1.0f / totalCount);
            timeline_main.UpdateProgress(mainProgressVal);

            timeline_sub.RefreshGrid(visible_main_grid_amount, startVal);
            timeline_sub.UpdateHandleText(eTimelineType, timelineTotalTime, startVal);

            RefreshTotalTimeLine(totalCount);
            UpdateMainTimelineBarText(totalCount);
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


