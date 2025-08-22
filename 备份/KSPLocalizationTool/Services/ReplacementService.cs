using System;
using System.Collections.Generic;
using System.Linq;
using KSPLocalizationTool.Models;
using KSPLocalizationTool.Services; // 确保包含正确的命名空间引用

namespace KSPLocalizationTool.Services
{
    // 使用主构造函数简化初始化
    public class ReplacementService(LocalizationFileHandler localizationHandler, BackupManager backupManager)
    {
        private readonly LocalizationFileHandler _localizationHandler = localizationHandler;
        private readonly BackupManager _backupManager = backupManager;

        // 进度更新事件
        public event Action<int>? ProgressUpdated;

        // 替换完成事件
        public event Action<bool, string>? ReplacementCompleted;

        // 删除UpdateDependencies方法

        public void ReplaceAll(List<LocalizationItem> items)
        {
            if (items is null || items.Count == 0)
            {
                ReplacementCompleted?.Invoke(false, "没有可替换的本地化项");
                return;
            }

            try
            {
                int totalItems = items.Count;
                int completed = 0;

                // 获取所有唯一的文件路径
                var uniqueFiles = items.Select(i => i.FilePath).Distinct().ToList();

                // 批量创建文件备份（使用BackupManager中实际存在的BackupFiles方法）
                _backupManager.BackupFiles(uniqueFiles);

                // 按文件分组处理替换
                var itemsByFile = items.GroupBy(i => i.FilePath);

                foreach (var fileGroup in itemsByFile)
                {
                    // 处理每个文件的所有项
                    foreach (var item in fileGroup)
                    {
                        // 补充本地化文本（如果尚未设置）
                        if (string.IsNullOrEmpty(item.LocalizedText))
                        {
                            item.LocalizedText = _localizationHandler.GetLocalizedText(item.Key);
                        }

                        completed++;

                        // 计算并报告进度
                        int progress = (int)((double)completed / totalItems * 100);
                        ProgressUpdated?.Invoke(progress);
                    }
                }

                // 保存所有更改
                _localizationHandler.SaveLocalizationItems(items);
                ReplacementCompleted?.Invoke(true, $"成功替换 {completed} 个本地化项");
            }
            catch (Exception ex)
            {
                LogManager.Log($"替换过程出错: {ex.Message}");
                ReplacementCompleted?.Invoke(false, ex.Message);
            }
        }
    }
}