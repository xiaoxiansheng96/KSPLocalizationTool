using System;
using System.IO;
using KSPLocalizationTool.Services;

namespace KSPLocalizationTool.Services
{
    public class BackupManager
    {
        // 修复：确保字段可为null或在构造函数中初始化
        private string? _backupDirectory;

        public BackupManager(string backupDirectory)
        {
            if (string.IsNullOrEmpty(backupDirectory))
                throw new ArgumentException("备份目录不能为空", nameof(backupDirectory));

            _backupDirectory = backupDirectory;
            EnsureDirectoryExists();
        }

        public void SetBackupDirectory(string backupDirectory)
        {
            if (string.IsNullOrEmpty(backupDirectory))
                throw new ArgumentException("备份目录不能为空", nameof(backupDirectory));

            _backupDirectory = backupDirectory;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            // 修复可能的空引用
            if (!string.IsNullOrEmpty(_backupDirectory) && !Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        public void BackupFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                // 修复可能的空引用
                if (string.IsNullOrEmpty(_backupDirectory))
                    throw new InvalidOperationException("备份目录未设置");

                string fileName = Path.GetFileName(filePath);
                string relativePath = Path.GetDirectoryName(filePath)?.Replace(
                    AppDomain.CurrentDomain.BaseDirectory, "") ?? "";

                string backupPath = Path.Combine(_backupDirectory, relativePath);
                string backupFile = Path.Combine(backupPath,
                    $"{fileName}.bak_{DateTime.Now:yyyyMMddHHmmss}");

                Directory.CreateDirectory(backupPath);
                File.Copy(filePath, backupFile, true);
                LogManager.Log($"已备份文件: {filePath} -> {backupFile}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"备份文件失败: {ex.Message}");
            }
        }
    }
}
