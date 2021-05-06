using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using RyanNielson.InputBinder;

namespace MVDance.MapEditor
{
    public class Timeline : MonoBehaviour
    {
        [Header("-- Timeline Config --")]
        [SerializeField] TimelinePanel timeline_panel;
        [SerializeField] TimelineHandle timeline_handle;
        [SerializeField] UIGridRenderer bottom_timeline_grid_main;
        [SerializeField] UIGridRenderer bottom_timeline_grid_sub;
        [SerializeField] Scrollbar bottom_timeline_progress_bar;
        [SerializeField] Text time_text;
        [SerializeField] Text time_text_copy;
        [SerializeField] Transform timeline_grid_value_root;
        [SerializeField] Transform timeline_grid_value_text_tr;

        [Header("-- Test Var --")]
        [SerializeField] double mainGridTotalTime = 10;



        InputBinder inputComponent;

        bool isMouseHoverOnTimeline = false;
        bool canRunTimeline = true;
        Action Action_OnTimelineScaled;

        private void Awake()
        {
            timeline_handle.Action_OnDragging += d =>
            {
                //  do not remove.
                bottom_timeline_progress_bar.OnDrag(d);
            };
            timeline_handle.Action_OnPointerDown += d =>
            {
                //  hold is drag, hold is pause
                dragStartVal = bottom_timeline_progress_bar.value;
                UpdateTimelineData(false);
            };
            timeline_handle.Action_OnPointerUp += d =>
            {
                //  hold is drag, hold is pause
                dragEndVal = bottom_timeline_progress_bar.value;
                dragOffset_Sec += mainGridTotalTime * (dragEndVal - dragStartVal);
                UpdateTimelineData(true);
            };

            bottom_timeline_progress_bar.onValueChanged.AddListener(f =>
            {
                UpdateTimelineHandleText(0, (long)(f * mainGridTotalTime * 1000));
            });
            Action_OnTimelineScaled += () =>
            {
                UpdateTimelineHandleText(0, (long)(bottom_timeline_progress_bar.value * mainGridTotalTime * 1000));
                RefreshGridValue();
            };
            RefreshGridValue();
            UpdateTimelineHandleText(0, (long)(bottom_timeline_progress_bar.value * mainGridTotalTime * 1000));

            timeline_panel.Action_OnPointerEnter += () => isMouseHoverOnTimeline = true;
            timeline_panel.Action_OnPointerExit += () => isMouseHoverOnTimeline = false;

            inputComponent = GetComponent<InputBinder>();
            inputComponent.BindAxis("Mouse ScrollWheel", TryScaleTimeline);
            inputComponent.BindKey(KeyCode.R, InputEvent.Pressed, TryRunTimelineFromStart);
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
            }

            totalPausedTime += pauseEndTime - pauseStartTime > 0 ? pauseEndTime - pauseStartTime : 0;
        }
        double GetDragOffset_Seconds() => dragOffset_Sec;
        long GetPausedTime() => totalPausedTime;
        bool CanRunTimeline() => canRunTimeline;
        bool CanScale() => isMouseHoverOnTimeline;
        private void TryScaleTimeline(float mouseWheelVal)
        {
            if (CanScale() && (mouseWheelVal > 0.01f || mouseWheelVal < -0.01f))
            {
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

        Coroutine task_updateTime = null;

        private void ResetTimeline()
        {
            bottom_timeline_progress_bar.value = 0;
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
                bottom_timeline_progress_bar.value = (float)progress;
                yield return new WaitUntil(CanRunTimeline);
            }
            bottom_timeline_progress_bar.value = Mathf.Clamp((float)progress, 0, 1);
        }

        void UpdateTimelineHandleText(int displayType, long inTime)
        {
            string displayStr = "NULL";
            if (displayType == 0)
            {
                displayStr = string.Format("{0:00.00}", (inTime / 1000.0f).ToString("f2"));
            }
            time_text.text = displayStr;
            time_text_copy.text = displayStr;
        }

        void ScaleUpTimeline()
        {
            bottom_timeline_grid_main.gridSize = new Vector2Int(Mathf.Clamp(bottom_timeline_grid_main.gridSize.x - 1, 1, 20), 1);
            bottom_timeline_grid_main.gameObject.SetActive(false);
            bottom_timeline_grid_main.gameObject.SetActive(true);

            bottom_timeline_grid_sub.gridSize = new Vector2Int(Mathf.Clamp(bottom_timeline_grid_sub.gridSize.x - 10, 10, 200), 1);
            bottom_timeline_grid_sub.gameObject.SetActive(false);
            bottom_timeline_grid_sub.gameObject.SetActive(true);

            mainGridTotalTime = bottom_timeline_grid_main.gridSize.x;
        }
        void ScaleDownTimeline()
        {
            bottom_timeline_grid_main.gridSize = new Vector2Int(Mathf.Clamp(bottom_timeline_grid_main.gridSize.x + 1, 1, 20), 1);
            bottom_timeline_grid_main.gameObject.SetActive(false);
            bottom_timeline_grid_main.gameObject.SetActive(true);

            bottom_timeline_grid_sub.gridSize = new Vector2Int(Mathf.Clamp(bottom_timeline_grid_sub.gridSize.x + 10, 10, 200), 1);
            bottom_timeline_grid_sub.gameObject.SetActive(false);
            bottom_timeline_grid_sub.gameObject.SetActive(true);

            mainGridTotalTime = bottom_timeline_grid_main.gridSize.x;
        }
        void InitTimelineGrid(int mainGridNum)
        {
            bottom_timeline_grid_main.gridSize = new Vector2Int(mainGridNum, 1);
            bottom_timeline_grid_main.gameObject.SetActive(false);
            bottom_timeline_grid_main.gameObject.SetActive(true);

            bottom_timeline_grid_sub.gridSize = new Vector2Int(mainGridNum * 10, 1);
            bottom_timeline_grid_sub.gameObject.SetActive(false);
            bottom_timeline_grid_sub.gameObject.SetActive(true);
        }
        void RefreshGridValue()
        {
            RectTransform grid_rt = timeline_grid_value_root.GetComponent<RectTransform>();
            for (int i = 0; i < timeline_grid_value_root.childCount; i++)
            {
                Destroy(timeline_grid_value_root.GetChild(i).gameObject);
            }

            for (int i = 0; i < (int)mainGridTotalTime + 1; i++)
            {
                Transform t = Instantiate(timeline_grid_value_text_tr);
                t.GetComponent<Text>().text = i.ToString();
                t.SetParent(timeline_grid_value_root);
                t.localScale = Vector3.one;
                float posX = mainGridTotalTime > 0 ? i * grid_rt.rect.size.x / (float)mainGridTotalTime - grid_rt.rect.size.x / 2.0f : 0;
                // print($"POSX: {posX}");
                t.localPosition = new Vector2(posX, -10);
            }
        }
    }
}


