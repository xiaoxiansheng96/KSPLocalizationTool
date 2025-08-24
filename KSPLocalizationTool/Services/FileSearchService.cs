// 修改FileSearchService类，添加详细日志和统计信息
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class FileSearchService(LogService logService)
    {
        private readonly LogService _logService = logService;
        private int _processedFiles;
        private int _totalFiles;
        private int _cfgFileCount; // CFG文件计数
        private int _csFileCount;  // CS文件计数
        private int _foundTextCount; // 找到的文本计数

        // 添加ProgressUpdated事件
        public event Action<int>? ProgressUpdated;

        /// <summary>
        /// 搜索文件中需要本地化的内容
        /// </summary>
        public List<SearchResultItem> SearchFiles(
            string rootDirectory,
            string[] cfgFilters,
            string[] csFilters,
            string localizationDirectory,
            CancellationToken cancellationToken)
        {
            var results = new List<SearchResultItem>();
            _processedFiles = 0;
            _cfgFileCount = 0;
            _csFileCount = 0;
            _foundTextCount = 0;

            _logService.LogMessage($"开始搜索: 根目录={rootDirectory} [KSPLocalizationTool]");
            _logService.LogMessage($"搜索过滤器 - CFG: {string.Join(", ", cfgFilters)}, CS: {string.Join(", ", csFilters)} [KSPLocalizationTool]");

            if (!Directory.Exists(rootDirectory))
            {
                _logService.LogMessage($"目录不存在: {rootDirectory}) [KSPLocalizationTool]");
                return results;
            }

            // 搜索所有CFG和CS文件，排除本地化目录和backup文件夹
            var cfgFiles = Directory.EnumerateFiles(rootDirectory, "*.cfg", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(localizationDirectory, StringComparison.OrdinalIgnoreCase) &&
                               path.IndexOf("\\backup\\", StringComparison.OrdinalIgnoreCase) < 0);

            var csFiles = Directory.EnumerateFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(localizationDirectory, StringComparison.OrdinalIgnoreCase) &&
                               path.IndexOf("\\backup\\", StringComparison.OrdinalIgnoreCase) < 0);

            _totalFiles = cfgFiles.Count() + csFiles.Count();

            // 处理CFG文件
            foreach (var file in cfgFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessCfgFile(file, cfgFilters, results);
                _processedFiles++;
                UpdateProgress();
            }

            // 处理CS文件
            foreach (var file in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessCsFile(file, csFilters, results);
                _processedFiles++;
                UpdateProgress();
            }

            // 搜索结束后输出统计信息
            _logService.LogMessage($"搜索完成: 共搜索 {_totalFiles} 个文件，其中CFG文件 {_cfgFileCount} 个，CS文件 {_csFileCount} 个 [KSPLocalizationTool]");
            _logService.LogMessage($"找到 {_foundTextCount} 项需要本地化的文本 [KSPLocalizationTool]");

            return results;
        }

        private void UpdateProgress()
        {
            if (_totalFiles > 0)
            {
                int progress = (int)((double)_processedFiles / _totalFiles * 100);
                ProgressUpdated?.Invoke(progress);
            }
        }

        // KSPLocalizationTool/Services/FileSearchService.cs
        private void ProcessCfgFile(string filePath, string[] filters, List<SearchResultItem> results)
        {
            try
            {
                _cfgFileCount++;
                _logService.LogMessage($"正在处理CFG文件: {filePath} [KSPLocalizationTool]");
                var lines = File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++) // i为行索引（从0开始）
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    foreach (var filter in filters)
                    {
                        var pattern = $@"{filter}\s*=\s*[""'](.*?)[""']";
                        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);

                        if (match.Success && match.Groups.Count > 1)
                        {
                            var value = match.Groups[1].Value.Trim();

                            if (!string.IsNullOrEmpty(value) && !value.StartsWith("#LOC"))
                            {
                                results.Add(new SearchResultItem
                                {
                                    FilePath = filePath,
                                    ParameterType = filter,
                                    OriginalText = value,
                                    LineNumber = i + 1 // 行号从1开始
                                });
                                _foundTextCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"处理文件 {filePath} 时出错: {ex.Message}");
            }
        }

        // KSPLocalizationTool/Services/FileSearchService.cs
        private void ProcessCsFile(string filePath, string[] filters, List<SearchResultItem> results)
        {
            try
            {
                _csFileCount++;
                _logService.LogMessage($"正在处理CS文件: {filePath} [KSPLocalizationTool]");
                var lines = File.ReadAllLines(filePath); // 按行读取文件

                for (int i = 0; i < lines.Length; i++) // i为行索引（从0开始）
                {
                    var line = lines[i];
                    foreach (var filter in filters)
                    {
                        var pattern = $@"({filter}\w*)\s*[=:]\s*[""'](.*?)[""']";
                        var matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);

                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count > 2)
                            {
                                var paramType = match.Groups[1].Value;
                                var value = match.Groups[2].Value.Trim();

                                if (!string.IsNullOrEmpty(value) && !value.StartsWith("#LOC"))
                                {
                                    results.Add(new SearchResultItem
                                    {
                                        FilePath = filePath,
                                        ParameterType = paramType,
                                        OriginalText = value,
                                        LineNumber = i + 1 // 行号从1开始
                                    });
                                    _foundTextCount++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"处理文件 {filePath} 时出错: {ex.Message}");
            }
        }
    }
}