using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KSPLocalizationTool.Models;

namespace KSPLocalizationTool.Services
{
    public class LocalizationService
    {
        private readonly LogService _logService;

        public LocalizationService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 替换文本并添加注释
        /// </summary>
        public string ReplaceText(string content, string parameterType, string originalText, string localizationKey)
        {
            // 对于CFG文件格式: parameterType = "originalText"
            var cfgPattern = $@"({parameterType}\s*=\s*"")({originalText})([""])";
            var cfgReplacement = $"$1{localizationKey}$3{Environment.NewLine}// 原始文本: {originalText}";

            // 对于CS文件格式: 各种包含字符串的情况
            var csPattern = $@"({parameterType}\s*[=:]\s*[""'])({originalText})([""'])";
            var csReplacement = $"$1{localizationKey}$3; // 原始文本: {originalText}";

            // 先替换CFG格式
            var newContent = System.Text.RegularExpressions.Regex.Replace(
                content,
                cfgPattern,
                cfgReplacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 再替换CS格式
            newContent = System.Text.RegularExpressions.Regex.Replace(
                newContent,
                csPattern,
                csReplacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return newContent;
        }

        /// <summary>
        /// 生成本地化文件
        /// </summary>
        public void GenerateLocalizationFile(string filePath, IEnumerable<LocalizationItem> items, Func<LocalizationItem, string> valueSelector)
        {
            var existingContent = string.Empty;
            var existingKeys = new HashSet<string>();

            // 如果文件存在，读取现有内容并记录已有键
            if (File.Exists(filePath))
            {
                existingContent = File.ReadAllText(filePath);

                // 提取已存在的本地化键
                var keyMatches = System.Text.RegularExpressions.Regex.Matches(
                    existingContent,
                    @"#LOC_\w+");

                foreach (var match in keyMatches)
                {
                    existingKeys.Add(match.ToString());
                }
            }

            using (var writer = new StreamWriter(filePath, true))
            {
                // 如果是新文件，写入头部
                if (!File.Exists(filePath))
                {
                    writer.WriteLine("Localization");
                    writer.WriteLine("{");
                    var langCode = Path.GetFileNameWithoutExtension(filePath).Replace(".cfg", "");
                    writer.WriteLine($"    {langCode}");
                    writer.WriteLine("    {");
                }
                else
                {
                    // 在现有文件末尾添加新内容前写入注释
                    writer.WriteLine();
                    writer.WriteLine($"        // 自动添加于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }

                // 写入新的本地化键值对
                foreach (var item in items)
                {
                    if (existingKeys.Contains(item.LocalizationKey))
                        continue; // 跳过已存在的键

                    var value = valueSelector(item)
                        .Replace("\r", "")
                        .Replace("\n", " ");

                    writer.WriteLine($"        {item.LocalizationKey} = {value}");
                    existingKeys.Add(item.LocalizationKey);
                }

                // 如果是新文件，写入尾部
                if (!File.Exists(filePath))
                {
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
            }
        }
    }
}