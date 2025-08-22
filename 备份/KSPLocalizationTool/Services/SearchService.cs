using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    // 使用主构造函数简化初始化
    public class SearchService(string searchDirectory)
    {
        // 搜索目录字段
        private string _searchDirectory = searchDirectory;

        // 支持的文件扩展名 - 类级别静态只读字段
        private static readonly string[] _searchPatterns = { "*.cfg", "*.txt", "*.xml" };

        // 支持的扩展名集合，用于快速查找
        private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cfg", ".txt", ".xml"
        };

        /// <summary>
        /// 更新搜索目录
        /// </summary>
        /// <param name="newDirectory">新的搜索目录路径</param>
        public void UpdateSearchDirectory(string newDirectory)
        {
            if (!string.IsNullOrEmpty(newDirectory) && Directory.Exists(newDirectory))
            {
                _searchDirectory = newDirectory;
                LogManager.Log($"搜索目录已更新为: {_searchDirectory}");
            }
            else
            {
                LogManager.Log($"无效的搜索目录: {newDirectory}");
            }
        }

        /// <summary>
        /// 搜索指定目录中的所有本地化项
        /// </summary>
        /// <returns>找到的本地化项列表</returns>
        public List<LocalizationItem> SearchLocalizationItems()
        {
            var results = new List<LocalizationItem>();

            try
            {
                // 验证目录是否存在
                if (string.IsNullOrEmpty(_searchDirectory) || !Directory.Exists(_searchDirectory))
                {
                    LogManager.Log($"搜索目录不存在或无效: {_searchDirectory}");
                    return results;
                }

                LogManager.Log($"开始在 {_searchDirectory} 中搜索本地化项");

                // 收集所有符合条件的文件
                var files = new List<string>();
                foreach (var pattern in _searchPatterns)
                {
                    try
                    {
                        // 枚举目录中的文件，包括子目录
                        var foundFiles = Directory.EnumerateFiles(
                            _searchDirectory,
                            pattern,
                            SearchOption.AllDirectories);

                        files.AddRange(foundFiles);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogManager.Log($"访问目录被拒绝: {ex.Message}");
                    }
                    catch (PathTooLongException ex)
                    {
                        LogManager.Log($"路径过长: {ex.Message}");
                    }
                }

                // 去除重复文件
                var uniqueFiles = files.Distinct().ToList();
                LogManager.Log($"找到 {uniqueFiles.Count} 个候选文件");

                // 逐个文件处理
                foreach (var file in uniqueFiles)
                {
                    ProcessFile(file, results);
                }

                LogManager.Log($"搜索完成，共找到 {results.Count} 个本地化项");
            }
            catch (Exception ex)
            {
                LogManager.Log($"搜索过程发生错误: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 处理单个文件，提取本地化项
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="results">存储结果的列表</param>
        private static void ProcessFile(string filePath, List<LocalizationItem> results)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                // 读取文件内容
                var lines = File.ReadAllLines(filePath);

                // 提取本地化项
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    // 跳过空行和注释行
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#"))
                        continue;

                    // 匹配类似 "key = value" 的模式
                    if (line.Contains('='))
                    {
                        // 使用字符重载提高性能
                        var parts = line.Split('=', 2); // 修复CA1866：使用字符重载
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            // 只添加有意义的项
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                results.Add(new LocalizationItem
                                {
                                    Key = key,
                                    OriginalText = value,
                                    LocalizedText = string.Empty,
                                    FilePath = filePath,
                                    LineNumber = i + 1 // 行号从1开始
                                });
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                LogManager.Log($"处理文件 {Path.GetFileName(filePath)} 时发生I/O错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"处理文件 {Path.GetFileName(filePath)} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断文件是否为支持的本地化文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>如果是支持的本地化文件则返回true，否则返回false</returns>
        public static bool IsLocalizationFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName);
            return _supportedExtensions.Contains(extension);
        }

        /// <summary>
        /// 判断一行文本是否包含本地化键
        /// </summary>
        /// <param name="line">要检查的文本行</param>
        /// <returns>如果包含本地化键则返回true，否则返回false</returns>
        public static bool ContainsLocalizationKey(string line)
        {
            return !string.IsNullOrEmpty(line)
                && line.Contains('=') // 修复CA1866：使用字符重载
                && !line.StartsWith("//")
                && !line.StartsWith("#");
        }
    }
}