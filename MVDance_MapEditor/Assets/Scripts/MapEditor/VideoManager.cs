using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace MVDance.MapEditor
{
    public class VideoManager : MonoBehaviour
    {
        [Header("--- Config ---")]
        [SerializeField] VideoPlayer videoPlayer_dev;
        [SerializeField] VideoPlayer videoPlayer_in_game;
        [SerializeField] Button btn_play_pause;

        bool shouldPlay = false;

        private void Awake()
        {
            videoPlayer_dev.url = Application.streamingAssetsPath + "/TestVideo/dev_cut_noaudio.mp4";
            videoPlayer_in_game.url = Application.streamingAssetsPath + "/TestVideo/in_game_cut_noaudio.mp4";

            btn_play_pause?.onClick.AddListener(PlayOrPause);
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
    }
}


