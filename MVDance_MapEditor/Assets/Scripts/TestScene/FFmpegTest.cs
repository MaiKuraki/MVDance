using UnityEngine;
using UnityEngine.UI;
using FFmpeg.NET;
using FFmpeg.NET.Events;
using MVDance.MapEditor;

public class FFmpegTest : MonoBehaviour
{
    [SerializeField] Button btn_run_ffmpeg_command;
    [SerializeField] Slider ffmpeg_task_progress_bar;
    [SerializeField] StateFlag flag_ffmpeg_state;
    [SerializeField] InputField inputField;
    [Header("-- FFmpeg Config --")]
    [SerializeField] string inputFilePath;

    string ffmpegPath = Application.streamingAssetsPath + "\\ffmpegLib\\ffmpeg.exe".Replace("\\", "/");
    bool isRunningFFmpegTask = false;
    float ffmpeg_progress = 0;

    private void Awake()
    {
        btn_run_ffmpeg_command.onClick.AddListener(TryRunFFmpegCommand);
        ffmpeg_task_progress_bar.value = 0;
        flag_ffmpeg_state.UpdateState(new SignalStateSetting() { flag_color = Color.gray, state_text = "Idle" });
        inputField.onValueChanged.AddListener(s => { inputFilePath = s; });
    }
    void TryRunFFmpegCommand()
    {
        if (!isRunningFFmpegTask)
        {
            isRunningFFmpegTask = true;
            btn_run_ffmpeg_command.interactable = !isRunningFFmpegTask;
            flag_ffmpeg_state.UpdateState(new SignalStateSetting() { flag_color = Color.green, state_text = "Running" });


            StartConverting();
        }
    }


    #region FFmpeg
    async void StartConverting()
    {
        //var inputFile = new MediaFile(inputFilePath);
        //var outputFile = new MediaFile(@"D:\Desktop\output.mp4");

        if (!System.IO.File.Exists(inputFilePath))
        {
            Debug.LogError("Invalid input video path");
            isRunningFFmpegTask = false;
            return;
        }

        var ffmpeg = new Engine(ffmpegPath);
        ffmpeg.Progress += OnProgress;
        ffmpeg.Data += OnData;
        ffmpeg.Error += OnError;
        ffmpeg.Complete += OnComplete;
        string arguments = "-hwaccel auto -i " + inputFilePath + " -b:v 8M " + @"D:\output.mp4";
        Debug.Log($"arguments: {arguments}");
        await ffmpeg.ExecuteAsync(arguments);
        //await ffmpeg.ConvertAsync(inputFile, outputFile);
    }

    private void OnProgress(object sender, ConversionProgressEventArgs e)
    {
        //Console.WriteLine("[{0} => {1}]", e.Input.FileInfo.Name, e.Output.FileInfo.Name);
        //Console.WriteLine("Bitrate: {0}", e.Bitrate);
        //Console.WriteLine("Fps: {0}", e.Fps);
        //Console.WriteLine("Frame: {0}", e.Frame);
        //Console.WriteLine("ProcessedDuration: {0}", e.ProcessedDuration);
        //Console.WriteLine("Size: {0} kb", e.SizeKb);
        //Console.WriteLine("TotalDuration: {0}\n", e.TotalDuration);
        float totalSeconds = e.TotalDuration.TotalSeconds > 0 ? (float)e.TotalDuration.TotalSeconds : 1;
        ffmpeg_progress = (float)((float)e.ProcessedDuration.TotalSeconds / totalSeconds);
        ffmpeg_progress = Mathf.Clamp(ffmpeg_progress, 0, 1);
        ffmpeg_task_progress_bar.value = ffmpeg_progress;

        //Debug.Log($"TotalDuration: {e.TotalDuration.TotalSeconds}   ProcessedDuration:{e.ProcessedDuration.TotalSeconds}");
        Debug.Log(e.ToString());
    }

    private void OnData(object sender, ConversionDataEventArgs e)
    {
        //Console.WriteLine("[{0} => {1}]: {2}", e.Input.FileInfo.Name, e.Output.FileInfo.Name, e.Data);
        //Debug.Log($"Progress: {progress}");
        //ffmpeg_task_progress_bar.value = progress;
    }

    private void OnComplete(object sender, ConversionCompleteEventArgs e)
    {
        isRunningFFmpegTask = false;
        btn_run_ffmpeg_command.interactable = !isRunningFFmpegTask;
        flag_ffmpeg_state.UpdateState(new SignalStateSetting() { flag_color = Color.gray, state_text = "Idle" });


        Debug.Log($"Task Completed: {e.Output.FileInfo.FullName}");
        //Console.WriteLine("Completed conversion from {0} to {1}", e.Input.FileInfo.FullName, e.Output.FileInfo.FullName);
    }

    private void OnError(object sender, ConversionErrorEventArgs e)
    {
        isRunningFFmpegTask = false;
        btn_run_ffmpeg_command.interactable = !isRunningFFmpegTask;
        flag_ffmpeg_state.UpdateState(new SignalStateSetting() { flag_color = Color.gray, state_text = "Idle" });

        Debug.Log($"Task Error: {e.Exception?.Message}");

        //Console.WriteLine("[{0} => {1}]: Error: {2}\n{3}", e.Input.FileInfo.Name, e.Output.FileInfo.Name, e.Exception.ExitCode, e.Exception.InnerException);
    }
    #endregion
}
