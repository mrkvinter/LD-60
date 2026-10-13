using System.IO;
using UnityEngine;

namespace Code.Dev.Capture
{
    /// <summary>
    /// Пишет игровое окно в PNG-последовательность и печатает готовую команду ffmpeg для сборки mp4.
    /// </summary>
    public class FrameRecorder : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private bool recordOnStart;

        [Tooltip("Ноль — писать до ручной остановки")]
        [SerializeField, Min(0)] private int frameLimit;

        [Header("Формат")]
        [SerializeField, Min(1)] private int frameRate = 30;

        [Tooltip("Множитель разрешения игрового окна")]
        [SerializeField, Range(1, 4)] private int superSize = 1;

        [Tooltip("Пусто — <проект>/Recordings")]
        [SerializeField] private string outputDirectory;

        private string takeDirectory;
        private int frame;
        private int restoreCaptureFramerate;

        public bool IsRecording => takeDirectory != null;

        private void Start()
        {
            if (recordOnStart) StartRecording();
        }

        private void OnDisable() => StopRecording();

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                if (IsRecording) StopRecording();
                else StartRecording();
            }

            if (!IsRecording) return;

            ScreenCapture.CaptureScreenshot(Path.Combine(takeDirectory, $"frame_{frame:0000}.png"), superSize);
            frame++;

            if (frameLimit > 0 && frame >= frameLimit) StopRecording();
        }

        [ContextMenu("Start Recording")]
        public void StartRecording()
        {
            if (IsRecording) return;

            takeDirectory = NextTakeDirectory();
            Directory.CreateDirectory(takeDirectory);
            frame = 0;

            // Фиксированный шаг времени: движок ждёт каждый кадр, пока тот не запишется,
            // так что ролик идёт ровно, как бы ни проседал рендер.
            restoreCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = frameRate;

            Debug.Log($"[Recorder] пишу в {takeDirectory}");
        }

        [ContextMenu("Stop Recording")]
        public void StopRecording()
        {
            if (!IsRecording) return;

            Time.captureFramerate = restoreCaptureFramerate;

            var video = takeDirectory + ".mp4";
            Debug.Log($"[Recorder] {frame} кадров в {takeDirectory}\n"
                      + $"ffmpeg -y -framerate {frameRate} -i \"{Path.Combine(takeDirectory, "frame_%04d.png")}\" "
                      + $"-c:v libx264 -pix_fmt yuv420p -crf 18 \"{video}\"");

            takeDirectory = null;
        }

        private string NextTakeDirectory()
        {
            var root = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Recordings")
                : outputDirectory;

            for (var take = 1;; take++)
            {
                var candidate = Path.Combine(root, $"take_{take:000}");
                if (!Directory.Exists(candidate)) return candidate;
            }
        }
    }
}
