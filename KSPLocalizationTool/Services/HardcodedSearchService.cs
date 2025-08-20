using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class HardcodedSearchService(string searchDirectory)
    {
        private string _searchDirectory = searchDirectory;
        private static readonly string[] _searchPatterns = { "*.cs", "*.cshtml", "*.xml" };
        private static readonly HashSet<string> _hardcodedParameters = new HashSet<string>
        {
            "TitleBarText.text", "Row1HeaderLabel.text", "Row2HeaderLabel.text",
            "AlertText.text", "_transferTargetsController.DropdownDefaultText",
            "target.Value.DisplayName", "Column1HeaderText.text", "Column2HeaderText.text",
            "Column3HeaderText.text", "Column1Instructions.text", "Column2Instructions.text",
            "Column3Instructions.text", "ResourceHeaderText.text", "AvailableAmountHeaderText.text",
            "RequiredAmountHeaderText.text", "SelectedShipHeaderText.text", "ShipNameText.text",
            "ShipMassText.text", "ShipCostText.text", "BuildShipButtonText.text",
            "SelectShipButtonText.text", "_konstructor.InsufficientResourcesErrorText",
            "ResourceText.text", "RequiredAmountText.text", "AvailableAmountText.text",
            "HeaderLabel.text", "SliderALabel.text", "SliderBLabel.text", "TransferAmountInput.text"
        };

        public void UpdateSearchDirectory(string newDirectory)
        {
            if (!string.IsNullOrEmpty(newDirectory) && Directory.Exists(newDirectory))
            {
                _searchDirectory = newDirectory;
                LogManager.Log($"硬编码搜索目录已更新为: {_searchDirectory}");
            }
        }

        public List<LocalizationItem> SearchHardcodedItems(string localizationDirectory)
        {
            var results = new List<LocalizationItem>();

            if (string.IsNullOrEmpty(_searchDirectory) || !Directory.Exists(_searchDirectory))
            {
                LogManager.Log($"硬编码搜索目录不存在: {_searchDirectory}");
                return results;
            }

            LogManager.Log($"开始搜索硬编码文本: {_searchDirectory}");

            var files = new List<string>();
            foreach (var pattern in _searchPatterns)
            {
                try
                {
                    files.AddRange(Directory.EnumerateFiles(
                        _searchDirectory,
                        pattern,
                        SearchOption.AllDirectories)
                        .Where(f => !IsInLocalizationDirectory(f, localizationDirectory)));
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogManager.Log($"访问被拒绝: {ex.Message}");
                }
                catch (PathTooLongException ex)
                {
                    LogManager.Log($"路径过长: {ex.Message}");
                }
            }

            var uniqueFiles = files.Distinct().ToList();
            LogManager.Log($"找到 {uniqueFiles.Count} 个可能包含硬编码文本的文件");

            foreach (var file in uniqueFiles)
            {
                ProcessHardcodedFile(file, results);
            }

            LogManager.Log($"硬编码文本搜索完成，找到 {results.Count} 项");
            return results;
        }

        private void ProcessHardcodedFile(string filePath, List<LocalizationItem> results)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    foreach (var param in _hardcodedParameters)
                    {
                        if (line.Contains($"{param} = ") || line.Contains($"{param}=") ||
                            line.Contains($"{param}:"))
                        {
                            // 提取赋值的文本内容
                            string textValue = ExtractTextValue(line, param);
                            if (!string.IsNullOrEmpty(textValue) && !IsLocalizedKey(textValue))
                            {
                                results.Add(new LocalizationItem
                                {
                                    Key = "", // 暂时为空，替换时生成
                                    OriginalText = textValue,
                                    LocalizedText = textValue, // 默认与原文相同
                                    FilePath = filePath,
                                    LineNumber = i + 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"处理硬编码文件 {filePath} 出错: {ex.Message}");
            }
        }

        private string ExtractTextValue(string line, string parameter)
        {
            int equalsIndex = line.IndexOf('=', line.IndexOf(parameter));
            if (equalsIndex == -1)
                equalsIndex = line.IndexOf(':', line.IndexOf(parameter));

            if (equalsIndex == -1) return "";

            string valuePart = line.Substring(equalsIndex + 1).Trim();

            // 提取引号中的内容
            if (valuePart.StartsWith("\"") && valuePart.Contains("\""))
            {
                int start = 1;
                int end = valuePart.IndexOf("\"", 1);
                if (end > start)
                    return valuePart.Substring(start, end - start);
            }

            // 提取括号中的内容
            if (valuePart.StartsWith("(") && valuePart.Contains(")"))
            {
                int start = 1;
                int end = valuePart.IndexOf(")", 1);
                if (end > start)
                    return valuePart.Substring(start, end - start);
            }

            return valuePart.Split(';')[0].Trim();
        }

        private bool IsLocalizedKey(string text)
        {
            return text.StartsWith("LOC_") && text.IndexOf(' ') == -1;
        }

        private bool IsInLocalizationDirectory(string filePath, string localizationDirectory)
        {
            if (string.IsNullOrEmpty(localizationDirectory)) return false;
            return filePath.StartsWith(localizationDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }
}