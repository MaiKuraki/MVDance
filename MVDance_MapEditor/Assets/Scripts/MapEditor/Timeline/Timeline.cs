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
        [SerializeField] TimelineHandle timeline_handle;
        [SerializeField] UIGridRenderer grid_main;
        [SerializeField] UIGridRenderer grid_sub;
        [SerializeField] Transform timeline_bar;
        [SerializeField] Text time_text;
        [SerializeField] Text time_text_copy;
        [SerializeField] Transform grid;
        [SerializeField] Transform grid_value_root;
        [SerializeField] Transform grid_value_text_tr;

        [Header("-- Test Var --")]
        [SerializeField] double mainGridTotalTime = 10;


        Scrollbar timeline_bar_scroll;
        InputBinder inputComponent;
        RectTransform timeline_bar_rt;
        RectTransform grid_rt;


        bool isMouseHoverOnTimeline = false;
        bool canRunTimeline = true;
        Action Action_OnTimelineScaled;
        Action Action_OnTimelinePreScale;
        TimelineType eTimelineType = TimelineType.Second;

        private void Awake()
        {
            timeline_bar_scroll = timeline_bar.GetComponent<Scrollbar>();
            timeline_bar_rt = timeline_bar.GetComponent<RectTransform>();
            grid_rt = grid.GetComponent<RectTransform>();

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
                dragOffset_Sec += mainGridTotalTime * (dragEndVal - dragStartVal);
                UpdateTimelineData(true);
            };

            timeline_bar_scroll.onValueChanged.AddListener(f =>
            {
                UpdateTimelineHandleText((long)(f * mainGridTotalTime * 1000));
            });
            Action_OnTimelineScaled += () =>
            {
                UpdateTimelineHandleText((long)(timeline_bar_scroll.value * mainGridTotalTime * 1000));
                RefreshGridValue();
                TryLockGridToCursor();
            };
            Action_OnTimelinePreScale += () =>
            {
                TryLockGridToCursor();
            };
            RefreshGridValue();
            UpdateTimelineHandleText((long)(timeline_bar_scroll.value * mainGridTotalTime * 1000));

            timeline_panel.Action_OnPointerEnter += () => isMouseHoverOnTimeline = true;
            timeline_panel.Action_OnPointerExit += () => isMouseHoverOnTimeline = false;

            inputComponent = GetComponent<InputBinder>();
            inputComponent.BindAxis("Mouse ScrollWheel", TryScaleTimeline);
            inputComponent.BindKey(KeyCode.R, InputEvent.Pressed, ()=> { TryRunTimelineFromStart(); runningBeforePause = true; });
            inputComponent.BindKey(KeyCode.Space, InputEvent.Pressed, () => { UpdateTimelineData(!canRunTimeline); });

            InitTimelineGrid((int)mainGridTotalTime);
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
                runningBeforePause = false;
            }

            totalPausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }
        double GetDragOffset_Seconds() => dragOffset_Sec;
        long GetPausedTime() => totalPausedTime;
        bool CanRunTimeline() => canRunTimeline;
        bool CanScale() => isMouseHoverOnTimeline;
        bool IsTimelineRunning() => pauseStartTime < pauseEndTime || runningBeforePause;
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
        long startTime = 0;
        long pauseStartTime = 0;
        long pauseEndTime = 0;
        long totalPausedTime = 0;
        float dragStartVal = 0;
        float dragEndVal = 0;
        double dragOffset_Sec = 0;
        bool runningBeforePause = false;
        long timelineStartVal = 0;
        long timelineEndval = 0;

        Coroutine task_updateTime = null;

        private void ResetTimeline()
        {
            timeline_bar_scroll.value = 0;
            totalPausedTime = 0;
            dragOffset_Sec = 0;
            canRunTimeline = true;
            if (task_updateTime != null) StopCoroutine(task_updateTime);
        }
        void TryRunTimelineFromStart()
        {
            ResetTimeline();
            if (mainGridTotalTime <= 0)
            {
                Debug.LogError("invalid total time");
                return;
            }
            startTime = DateTime.Now.Ticks;
            if (task_updateTime != null) StopCoroutine(task_updateTime);
            task_updateTime = StartCoroutine(Task_UpdateTime());
        }
        IEnumerator Task_UpdateTime()
        {
            double progress = 0;
            while (progress <= 1)
            {
                if (mainGridTotalTime <= 0)
                {
                    Debug.LogError("invalid total time");
                    yield break;
                }
                TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - GetPausedTime() - startTime);
                // UpdateTimelineHandleText(0,(long)((ts.TotalSeconds + GetDragOffset_Seconds()) * 1000));
                progress = (ts.TotalSeconds + GetDragOffset_Seconds()) / mainGridTotalTime;
                timeline_bar_scroll.value = (float)progress;
                yield return new WaitUntil(CanRunTimeline);
            }
            timeline_bar_scroll.value = Mathf.Clamp((float)progress, 0, 1);
        }

        void UpdateTimelineHandleText(long inTime)
        {
            string displayStr = "NULL";
            if (eTimelineType == TimelineType.Second)
            {
                displayStr = string.Format("{0:00.00}", (inTime / 1000.0f).ToString("f2"));
            }
            time_text.text = displayStr;
            time_text_copy.text = displayStr;
        }

        float gridScaleRatio = 1;
        void ScaleUpTimeline()
        {
            //float startScaleSizeX = Mathf.Clamp(grid_main.gridSize.x, 1, 20);
            //float endScaleSizeX = Mathf.Clamp(grid_main.gridSize.x - 1, 1, 20);

            //gridScaleRatio = (startScaleSizeX / endScaleSizeX);
            //print($"ratio{gridScaleRatio}");
            //float sourceGridWidth = grid_rt.rect.width;
            //float sourceGridHeight = grid_rt.rect.height;
            //float targetWidth = sourceGridWidth * gridScaleRatio;
            //print($"targetWidth: {targetWidth}");
            //grid_rt.sizeDelta = new Vector2(targetWidth, sourceGridHeight);

            grid_main.gridSize = new Vector2Int(Mathf.Clamp(grid_main.gridSize.x - 1, 1, 20), 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);

            grid_sub.gridSize = new Vector2Int(Mathf.Clamp(grid_sub.gridSize.x - 10, 10, 200), 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);

            mainGridTotalTime = grid_main.gridSize.x;
        }
        void ScaleDownTimeline()
        {
            grid_main.gridSize = new Vector2Int(Mathf.Clamp(grid_main.gridSize.x + 1, 1, 20), 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);

            grid_sub.gridSize = new Vector2Int(Mathf.Clamp(grid_sub.gridSize.x + 10, 10, 200), 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);

            mainGridTotalTime = grid_main.gridSize.x;
        }
        void InitTimelineGrid(int mainGridNum)
        {
            grid_main.gridSize = new Vector2Int(mainGridNum, 1);
            grid_main.gameObject.SetActive(false);
            grid_main.gameObject.SetActive(true);

            grid_sub.gridSize = new Vector2Int(mainGridNum * 10, 1);
            grid_sub.gameObject.SetActive(false);
            grid_sub.gameObject.SetActive(true);
        }
        void RefreshGridValue()
        {
            for (int i = 0; i < grid_value_root.childCount; i++)
            {
                Destroy(grid_value_root.GetChild(i).gameObject);
            }

            for (int i = 0; i < (int)mainGridTotalTime + 1; i++)
            {
                Transform t = Instantiate(grid_value_text_tr);
                t.GetComponent<Text>().text = i.ToString();
                t.SetParent(grid_value_root);
                t.localScale = Vector3.one;
                float posX = mainGridTotalTime > 0 ? i * grid_rt.rect.size.x / (float)mainGridTotalTime - grid_rt.rect.size.x / 2.0f : 0;
                // print($"POSX: {posX}");
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

            print($"timelineBar: {timeline_bar_rt.rect.width}, Grid: {grid_rt.rect.width}");
        }

        long GetTimelineValue()
        {
            long result = 0;
            if (eTimelineType == TimelineType.Second)
            {
                long timelineTotalval = timelineEndval - timelineStartVal;
                result = timelineStartVal + (long)(timeline_bar_scroll.value * timelineTotalval);
            }
            return result;
        }
    }
}


