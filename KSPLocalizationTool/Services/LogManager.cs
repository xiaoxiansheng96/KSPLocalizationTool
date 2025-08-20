using System;
using System.Collections.Generic;
using System.IO;

namespace KSPLocalizationTool.Services
{
    public static class LogManager
    {
        // 修复IDE0028：简化集合初始化
        private static readonly List<string> _logEntries = new();
        private static readonly string _logFilePath;

        static LogManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDir = Path.Combine(appData, "KSPLocalizationTool", "Logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
        }

        public static void Log(string message)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            _logEntries.Add(entry);
            
            // 限制日志条目数量
            if (_logEntries.Count > 1000)
            {
                _logEntries.RemoveRange(0, _logEntries.Count - 1000);
            }
        }

        public static IEnumerable<string> GetLogEntries() => _logEntries.AsReadOnly();

        public static void SaveLog()
        {
            try
            {
                File.AppendAllLines(_logFilePath, _logEntries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存日志失败: {ex.Message}");
            }
        }
    }
}
    