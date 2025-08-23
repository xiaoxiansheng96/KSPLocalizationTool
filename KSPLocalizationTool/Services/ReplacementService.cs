using KSPLocalizationTool.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace KSPLocalizationTool.Services
{
    // 替换功能的专门服务类（功能块）
    public class ReplacementService
    {
        private readonly LogService _logService;
        private readonly FileBackupService _backupService;

        // 使用主构造函数语法，并移除未使用的localizationService参数
        public ReplacementService(LogService logService, FileBackupService backupService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        }

        // 核心方法：执行替换操作（包含所有实施逻辑）
        public (int successCount, int failCount) ReplaceItems(List<LocalizationItem> items)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var item in items)
            {
                try
                {
                    // 1. 验证文件是否存在
                    if (string.IsNullOrEmpty(item.FilePath) || !File.Exists(item.FilePath))
                    {
                        _logService?.LogMessage($"文件不存在: {item.FilePath}");
                        failCount++;
                        continue;
                    }
        
                    // 2. 验证替换信息是否完整
                    if (string.IsNullOrEmpty(item.OriginalText) || string.IsNullOrEmpty(item.LocalizationKey))
                    {
                        _logService?.LogMessage($"替换信息不完整: {item.FilePath} (行号: {item.LineNumber})");
                        failCount++;
                        continue;
                    }
        
                    // 3. 备份文件（仅备份一次）
                    if (!_backupService.IsFileBackedUp(item.FilePath))
                    {
                        _backupService.BackupFile(item.FilePath);
                    }
        
                    // 4. 读取文件内容并替换
                    string fileContent = File.ReadAllText(item.FilePath);
                    string newContent = LocalizationService.ReplaceText(
                        fileContent,
                        item.ParameterType,
                        item.OriginalText,
                        item.LocalizationKey);
        
                    // 5. 内容有变化才写入
                    if (newContent != fileContent)
                    {
                        File.WriteAllText(item.FilePath, newContent);
                        successCount++;
                        _logService?.LogMessage($"已替换: {Path.GetFileName(item.FilePath)} (行号: {item.LineNumber})");
                    }
                    else
                    {
                        _logService?.LogMessage($"未发现可替换内容: {Path.GetFileName(item.FilePath)} (行号: {item.LineNumber})");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logService?.LogMessage($"替换失败 {item.FilePath} (行号: {item.LineNumber}): {ex.Message}");
                    failCount++;
                }
            }
        
            return (successCount, failCount);
        }
    }
}