using System;
using System.IO;
using System.Text;

namespace KSPLocalizationTool.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogLevel Level { get; }

        // 使用主构造函数替代传统构造函数
        public LogEventArgs(string message, LogLevel level = LogLevel.Info) =>
            (Message, Level) = (message, level);
    }

    public class LogService
    {
        public string LogDirectory { get; }
        private readonly string _logFilePath;
        // 删除未使用的字段

        public event EventHandler<LogEventArgs>? LogUpdated;

        public LogService(string logDirectory)
        {
            LogDirectory = logDirectory;
            Directory.CreateDirectory(LogDirectory);

            // 日志文件按日期命名
            _logFilePath = Path.Combine(LogDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
        }

        public void LogMessage(string message)
        {
            try
            {
                // 写入日志文件
                using (var writer = new StreamWriter(_logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
                }

                // 触发日志更新事件
                LogUpdated?.Invoke(this, new LogEventArgs(message));
            }
            catch (Exception ex)
            {
                // 日志记录失败时不抛出异常，避免程序崩溃
                Console.WriteLine($"日志记录失败: {ex.Message}");
            }
        }
    }
}