using System;
using System.IO;
using System.Collections.Generic;

namespace KSPLocalizationTool.Services
{
    public class FileBackupService(LogService logService, AppConfig appConfig)
    {
        private readonly LogService _logService = logService;
        private readonly AppConfig _appConfig = appConfig;

        /// <summary>
        /// 记录已备份的文件
        /// </summary>
        private readonly HashSet<string> _backedUpFiles = new();

        /// <summary>
        /// 备份单个文件
        /// </summary>
        public void BackupFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                // 创建备份目录（如果不存在）
                if (!Directory.Exists(_appConfig.BackupDirectory))
                {
                    Directory.CreateDirectory(_appConfig.BackupDirectory);
                }

                // 生成备份路径（保留原始目录结构）
                string relativePath = Path.GetRelativePath(_appConfig.ModDirectory, filePath);
                string backupFilePath = Path.Combine(_appConfig.BackupDirectory,
                    $"{DateTime.Now:yyyyMMddHHmmss}_{relativePath}");

                // 创建备份文件目录
                string? backupDir = Path.GetDirectoryName(backupFilePath);
                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // 复制文件进行备份
                File.Copy(filePath, backupFilePath, true);
                _backedUpFiles.Add(filePath);
                _logService.LogMessage($"已备份文件: {backupFilePath}");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"备份文件失败 {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查文件是否已备份
        /// </summary>
        public bool IsFileBackedUp(string filePath)
        {
            return _backedUpFiles.Contains(filePath);
        }
    }
}