using System;
using System.IO;
using System.Collections.Generic;

namespace KSPLocalizationTool.Services
{
    public class BackupManager
    {
        private string _backupDirectory;

        public BackupManager(string backupDirectory)
        {
            _backupDirectory = backupDirectory;
            InitializeBackupDirectory();
        }

        private void InitializeBackupDirectory()
        {
            if (!string.IsNullOrEmpty(_backupDirectory) && !Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                LogManager.Log($"已创建备份目录: {_backupDirectory}");
            }
        }

        public void SetBackupDirectory(string newDirectory)
        {
            if (!string.IsNullOrEmpty(newDirectory) && newDirectory != _backupDirectory)
            {
                _backupDirectory = newDirectory;
                InitializeBackupDirectory();
            }
        }

        public void BackupFiles(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
                return;

            InitializeBackupDirectory();

            // 创建带有时间戳的备份子目录
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupDirWithTimestamp = Path.Combine(_backupDirectory, timestamp);
            Directory.CreateDirectory(backupDirWithTimestamp);

            foreach (string filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        // 创建与原文件相同的目录结构
                        string relativePath = Path.GetDirectoryName(filePath) ?? "";
                        string targetDir = Path.Combine(backupDirWithTimestamp, relativePath);
                        Directory.CreateDirectory(targetDir);

                        // 复制文件到备份目录
                        string targetPath = Path.Combine(targetDir, Path.GetFileName(filePath));
                        File.Copy(filePath, targetPath, true);
                        LogManager.Log($"已备份文件: {filePath} 到 {targetPath}");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log($"备份文件 {filePath} 失败: {ex.Message}");
                }
            }
        }

        public void BackupFile(string filePath)
        {
            BackupFiles(new List<string> { filePath });
        }
    }
}