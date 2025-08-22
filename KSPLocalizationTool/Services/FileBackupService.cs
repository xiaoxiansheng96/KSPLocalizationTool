using System;
using System.IO;
using System.Linq;
using System.Collections.Generic; // 添加HashSet所需的命名空间

namespace KSPLocalizationTool.Services
{
    public class FileBackupService
    {
        private readonly LogService _logService;
        private readonly AppConfig _appConfig;

        public FileBackupService(LogService logService, AppConfig appConfig)
        {
            _logService = logService;
            _appConfig = appConfig; // 初始化AppConfig
        }
        /// <summary>
        /// 记录已备份的文件
        /// </summary>
        private readonly HashSet<string> _backedUpFiles = new HashSet<string>();

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
                string backupDir = Path.GetDirectoryName(backupFilePath);
                if (!Directory.Exists(backupDir) && backupDir != null)
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

        /// <summary>
        /// 从备份恢复文件
        /// </summary>
        public void RestoreBackup(string backupDir, string targetDir)
        {
            if (!Directory.Exists(backupDir))
            {
                _logService.LogMessage($"备份目录不存在: {backupDir}");
                return;
            }

            // 恢复所有文件
            var backupFiles = Directory.EnumerateFiles(backupDir, "*.*", SearchOption.AllDirectories);

            foreach (var backupFile in backupFiles)
            {
                var relativePath = Path.GetRelativePath(backupDir, backupFile);
                var targetPath = Path.Combine(targetDir, relativePath);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    File.Copy(backupFile, targetPath, true);
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"恢复文件 {targetPath} 失败: {ex.Message}");
                }
            }
        }
    }
}