using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PeekShot
{
    /// <summary>
    /// 程序入口与流程编排：
    /// 同意弹窗 → 键鼠监控 → （触发）拍照 + 锁屏 /（热键）直接退出。
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Config config = Config.Load(Path.Combine(exeDirectory, "PeekShot.ini"));

            // 辅助模式：仅验证拍照管线，不安装钩子、不锁屏
            if (args != null && args.Length > 0 && args[0] == "--test-capture")
            {
                return RunTestCapture(config);
            }

            Log.Write("启动。摄像头=\"" + config.CameraName + "\" 热键=\"" +
                      config.HotkeyText + "\" 超时=" + config.CaptureTimeoutSeconds + "s");

            if (!ShowConsent(config))
            {
                Log.Write("用户拒绝启用，退出");
                return 0;
            }

            InputMonitor monitor = new InputMonitor(config);
            Thread monitorThread = new Thread(monitor.Run);
            monitorThread.Start();
            monitorThread.Join();

            Outcome outcome = monitor.FinalOutcome;
            Log.Write("监控结束，结果=" + outcome);

            if (outcome == Outcome.Cancelled)
            {
                // 用户热键退出：不拍照、不锁屏
                return 0;
            }

            if (outcome == Outcome.Triggered)
            {
                TryCapturePhoto(config);
            }
            // 其余情况（LockOnly / None）：跳过拍照，仅锁屏

            NativeMethods.LockWorkStation();
            Log.Write("已请求锁屏，退出");
            return 0;
        }

        // ==================== 同意弹窗 ====================

        private static bool ShowConsent(Config config)
        {
            using (ConsentDialog dialog = new ConsentDialog(config))
            {
                return dialog.ShowDialog() == DialogResult.Yes;
            }
        }

        // ==================== 拍照与锁屏 ====================

        private static void TryCapturePhoto(Config config)
        {
            string photoDirectory = GetPhotoDirectory();
            if (photoDirectory == null)
            {
                Log.Write("照片目录不可用，跳过拍照");
                return;
            }

            string photoPath = Path.Combine(
                photoDirectory,
                "PeekShot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".jpg");

            CaptureJob job = new CaptureJob(config, photoPath);
            Thread captureThread = new Thread(job.Execute);
            captureThread.IsBackground = true;
            captureThread.Start();

            // 看门狗：超时则放弃线程（不 Abort），继续锁屏；
            // 进程退出会回收残留线程。
            if (!captureThread.Join(config.CaptureTimeoutSeconds * 1000))
            {
                Log.Write("拍照超时（>" + config.CaptureTimeoutSeconds + "s），放弃并继续锁屏");
            }
        }

        private static string GetPhotoDirectory()
        {
            try
            {
                string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (string.IsNullOrEmpty(baseDirectory))
                {
                    baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }
                string directory = Path.Combine(baseDirectory, "PeekShot");
                Directory.CreateDirectory(directory);
                return directory;
            }
            catch (Exception ex)
            {
                Log.Write("创建照片目录失败：" + ex.Message);
                return null;
            }
        }

        // ==================== 测试模式 ====================

        private static int RunTestCapture(Config config)
        {
            string photoDirectory = GetPhotoDirectory();
            if (photoDirectory == null) return 1;

            string photoPath = Path.Combine(
                photoDirectory,
                "PeekShot_test_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".jpg");

            Log.Write("测试模式开始：摄像头=\"" + config.CameraName + "\"");

            CaptureJob job = new CaptureJob(config, photoPath);
            Thread captureThread = new Thread(job.Execute);
            captureThread.IsBackground = true;
            captureThread.Start();
            captureThread.Join(Math.Max(config.CaptureTimeoutSeconds, 15) * 1000);

            string message = "结果：" + job.Result + "\r\n" + job.Detail;
            Log.Write("测试模式结束: " + message);
            MessageBox.Show(message, "PeekShot 拍照测试");
            return job.Result == CaptureResult.Saved ? 0 : 1;
        }
    }

    /// <summary>
    /// 极简日志：追加到 图片\PeekShot\PeekShot_error.log。
    /// 只记录时间戳与文字，不写任何像素数据；日志失败静默忽略。
    /// </summary>
    internal static class Log
    {
        private static readonly object LockObject = new object();

        public static void Write(string message)
        {
            try
            {
                string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (string.IsNullOrEmpty(baseDirectory)) return;
                string directory = Path.Combine(baseDirectory, "PeekShot");
                string path = Path.Combine(directory, "PeekShot_error.log");
                lock (LockObject)
                {
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(path,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + "\r\n");
                }
            }
            catch
            {
                // 日志不可用时静默忽略
            }
        }
    }
}
