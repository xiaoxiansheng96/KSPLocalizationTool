using System;
using System.Collections.Generic;
using System.Threading;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    // 使用主构造函数简化代码
    public class ReplacementService(LocalizationFileHandler localizationHandler, BackupManager backupManager)
    {
        private readonly LocalizationFileHandler _localizationHandler = localizationHandler
            ?? throw new ArgumentNullException(nameof(localizationHandler));
        private readonly BackupManager _backupManager = backupManager
            ?? throw new ArgumentNullException(nameof(backupManager));

        // 进度更新事件
        public event Action<int>? ProgressUpdated;

        /// <summary>
        /// 替换所有项
        /// </summary>
        public void ReplaceAllItems(List<LocalizationItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            int total = items.Count;
            int completed = 0;

            foreach (var item in items)
            {
                try
                {
                    // 备份文件
                    _backupManager.BackupFile(item.FilePath);

                    // 执行替换（模拟操作）
                    Thread.Sleep(10);

                    completed++;
                    int progress = (int)((double)completed / total * 100);
                    ProgressUpdated?.Invoke(progress);
                }
                catch (Exception ex)
                {
                    LogManager.Log($"替换项 {item.Key} 失败: {ex.Message}");
                }
            }
        }
    }
}
