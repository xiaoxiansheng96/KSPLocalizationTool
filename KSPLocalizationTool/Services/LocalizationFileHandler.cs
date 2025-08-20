using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class LocalizationFileHandler
    {
        // 修复IDE0028：简化集合初始化
        private readonly Dictionary<string, string> _localizationData = new();
        private string _languageCode;
        private string _localizationDirectory;

        // 修复IDE0300：简化集合初始化
        private static readonly string[] _supportedExtensions = { ".cfg", ".txt", ".xml" };

        public LocalizationFileHandler(string languageCode, string localizationDirectory)
        {
            _languageCode = languageCode ?? "zh-cn";
            _localizationDirectory = localizationDirectory;

            LoadLocalizationFiles();
        }

        // 添加缺失的方法：更新目录
        public void UpdateDirectory(string newDirectory)
        {
            if (!string.IsNullOrEmpty(newDirectory) && newDirectory != _localizationDirectory)
            {
                _localizationDirectory = newDirectory;
                LoadLocalizationFiles(); // 重新加载文件
            }
        }

        // 添加缺失的方法：设置目标语言
        public void SetTargetLanguage(string languageCode)
        {
            if (!string.IsNullOrEmpty(languageCode) && languageCode != _languageCode)
            {
                _languageCode = languageCode;
                LoadLocalizationFiles(); // 重新加载文件
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
            // 实现文件加载逻辑
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    // 假设文件格式为 "键=值"
                    string[] parts = line.Split(new[] { '=' }, 2);
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
            catch (Exception ex)
            {
                LogManager.Log($"加载文件 {filePath} 失败: {ex.Message}");
            }
        }

        public bool IsKeyExists(string key)
        {
            return !string.IsNullOrEmpty(key) && _localizationData.ContainsKey(key);
        }

        public string GetLocalizedText(string key)
        {
            return _localizationData.TryGetValue(key, out string? value) ? value ?? string.Empty : string.Empty;
        }

        public void SaveLocalizationItems(List<LocalizationItem> items)
        {
            // 按文件分组保存
            var itemsByFile = items.GroupBy(item => item.FilePath);

            foreach (var fileGroup in itemsByFile)
            {
                SaveLocalizationFile(fileGroup.Key, fileGroup.ToList());
            }
        }

        private void SaveLocalizationFile(string filePath, List<LocalizationItem> items)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        foreach (var item in items)
                        {
                            if (lines[i].StartsWith($"{item.Key}="))
                            {
                                lines[i] = $"{item.Key}={item.LocalizedText}";
                                break;
                            }
                        }
                    }

                    File.WriteAllLines(filePath, lines);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"保存文件 {filePath} 失败: {ex.Message}");
                throw;
            }
        }
    }
}
