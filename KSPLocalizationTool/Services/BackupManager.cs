using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KSPLocalizationTool.Services
{
    public class BackupManager
    {
        private string _backupDirectory;

        public BackupManager(string backupDirectory)

        {
            SetBackupDirectory(backupDirectory);
        }

        public void SetBackupDirectory(string backupDirectory)
        {
            if (string.IsNullOrEmpty(backupDirectory))
            {
                backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
            }

            _backupDirectory = backupDirectory;
            EnsureDirectoryExists(_backupDirectory);
        }

        public void BackupFiles(List<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any())
                return;

            // 创建带时间戳的备份子目录
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupDirWithTimestamp = Path.Combine(_backupDirectory, timestamp);
            EnsureDirectoryExists(backupDirWithTimestamp);

            foreach (string filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        // 创建与源文件相同的目录结构
                        string relativePath = Path.GetDirectoryName(filePath)?.Substring(Path.GetPathRoot(filePath)?.Length ?? 0) ?? "";
                        string targetDir = Path.Combine(backupDirWithTimestamp, relativePath.TrimStart(Path.DirectorySeparatorChar));
                        EnsureDirectoryExists(targetDir);

                        // 复制文件
                        string targetPath = Path.Combine(targetDir, Path.GetFileName(filePath));
                        File.Copy(filePath, targetPath, true);
                        LogManager.Log($"已备份文件: {filePath} -> {targetPath}");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"备份文件 {filePath} 失败: {ex.Message}");
                }
            }
        }

        private void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
