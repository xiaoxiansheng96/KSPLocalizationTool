using System;
using System.Collections.Generic;
using System.IO;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class LocalizationGeneratorService
    {
        private string _localizationDirectory;
        private string _targetLanguageCode;

        public LocalizationGeneratorService(string localizationDirectory, string targetLanguageCode)
        {
            _localizationDirectory = localizationDirectory;
            _targetLanguageCode = targetLanguageCode;
            EnsureDirectoryExists();
        }

        public void UpdateSettings(string localizationDirectory, string targetLanguageCode)
        {
            _localizationDirectory = localizationDirectory;
            _targetLanguageCode = targetLanguageCode;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!string.IsNullOrEmpty(_localizationDirectory) && !Directory.Exists(_localizationDirectory))
            {
                Directory.CreateDirectory(_localizationDirectory);
                LogManager.Log($"已创建本地化目录: {_localizationDirectory}");
            }
        }

        public void GenerateOrUpdateLocalizationFiles(List<LocalizationItem> items)
        {
            if (items == null || items.Count == 0) return;

            EnsureDirectoryExists();

            // 生成或更新默认语言文件 (en-us.cfg)
            string enUsFilePath = Path.Combine(_localizationDirectory, "en-us.cfg");
            UpdateLocalizationFile(enUsFilePath, items, "en-us");

            // 生成或更新目标语言文件
            string targetFilePath = Path.Combine(_localizationDirectory, $"{_targetLanguageCode}.cfg");
            UpdateLocalizationFile(targetFilePath, items, _targetLanguageCode);
        }

        private void UpdateLocalizationFile(string filePath, List<LocalizationItem> items, string languageCode)
        {
            Dictionary<string, string> existingEntries = new Dictionary<string, string>();
            List<string> fileContent = new List<string>();
            bool fileExists = File.Exists(filePath);

            // 如果文件存在，读取现有内容
            if (fileExists)
            {
                fileContent.AddRange(File.ReadAllLines(filePath));
                existingEntries = ParseExistingLocalizationFile(fileContent);
            }
            else
            {
                // 创建新文件的基本结构
                fileContent.Add("Localization");
                fileContent.Add("{");
                fileContent.Add($"    {languageCode}");
                fileContent.Add("    {");
                fileContent.Add("    }");
                fileContent.Add("}");
            }

            // 添加新的本地化项
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Key) || existingEntries.ContainsKey(item.Key))
                    continue;

                string value = languageCode == "en-us" ? item.OriginalText : item.LocalizedText;

                // 找到语言区块的位置
                int languageBlockStart = -1;
                int languageBlockEnd = -1;

                for (int i = 0; i < fileContent.Count; i++)
                {
                    if (fileContent[i].Trim().StartsWith($"{languageCode}") &&
                        fileContent[i].TrimEnd().EndsWith("{"))
                    {
                        languageBlockStart = i;

                        // 找到对应的结束括号
                        int braceCount = 1;
                        for (int j = i + 1; j < fileContent.Count; j++)
                        {
                            string trimmedLine = fileContent[j].Trim();
                            if (trimmedLine == "{") braceCount++;
                            else if (trimmedLine == "}") braceCount--;

                            if (braceCount == 0)
                            {
                                languageBlockEnd = j;
                                break;
                            }
                        }
                        break;
                    }
                }

                // 如果找到了语言区块，添加新的本地化项
                if (languageBlockStart != -1 && languageBlockEnd != -1)
                {
                    // 添加时间注释
                    string comment = $"    // {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    fileContent.Insert(languageBlockEnd, comment);

                    // 添加本地化键值对
                    string entry = $"    {item.Key} = {value}";
                    fileContent.Insert(languageBlockEnd + 1, entry);

                    existingEntries[item.Key] = value;
                    LogManager.Log($"已添加本地化项到 {Path.GetFileName(filePath)}: {item.Key}");
                }
            }

            // 保存文件
            File.WriteAllLines(filePath, fileContent);
            LogManager.Log($"{(fileExists ? "已更新" : "已创建")} 本地化文件: {filePath}");
        }

        private Dictionary<string, string> ParseExistingLocalizationFile(List<string> fileContent)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>();
            bool inLocalizationBlock = false;
            string currentLanguage = "";

            foreach (string line in fileContent)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine == "Localization" && line.TrimEnd().EndsWith("{"))
                {
                    inLocalizationBlock = true;
                    continue;
                }

                if (inLocalizationBlock && trimmedLine == "}")
                {
                    inLocalizationBlock = false;
                    currentLanguage = "";
                    continue;
                }

                if (inLocalizationBlock && !string.IsNullOrEmpty(trimmedLine) &&
                    !trimmedLine.StartsWith("//") && trimmedLine.EndsWith("{"))
                {
                    currentLanguage = trimmedLine.Split('{')[0].Trim();
                    continue;
                }

                if (!string.IsNullOrEmpty(currentLanguage) && trimmedLine.Contains('=') &&
                    !trimmedLine.StartsWith("//"))
                {
                    string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        if (!entries.ContainsKey(key))
                        {
                            entries[key] = value;
                        }
                    }
                }
            }

            return entries;
        }
    }
}