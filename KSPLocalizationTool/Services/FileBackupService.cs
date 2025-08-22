using System;
using System.IO;
using System.Linq;

namespace KSPLocalizationTool.Services
{
    public class FileBackupService
    {
        private readonly LogService _logService;

        public FileBackupService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 备份单个文件
        /// </summary>
        public void BackupFile(string sourcePath, string backupDir)
        {
            try
            {
                // 创建与源文件相同的目录结构
                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(sourcePath)!, sourcePath);
                var backupPath = Path.Combine(backupDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

                // 复制文件
                File.Copy(sourcePath, backupPath, true);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"备份文件 {sourcePath} 失败: {ex.Message}");
            }
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