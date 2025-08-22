using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class FileSearchService
    {
        private readonly LogService _logService;
        private int _processedFiles;

        public FileSearchService(LogService logService)
        {
            _logService = logService;
        }

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

            if (!Directory.Exists(rootDirectory))
            {
                _logService.LogMessage($"目录不存在: {rootDirectory}");
                return results;
            }

            // 搜索所有CFG和CS文件，排除本地化目录
            var cfgFiles = Directory.EnumerateFiles(rootDirectory, "*.cfg", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(localizationDirectory, StringComparison.OrdinalIgnoreCase));

            var csFiles = Directory.EnumerateFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(localizationDirectory, StringComparison.OrdinalIgnoreCase));

            // 处理CFG文件
            foreach (var file in cfgFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessCfgFile(file, cfgFilters, results);
                _processedFiles++;

                if (_processedFiles % 10 == 0)
                {
                    _logService.LogMessage($"已处理 {_processedFiles} 个文件");
                }
            }

            // 处理CS文件
            foreach (var file in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessCsFile(file, csFilters, results);
                _processedFiles++;

                if (_processedFiles % 10 == 0)
                {
                    _logService.LogMessage($"已处理 {_processedFiles} 个文件");
                }
            }

            return results;
        }

        // KSPLocalizationTool/Services/FileSearchService.cs
        private void ProcessCfgFile(string filePath, string[] filters, List<SearchResultItem> results)
        {
            try
            {
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