using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class LocalizationFileHandler
    {
        private readonly Dictionary<string, string> _localizationData = new();
        private string _languageCode;
        private string _localizationDirectory;
        private static readonly string[] _supportedExtensions = { ".cfg", ".txt", ".xml" };

        public LocalizationFileHandler(string languageCode, string localizationDirectory)
        {
            _languageCode = languageCode ?? "zh-cn";
            _localizationDirectory = localizationDirectory;
            LoadLocalizationFiles();
        }

        public void UpdateDirectory(string newDirectory)
        {
            if (!string.IsNullOrEmpty(newDirectory) && newDirectory != _localizationDirectory)
            {
                _localizationDirectory = newDirectory;
                LoadLocalizationFiles();
            }
        }

        public void SetTargetLanguage(string languageCode)
        {
            if (!string.IsNullOrEmpty(languageCode) && languageCode != _languageCode)
            {
                _languageCode = languageCode;
                LoadLocalizationFiles();
            }
        }

        private void LoadLocalizationFiles()
        {
            try
            {
                _localizationData.Clear();

                if (!Directory.Exists(_localizationDirectory))
                {
                    Directory.CreateDirectory(_localizationDirectory);
                    return;
                }

                var files = Directory.EnumerateFiles(_localizationDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => _supportedExtensions.Any(ext =>
                        string.Equals(ext, Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)));

                foreach (var file in files)
                {
                    LoadLocalizationFile(file);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载本地化文件失败: {ex.Message}");
            }
        }

        private void LoadLocalizationFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                bool inTargetLanguageBlock = false;
                int braceLevel = 0;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    // 检测目标语言区块
                    if (trimmedLine.StartsWith(_languageCode, StringComparison.OrdinalIgnoreCase) &&
                        trimmedLine.EndsWith("{"))
                    {
                        inTargetLanguageBlock = true;
                        braceLevel = 1;
                        continue;
                    }

                    // 跟踪括号层级
                    if (inTargetLanguageBlock)
                    {
                        if (trimmedLine == "{") braceLevel++;
                        else if (trimmedLine == "}") braceLevel--;

                        // 退出目标语言区块
                        if (braceLevel == 0)
                        {
                            inTargetLanguageBlock = false;
                            continue;
                        }

                        // 提取键值对
                        if (trimmedLine.Contains('=') && !trimmedLine.StartsWith("//"))
                        {
                            string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();
                                if (!_localizationData.ContainsKey(key))
                                {
                                    _localizationData[key] = value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"加载文件 {filePath} 失败: {ex.Message}");
            }
        }

        public string GetLocalizedText(string key)
        {
            return _localizationData.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public bool IsKeyExists(string key)
        {
            return _localizationData.ContainsKey(key);
        }

        public void SaveLocalizationItems(List<LocalizationItem> items)
        {
            if (items == null || items.Count == 0) return;

            try
            {
                // 按文件分组处理
                var itemsByFile = items.GroupBy(i => i.FilePath);
                foreach (var group in itemsByFile)
                {
                    ProcessFileForSaving(group.Key, group.ToList());
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存本地化项失败: {ex.Message}");
            }
        }

        private void ProcessFileForSaving(string filePath, List<LocalizationItem> items)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                List<string> newLines = new List<string>();

                foreach (string line in lines)
                {
                    string modifiedLine = line;
                    foreach (var item in items)
                    {
                        // 替换参数文本
                        if (line.Contains($"{item.Key} = "))
                        {
                            modifiedLine = $"{item.Key} = {item.LocalizedText}";
                            break;
                        }
                        // 替换硬编码文本
                        else if (_searchingHardcoded && line.Contains(item.OriginalText) &&
                                 !line.Contains("LOC_") && !line.StartsWith("//"))
                        {
                            modifiedLine = line.Replace(item.OriginalText, item.Key);
                            break;
                        }
                    }
                    newLines.Add(modifiedLine);
                }

                File.WriteAllLines(filePath, newLines);
                LogManager.Log($"已更新文件: {filePath}");
            }
            catch (Exception ex)
            {
                LogManager.Log($"更新文件 {filePath} 失败: {ex.Message}");
            }
        }

        // 用于在MainForm中访问搜索状态的辅助属性
        internal bool _searchingHardcoded { get; set; }
    }
}