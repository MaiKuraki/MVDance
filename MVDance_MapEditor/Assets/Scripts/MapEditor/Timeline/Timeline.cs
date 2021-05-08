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

    public class Timeline : MonoBehaviour
    {
        [Header("-- Timeline Config --")]
        [SerializeField] TimelinePanel timeline_panel;
        [SerializeField] TimelineInteractable timeline_handle;
        [SerializeField] UIGridRenderer grid_main;
        [SerializeField] UIGridRenderer grid_sub;
        [SerializeField] Transform timeline_bar;
        [SerializeField] Transform total_time_bar;
        [SerializeField] Transform total_time_bar_handle;
        [SerializeField] Text time_text;
        [SerializeField] Text time_text_copy;
        [SerializeField] Transform grid;
        [SerializeField] Transform grid_value_root;
        [SerializeField] Transform grid_value_text_tr;
        [SerializeField] Text total_time_start_text;
        [SerializeField] Text total_time_end_text;

        [Header("-- Test Val --")]
        [SerializeField] double YourTotalTimeSet = 15.5;
        

        Scrollbar timeline_bar_scroll;
        Scrollbar total_time_bar_scroll;
        InputBinder inputComponent;
        RectTransform timeline_bar_rt;
        RectTransform grid_rt;


        bool isMouseHoverOnTimeline = false;
        bool canRunTimeline = true;
        Action Action_OnTimelineScaled;
        Action Action_OnTimelinePreScale;
        TimelineType eTimelineType = TimelineType.Second;

        int max_main_grid_amount = 20;
        int min_main_grid_amount = 1;
        int visible_main_grid_amount;

        private void Awake()
        {
            timeline_bar_scroll = timeline_bar.GetComponent<Scrollbar>();
            total_time_bar_scroll = total_time_bar.GetComponent<Scrollbar>();
            timeline_bar_rt = timeline_bar.GetComponent<RectTransform>();
            grid_rt = grid.GetComponent<RectTransform>();

            timeline_panel.Action_OnPointerEnter += () => isMouseHoverOnTimeline = true;
            timeline_panel.Action_OnPointerExit += () => isMouseHoverOnTimeline = false;

            timeline_handle.Action_OnDragging += d =>
            {
                //  do not remove.
                timeline_bar_scroll.OnDrag(d);
            };
            timeline_handle.Action_OnPointerDown += d =>
            {
                //  hold is drag, hold is pause
                dragStartVal = timeline_bar_scroll.value;
                UpdateTimelineData(false);
            };
            timeline_handle.Action_OnPointerUp += d =>
            {
                //  hold is drag, hold is pause
                dragEndVal = timeline_bar_scroll.value;
                dragOffset_Sec += timelineTotalTime * (dragEndVal - dragStartVal);
                UpdateTimelineData(true);
            };

            timeline_bar_scroll.onValueChanged.AddListener(f =>
            {
                UpdateTimelineHandleText();
            });
            total_time_bar_scroll.onValueChanged.AddListener(f => {
                print($"value: {total_time_bar_scroll.value}");
            });
            Action_OnTimelineScaled += () =>
            {
                UpdateTimelineHandleText();
                RefreshGrid();
                TryLockGridToCursor();
            };
            Action_OnTimelinePreScale += () =>
            {
                TryLockGridToCursor();
            };

            inputComponent = GetComponent<InputBinder>();
            inputComponent.BindAxis("Mouse ScrollWheel", TryScaleTimeline);
            inputComponent.BindKey(KeyCode.R, InputEvent.Pressed, ()=> { ResetTotalTime(); TryRunTimelineFromStart(); runningBeforePause = true; });
            inputComponent.BindKey(KeyCode.Space, InputEvent.Pressed, () => { UpdateTimelineData(!canRunTimeline); });

            InitTimeline();
        }

        void UpdateTimelineData(bool newCanRunTimeline)
        {
            canRunTimeline = newCanRunTimeline;
            if (canRunTimeline)
            {
                pauseEndTime = DateTime.Now.Ticks;
            }
            else
            {
                pauseStartTime = DateTime.Now.Ticks;
                // runningBeforePause = false;
            }

            timelinePausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }
        double GetDragOffset_Seconds() => dragOffset_Sec;
        long GetPausedTime() => timelinePausedTime;
        bool CanRunTimeline() => canRunTimeline;
        bool CanScale() => isMouseHoverOnTimeline;
        bool IsTimelineRunning() => pauseStartTime < pauseEndTime || runningBeforePause;
        long GetNowHandleTime() => (long)((timeline_bar_scroll.value * timelineTotalTime + totalProgressedTimeStamp) * 1000);
        private void TryScaleTimeline(float mouseWheelVal)
        {
            if (CanScale() && (mouseWheelVal > 0.01f || mouseWheelVal < -0.01f))
            {
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
        long grid_start_time = 0;
        long pauseStartTime = 0;
        long pauseEndTime = 0;
        long timelinePausedTime = 0;
        float dragStartVal = 0;
        float dragEndVal = 0;
        double dragOffset_Sec = 0;
        bool runningBeforePause = false;
        long timelineStartVal = 0;
        long timelineEndval = 0;
        double gridOriginWidth;

        double timelineProgressTime = 0;
        double timelineTotalTime = 0;
        double totalProgressedTimeStamp = 0;



        Coroutine task_updateTime = null;

        void ResetTotalTime()
        {
            totalProgressedTimeStamp = 0;
        }
        void ResetTimeline()
        {
            timelinePausedTime = 0;
            dragOffset_Sec = 0;
            canRunTimeline = true;
            if (task_updateTime != null) StopCoroutine(task_updateTime);
        }
        void TryRunTimelineFromStart()
        {
            ResetTimeline();
            grid_start_time = DateTime.Now.Ticks;
            if (task_updateTime != null) StopCoroutine(task_updateTime);
            task_updateTime = StartCoroutine(Task_UpdateTime());
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

                timeline_bar_scroll.value = (float)progress;

                if (timelineProgressTime + totalProgressedTimeStamp > YourTotalTimeSet)
                {
                    yield break;
                }
                yield return new WaitUntil(CanRunTimeline);
            }
            timeline_bar_scroll.value = Mathf.Clamp((float)progress, 0, 1);

            /** after end of progress */

            totalProgressedTimeStamp += timelineProgressTime;
            // print($"totalProgressTime:{totalProgressTime}");
            if (totalProgressedTimeStamp < YourTotalTimeSet)
            {
                for (int i = 0; i < grid_value_root.childCount; i++)
                {
                    Text t = grid_value_root.GetChild(i).GetComponent<Text>();
                    long timeText = long.Parse(t.text);
                    t.text = (timeText + visible_main_grid_amount).ToString();
                }
                //float next_pos_x = grid_rt.localPosition.x - (float)gridOriginWidth;
                //float pos_y = grid_rt.localPosition.y;
                //grid_rt.localPosition = new Vector2(next_pos_x, pos_y);
                int totalCount = (int)(YourTotalTimeSet * 1000) / (int)(timelineTotalTime * 1000);
                int index = (int)(totalProgressedTimeStamp * 1000) / (int)(timelineTotalTime * 1000);
                //double midVal = totalProgressedTimeStamp + timelineTotalTime / 2;
                //double maxGridTime = (totalCount - 1) * timelineProgressTime;
                //total_time_bar_scroll.value = (float)(midVal / maxGridTime);

                print($"totalCount: {totalCount}, index: {index}, midVal:{index}");
                total_time_bar_scroll.value = (float)((index * 2.0 / (totalCount - 1)) / 2);

                total_time_start_text.text = string.Format("{0:00.00}", totalProgressedTimeStamp.ToString("f2"));
                total_time_end_text.text = string.Format("{0:00.00}", (totalProgressedTimeStamp + timelineTotalTime).ToString("f2"));


                timeline_bar_scroll.value = 0;
                TryRunTimelineFromStart();
            }

            //StartCoroutine(Task_UpdateTime());
        }

        void UpdateTimelineHandleText()
        {
            string displayStr = "NULL";
            if (eTimelineType == TimelineType.Second)
            {
                displayStr = string.Format("{0:00.00}", (GetNowHandleTime() / 1000.0f).ToString("f2"));
            }
            time_text.text = displayStr;
            time_text_copy.text = displayStr;
        }



        void ScaleUpTimeline()
        {
            //double startScaleSizeX = Mathf.Clamp(grid_main.gridSize.x, 1, 20);
            //double endScaleSizeX = Mathf.Clamp(grid_main.gridSize.x - 1, 1, 20);

            //gridScaleRatio = startScaleSizeX / endScaleSizeX;
            //double targetWidth = gridOriginWidth * gridScaleRatio;


            //grid_rt.sizeDelta = new Vector2((float)targetWidth, grid_height);

            //double start_amount = max_main_grid_amount;

            if (visible_main_grid_amount > min_main_grid_amount) visible_main_grid_amount--;
   
            timelineTotalTime = visible_main_grid_amount;
        }
        void ScaleDownTimeline()
        {
            if (visible_main_grid_amount < max_main_grid_amount) visible_main_grid_amount++;

            timelineTotalTime = visible_main_grid_amount;
        }
        void InitTimeline()
        {
            /** ------ */
            visible_main_grid_amount = max_main_grid_amount;
            timelineTotalTime = visible_main_grid_amount;
            gridOriginWidth = grid_rt.rect.width;
            timeline_bar_scroll.value = 0;

            grid_main.gridSize = new Vector2Int(max_main_grid_amount, 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);

            grid_sub.gridSize = new Vector2Int(max_main_grid_amount * 10, 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);

            int totalCount = (int)(YourTotalTimeSet * 1000) / (int)(timelineTotalTime * 1000);
            total_time_bar_scroll.size = 1.0f / totalCount;

            total_time_start_text.text = string.Format("{0:00.00}", totalProgressedTimeStamp.ToString("f2"));
            total_time_end_text.text = string.Format("{0:00.00}", (totalProgressedTimeStamp + timelineTotalTime).ToString("f2"));

            //RectTransform total_time_bar_rt = total_time_bar.GetComponent<RectTransform>();
            //RectTransform total_time_bar_handle_rt = total_time_bar_handle.GetComponent<RectTransform>();
            //int totalCount = (int)(YourTotalTimeSet * 1000) / (int)(timelineTotalTime * 1000);
            //float total_time_handle_width = (float)gridOriginWidth / totalCount;
            //float total_time_handle_height = total_time_bar.GetComponent<RectTransform>().rect.height;
            //total_time_bar_rt.sizeDelta = new Vector2(total_time_bar_rt.rect.width * (totalCount - 1) / (float)totalCount, total_time_bar_rt.rect.height);
            //total_time_bar_handle_rt.sizeDelta = new Vector2(total_time_handle_width, total_time_handle_height);

            RefreshGrid();
            UpdateTimelineHandleText();
        }

        void RefreshGrid()
        {
            /** -- refresh render -- */
            double ratio = max_main_grid_amount / (double)visible_main_grid_amount;
            double full_timeline_width = gridOriginWidth * ratio;
            // print($"ratio{ratio}, targetWidth: {final_grid_width}");
            grid_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_rt.rect.height);

            /** -- refresh number value -- */
            RectTransform grid_value_root_rt = grid_value_root.GetComponent<RectTransform>();
            grid_value_root_rt.sizeDelta = new Vector2((float)full_timeline_width, grid_value_root_rt.rect.height);
            for (int i = 0; i < grid_value_root.childCount; i++)
            {
                Destroy(grid_value_root.GetChild(i).gameObject);
                print("Destroy");
            }

            for (int i = 0; i < max_main_grid_amount + 1; i++)
            {
                Transform t = Instantiate(grid_value_text_tr);
                t.GetComponent<Text>().text = i.ToString();
                t.SetParent(grid_value_root);
                t.localScale = Vector3.one;
                float posX = i * grid_value_root_rt.rect.width / max_main_grid_amount;
                t.localPosition = new Vector2(posX, -10);
            }
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


