using System;
using System.IO;

namespace KSPLocalizationTool.Services
{
    public static class LogManager
    {
        private static readonly string _logFilePath;
        private static readonly object _lockObj = new object();
        private static string _logContent = string.Empty;

        // 添加日志更新事件
        public static event Action<string>? Logged;

        static LogManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDir = Path.Combine(appData, "KSPLocalizationTool", "Logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"{DateTime.Now:yyyyMMdd}.log");
        }

        public static void Log(string message)
        {
            lock (_lockObj)
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                _logContent += logEntry;

                try
                {
                    File.AppendAllText(_logFilePath, logEntry);
                    // 触发日志更新事件
                    Logged?.Invoke(message);
                }
                catch (Exception ex)
                {
                    // 日志写入失败时不抛出异常，避免影响主程序
                    Console.WriteLine($"日志写入失败: {ex.Message}");
                }
            }
        }

        public static void SaveLog()
        {
            // 已在Log方法中实时写入，此处仅作保障
            try
            {
                if (!string.IsNullOrEmpty(_logContent))
                {
                    File.AppendAllText(_logFilePath, _logContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志保存失败: {ex.Message}");
            }
        }

        public static string GetLogContent()
        {
            lock (_lockObj)
            {
                return _logContent;
            }
        }
    }
}