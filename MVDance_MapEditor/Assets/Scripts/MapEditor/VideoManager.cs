using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using RyanNielson.InputBinder;

namespace MVDance.MapEditor
{
    public class VideoManager : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] VideoPlayer videoPlayer_dev;
        [SerializeField] VideoPlayer videoPlayer_in_game;
        [SerializeField] Button btn_play_pause;
        [SerializeField] TimelineManager timelineManager;

        [SerializeField] Toggle mainTimelineViewResetToggle;

        InputBinder inputComponent;
        bool shouldPlay = false;

        private void Awake()
        {
            videoPlayer_dev.url = Application.streamingAssetsPath + "/TestVideo/dev_cut_noaudio.mp4";
            videoPlayer_in_game.url = Application.streamingAssetsPath + "/TestVideo/in_game_cut_noaudio.mp4";

            videoPlayer_dev.Prepare();
            videoPlayer_in_game.Prepare();

            //  get totalsec from video config
            double totalSec = 60;
            timelineManager.SetMainTimerlineTotalTime(totalSec);

            btn_play_pause?.onClick.AddListener(PlayOrPause);

            timelineManager.OnTimelinePlaying += OnStartTimeline;
            timelineManager.OnTimelinePaused += OnTimelinePaused;
            timelineManager.OnTimelineValueChanged += OnTimelineValueChanged;
            timelineManager.OnDragMainTimeline += CancelResetMainTimelineOffset;

            mainTimelineViewResetToggle?.onValueChanged.AddListener(b => { timelineManager.SetAutoResetMainTimelineOffset(b); });
        }

        void CancelResetMainTimelineOffset()
        {
            mainTimelineViewResetToggle.isOn = false;
        }

        void OnStartTimeline(long newTime_Ms)
        {
            videoPlayer_dev?.Play();
            videoPlayer_in_game?.Play();
        }

        void OnTimelinePaused()
        {
            videoPlayer_dev?.Pause();
            videoPlayer_in_game?.Pause();
        }

        void OnTimelineValueChanged(long newTime_Ms)
        {
            if (videoPlayer_dev.time * 1000 - newTime_Ms > 200)
            {
                videoPlayer_dev.time = newTime_Ms / 1000.0;
                print("force sync dev video to timeline");
            }
            if (videoPlayer_in_game.time * 1000 - newTime_Ms > 200)
            {
                videoPlayer_in_game.time = newTime_Ms / 1000.0;
                print("force in game dev video to timeline");
            }
        }

        void PlayOrPause()
        {
            shouldPlay = !shouldPlay;
            if (shouldPlay)
            {
                videoPlayer_dev?.Play();
                videoPlayer_in_game?.Play();
            }
            else
            {
                videoPlayer_dev?.Pause();
                videoPlayer_in_game?.Pause();
            }
        }

        private void OnDestroy()
        {
            timelineManager.OnTimelinePlaying -= OnStartTimeline;
            timelineManager.OnTimelinePaused -= OnTimelinePaused;
            timelineManager.OnTimelineValueChanged -= OnTimelineValueChanged;
            timelineManager.OnDragMainTimeline -= CancelResetMainTimelineOffset;

        }
    }
}


