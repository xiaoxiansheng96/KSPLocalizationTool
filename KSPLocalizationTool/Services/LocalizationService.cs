using KSPLocalizationTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace KSPLocalizationTool.Services
{
    // 将类声明改为partial
    public partial class LocalizationService
    {
        // 添加GeneratedRegex属性和分部方法到类内部
        [GeneratedRegex(@"#LOC_\w+")]
        private static partial Regex LocalizationKeyRegex();
    
        public LocalizationService() { }

        /// <summary>
        /// 替换文本并添加注释
        /// </summary>
        public static string ReplaceText(string content, string parameterType, string originalText, string localizationKey)
        {
            // 1. 处理CFG文件格式: parameterType = "originalText"
            var cfgPattern = $@"({parameterType}\s*=\s*"")({originalText})([""])";
            var cfgReplacement = $"$1{localizationKey}$3{Environment.NewLine}// 原始文本: {originalText}";

            // 2. 处理CS文件中的属性标签内格式（如[KSPEvent]、[KSPField]中的guiName）
            // 匹配属性内的 guiName = "原始文本"（不含分号）
            // 修正：增强正则匹配，确保包含完整的属性上下文（如逗号、括号等）
            var csAttributePattern = $@"(guiName\s*=\s*[""'])({Regex.Escape(originalText)})([""'])(?=[,\s}}\]])";
            // 修正：替换时严格保留引号，并确保注释添加在正确位置（不破坏标签结构）
            var csAttributeReplacement = $"$1{localizationKey}$3";
            // 单独处理注释添加，避免插入到标签内部导致语法错误
            // （在标签外另起一行添加注释）
            csAttributeReplacement += $"{Environment.NewLine}// 原始文本: {originalText}";

            // 3. 处理CS文件中的普通代码格式（如变量赋值，含分号）
            // 匹配 变量 = "原始文本"; 格式
            var csCodePattern = $@"({parameterType}\s*[=:]\s*[""'])({originalText})([""']\s*;)";
            var csCodeReplacement = $"$1{localizationKey}$3 // 原始文本: {originalText}";

            // 执行替换（按顺序：先属性内，再普通代码，最后CFG）
            var newContent = content;

            // 优先替换属性内的格式（避免分号污染）
            newContent = System.Text.RegularExpressions.Regex.Replace(
                newContent,
                csAttributePattern,
                csAttributeReplacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 再替换普通CS代码格式
            newContent = System.Text.RegularExpressions.Regex.Replace(
                newContent,
                csCodePattern,
                csCodeReplacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 最后替换CFG格式
            newContent = System.Text.RegularExpressions.Regex.Replace(
                newContent,
                cfgPattern,
                cfgReplacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return newContent;
        }

        /// <summary>
        /// 生成本地化文件
        /// </summary>
        public static void GenerateLocalizationFile(string filePath, IEnumerable<LocalizationItem> items, Func<LocalizationItem, string> valueSelector)
        {
            string existingContent;
            var existingKeys = new HashSet<string>();
        
            // 如果文件存在，读取现有内容并记录已有键
            if (File.Exists(filePath))
            {
                existingContent = File.ReadAllText(filePath);
        
                // 提取已存在的本地化键
                var keyMatches = LocalizationKeyRegex().Matches(existingContent);
        
                foreach (var match in keyMatches)
                {
if (match?.ToString() is string key)
{
    existingKeys.Add(key);
}
                }
            }
        
            // 修复using语句的错误格式 - 移除多余的大括号
            using var writer = new StreamWriter(filePath, true);
        
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